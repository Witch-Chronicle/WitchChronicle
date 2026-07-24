using UnityEngine;

/// <summary>
/// 강화의 "규칙"을 전담하는 컨트롤러.
/// - EnhanceTableData 조회, 비용 확인, 확률 판정, 성공 시 EquipmentInstance 갱신까지 담당
/// - 실제 골드/재료 소모는 PlayerInventory의 저수준 메서드(TrySpendGold/TryConsumeItem)에 위임
/// * PlayerInventory는 "얼마 갖고 있는지"만 알고, 강화 규칙 자체는 전혀 모름
/// </summary>
public class EnhanceController : MonoBehaviour
{
    [Header("강화 테이블")]
    [SerializeField] private EnhanceTableData _enhanceTableData;

    // UI 등 외부에서 테이블이 필요할 때(스탯 미리보기 계산 등) 이걸 통해서만 접근
    public EnhanceTableData Table => _enhanceTableData;

    /// <summary>
    /// 이 장비의 다음 강화 단계 데이터를 반환. 최대 단계면 null.
    /// </summary>
    public EnhanceLevelEntry GetNextLevelEntry(EquipmentInstance instance)
    {
        if (instance == null || _enhanceTableData == null) return null;

        return _enhanceTableData.GetLevelData(instance.enhanceLevel + 1);
    }

    /// <summary>
    /// 지금 강화가 가능한 상태인지 (최대 단계 아님 + 골드/재료 전부 충분) 확인.
    /// </summary>
    public bool CanEnhance(EquipmentInstance instance, out EnhanceLevelEntry nextEntry)
    {
        nextEntry = GetNextLevelEntry(instance);

        if (nextEntry == null || PlayerInventory.Instance == null)
        {
            return false;
        }

        bool hasEnoughGold = PlayerInventory.Instance.Gold >= nextEntry.requiredGold;

        bool hasEnoughMaterials = true;
        if (nextEntry.requiredMaterials != null)
        {
            foreach (var required in nextEntry.requiredMaterials)
            {
                if (required.material == null) continue;

                if (PlayerInventory.Instance.GetTotalQuantity(required.material) < required.amount)
                {
                    hasEnoughMaterials = false;
                    break;
                }
            }
        }

        return hasEnoughGold && hasEnoughMaterials;
    }

    /// <summary>
    /// 천장까지의 진행률(%). UI에 "n.n%" 형식으로 표시할 때 사용.
    /// pityCount가 0(천장 없음)이면 0을 반환.
    /// </summary>
    public float GetPityProgress(EquipmentInstance instance, EnhanceLevelEntry nextEntry)
    {
        if (instance == null || nextEntry == null || nextEntry.pityCount <= 0)
        {
            return 0f;
        }

        return Mathf.Min(100f, (float)instance.enhanceAttemptCount / nextEntry.pityCount * 100f);
    }

    /// <summary>
    /// 강화 실패 1번당 천장 진행률이 몇 % 오르는지. UI에 "(+n.n%)" 형식으로 같이 보여줄 때 사용.
    /// pityCount가 0(천장 없음)이면 0을 반환.
    /// </summary>
    public float GetPityIncreasePerAttempt(EnhanceLevelEntry nextEntry)
    {
        if (nextEntry == null || nextEntry.pityCount <= 0)
        {
            return 0f;
        }

        return 1f / nextEntry.pityCount * 100f;
    }

    /// <summary>
    /// 장비 강화 시도. 골드/재료가 전부 충분하면 소모하고 확률 판정.
    /// 성공하면 enhanceLevel 증가 + cachedStats 갱신 + 시도횟수 리셋, 실패하면 단계 유지 + 시도횟수 증가.
    /// (비용은 성공/실패 상관없이 소모)
    /// * 천장 시스템: 이번 시도로 pityCount에 도달하면 확률과 무관하게 강제 성공.
    /// * 소모 전에 CanEnhance()로 전체 비용을 미리 검증하므로, 소모 도중 부족해서 롤백하는 경우는 없음.
    /// </summary>
    /// <returns>강화 성공 여부</returns>
    public bool TryEnhance(EquipmentInstance instance)
    {
        if (instance == null || instance.baseData == null || PlayerInventory.Instance == null)
        {
            return false;
        }

        if (!CanEnhance(instance, out EnhanceLevelEntry nextEntry))
        {
            if (nextEntry == null)
            {
                Debug.Log($"[EnhanceController] 이미 최대 강화 단계: {instance.baseData.itemName}");
            }
            else
            {
                Debug.Log($"[EnhanceController] 강화 조건 부족(골드/재료): {instance.baseData.itemName}");
            }
            return false;
        }

        // 위에서 이미 충분함을 확인했으므로 그대로 소모
        PlayerInventory.Instance.TrySpendGold(nextEntry.requiredGold);

        if (nextEntry.requiredMaterials != null)
        {
            foreach (var required in nextEntry.requiredMaterials)
            {
                if (required.material != null && required.amount > 0)
                {
                    PlayerInventory.Instance.TryConsumeItem(required.material, required.amount);
                }
            }
        }

        // 진행률이 100%(pityCount번 실패)에 도달한 "다음" 시도부터 확정 성공.
        // 즉 pityCount번째 실패로 100%가 채워지고, 그 다음 시도(=이번 시도)가 확정 성공.
        bool isPityGuaranteed = nextEntry.pityCount > 0
            && instance.enhanceAttemptCount >= nextEntry.pityCount;

        bool isSuccess = isPityGuaranteed || Random.Range(0f, 100f) < nextEntry.successRate;

        if (isSuccess)
        {
            instance.enhanceLevel = nextEntry.level;
            instance.enhanceAttemptCount = 0; // 다음 단계로 넘어갔으니 시도횟수 리셋
            instance.RefreshStats(_enhanceTableData);
        }
        else
        {
            instance.enhanceAttemptCount++;
        }

        // 골드/재료 소모, (성공 시)강화 단계 변경까지 끝났으니 UI 갱신 신호만 보냄
        PlayerInventory.Instance.RaiseInventoryChanged();

        Debug.Log(isSuccess
            ? $"[EnhanceController] 강화 성공: {instance.baseData.itemName} -> +{instance.enhanceLevel}"
            : $"[EnhanceController] 강화 실패: {instance.baseData.itemName} (단계 유지: +{instance.enhanceLevel}, 시도횟수: {instance.enhanceAttemptCount})");

        return isSuccess;
    }
}
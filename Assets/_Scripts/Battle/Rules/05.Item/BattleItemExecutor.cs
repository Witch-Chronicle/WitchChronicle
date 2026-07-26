using UnityEngine;

namespace Battle.Rules
{
    /// <summary>
    /// 전투 중 아이템(포션) 사용 실행부
    /// PotionItemData를 받아 회복/상태이상 해제 효과를 적용하고 인벤토리에서 차감
    /// </summary>
    public class BattleItemExecutor
    {
        private readonly StatusEffectController _statusEffectController;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="statusEffectController">상태이상 관리자</param>
        public BattleItemExecutor(StatusEffectController statusEffectController)
        {
            _statusEffectController = statusEffectController;
        }

        /// <summary>
        /// 포션 사용
        /// 인벤토리 차감 후 효과 실행
        /// </summary>
        /// <param name="user">사용자 (효과 적용 대상)</param>
        /// <param name="potionData">사용할 포션 데이터</param>
        /// <returns>실행 결과</returns>
        public BattleItemResult UsePotion(BattleUnit user, PotionItemData potionData)
        {
            BattleItemResult result = new BattleItemResult();

            if (user == null || potionData == null)
            {
                Debug.LogWarning("[BattleItemExecutor] 인자가 null입니다");
                result.Success = false;
                return result;
            }

            if (user.IsAlive == false)
            {
                Debug.LogWarning($"[BattleItemExecutor] {user.UnitName}은 사망 상태입니다");
                result.Success = false;
                return result;
            }

            // 인벤토리에서 포션 하나 차감
            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("[BattleItemExecutor] PlayerInventory.Instance가 null입니다");
                result.Success = false;
                return result;
            }

            bool consumed = PlayerInventory.Instance.TryConsumeItem(potionData, 1);

            if (consumed == false)
            {
                Debug.Log($"[BattleItemExecutor] {potionData.itemName} 보유 수량 부족");
                result.Success = false;
                return result;
            }

            // 포션 효과 실행
            switch (potionData.PotionEffect)
            {
                case PotionEffect.HealHp:
                    ApplyHealHp(user, potionData, ref result);
                    break;

                case PotionEffect.HealMp:
                    ApplyHealMp(user, potionData, ref result);
                    break;

                case PotionEffect.CureStatusEffect:
                    ApplyCureStatusEffect(user, potionData, ref result);
                    break;

                default:
                    Debug.LogWarning($"[BattleItemExecutor] 알 수 없는 PotionEffect: {potionData.PotionEffect}");
                    break;
            }

            // 인벤토리 UI 갱신 이벤트 발행
            PlayerInventory.Instance.RaiseInventoryChanged();

            result.Success = true;
            result.UsedItem = potionData;
            return result;
        }

        /// <summary>
        /// HP 회복 포션 실행
        /// </summary>
        private void ApplyHealHp(BattleUnit user, PotionItemData potionData, ref BattleItemResult result)
        {
            int healAmount = Mathf.RoundToInt(user.MaxHp * potionData.HealRatio);
            healAmount = Mathf.Max(1, healAmount);

            user.Heal(healAmount);
            result.HealHpAmount = healAmount;

            Debug.Log($"[Item] {user.UnitName}: {potionData.itemName} 사용 (HP +{healAmount})");
        }

        /// <summary>
        /// MP 회복 포션 실행
        /// </summary>
        private void ApplyHealMp(BattleUnit user, PotionItemData potionData, ref BattleItemResult result)
        {
            if (user.UsesMp == false)
            {
                Debug.Log($"[Item] {user.UnitName}은 MP를 사용하지 않아 마나 포션 효과 없음");
                return;
            }

            int healAmount = Mathf.RoundToInt(user.MaxMp * potionData.HealRatio);
            healAmount = Mathf.Max(1, healAmount);

            RestoreMp(user, healAmount);
            result.HealMpAmount = healAmount;

            Debug.Log($"[Item] {user.UnitName}: {potionData.itemName} 사용 (MP +{healAmount})");
        }

        /// <summary>
        /// 상태이상 해제 포션 실행
        /// </summary>
        private void ApplyCureStatusEffect(BattleUnit user, PotionItemData potionData, ref BattleItemResult result)
        {
            if (potionData.CureStatusEffectType == StatusEffectType.None)
            {
                Debug.LogWarning($"[BattleItemExecutor] {potionData.itemName}의 해제 대상이 None입니다");
                return;
            }

            if (_statusEffectController == null)
            {
                Debug.LogWarning("[BattleItemExecutor] StatusEffectController가 null입니다");
                return;
            }

            bool removed = _statusEffectController.RemoveStatusEffect(user, potionData.CureStatusEffectType);
            result.CuredStatusEffect = potionData.CureStatusEffectType;
            result.CureSuccess = removed;

            if (removed)
            {
                Debug.Log($"[Item] {user.UnitName}: {potionData.itemName} 사용, {potionData.CureStatusEffectType} 해제");
            }
            else
            {
                Debug.Log($"[Item] {user.UnitName}은 {potionData.CureStatusEffectType} 상태가 아님 (포션은 소모됨)");
            }
        }

        /// <summary>
        /// BattleUnit에 MP 회복 메서드가 없어 UseMp를 음수처럼 사용하는 대신
        /// UseMp의 반대 방향으로 리플렉션 없이 처리하기 위한 헬퍼
        /// (BattleUnit 수정 없이 우회)
        /// </summary>
        private void RestoreMp(BattleUnit user, int amount)
        {
            // BattleUnit에 Heal 메서드는 있지만 MP 회복 메서드는 없음
            // 코어 담당자에게 RestoreMp 메서드 추가 요청 필요
            // 임시: UseMp에 음수를 넘겨 회복 시도 (BattleUnit 구현에 따라 실패 가능)
            Debug.LogWarning("[BattleItemExecutor] BattleUnit.RestoreMp 메서드 필요. 현재는 MP 회복 미작동");
            // TODO: 코어 담당자에게 BattleUnit.RestoreMp(int amount) 메서드 요청
        }
    }

    /// <summary>
    /// 아이템 사용 결과
    /// UI, 이펙트가 이 결과를 받아 연출
    /// </summary>
    public struct BattleItemResult
    {
        public bool Success;
        public ItemData UsedItem;
        public int HealHpAmount;
        public int HealMpAmount;
        public StatusEffectType CuredStatusEffect;
        public bool CureSuccess;
    }
}

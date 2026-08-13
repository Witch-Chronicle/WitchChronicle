using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마도서 시스템 테스트용 디버그 컴포넌트 (UI 없이 키로 확인).
/// 씬 아무 오브젝트에 붙여서 사용하고, 확인이 끝나면 제거하면 된다.
///
/// F5 : 마도서 인벤토리에 지급
/// F6 : 마도서 사용 (스킬 습득 / 중복 시 골드) - UI 없이 로직만
/// F7 : 현재 보유 스킬 목록 출력
/// F8 : 후보 스킬 상태 출력 (티어 범위 / 미습득 수)
/// F9 : 가챠 UI 열기/닫기
/// F10: 스킬 장착 UI 열기/닫기
/// </summary>
public class SkillBookDebug : MonoBehaviour
{
    [Header("테스트할 마도서")]
    [SerializeField] private SkillBookItemData _skillBook;

    [Header("가챠 UI")]
    [Tooltip("F9로 열 가챠 창. 비우면 씬에서 자동으로 찾는다")]
    [SerializeField] private SkillGachaController _gachaUI;

    [Header("스킬 장착 UI")]
    [Tooltip("F10으로 열 장착 창. 비우면 씬에서 자동으로 찾는다")]
    [SerializeField] private SkillEquipUIController _equipUI;

    [Header("키 설정")]
    [SerializeField] private KeyCode _giveKey = KeyCode.F5;
    [SerializeField] private KeyCode _useKey = KeyCode.F6;
    [SerializeField] private KeyCode _listKey = KeyCode.F7;
    [SerializeField] private KeyCode _statusKey = KeyCode.F8;
    [SerializeField] private KeyCode _gachaKey = KeyCode.F9;
    [SerializeField] private KeyCode _equipKey = KeyCode.F10;

    [Header("지급 개수")]
    [SerializeField] private int _giveAmount = 5;

    private readonly List<SkillData> _buffer = new List<SkillData>();
    private bool _isGachaOpen;
    private bool _isEquipOpen;

    private void Update()
    {
        if (Input.GetKeyDown(_giveKey))
        {
            GiveBook();
        }

        if (Input.GetKeyDown(_useKey))
        {
            UseBook();
        }

        if (Input.GetKeyDown(_listKey))
        {
            PrintLearnedSkills();
        }

        if (Input.GetKeyDown(_statusKey))
        {
            PrintCandidateStatus();
        }

        if (Input.GetKeyDown(_gachaKey))
        {
            ToggleGachaUI();
        }

        if (Input.GetKeyDown(_equipKey))
        {
            ToggleEquipUI();
        }
    }

    /// <summary>스킬 장착 UI 열기/닫기.</summary>
    private void ToggleEquipUI()
    {
        if (_equipUI == null)
        {
            _equipUI = FindFirstObjectByType<SkillEquipUIController>(FindObjectsInactive.Include);
        }

        if (_equipUI == null)
        {
            Debug.LogError("[SkillBookDebug] 씬에 SkillEquipUIController가 없습니다");
            return;
        }

        // if (_isEquipOpen)
        // {
        //     _equipUI.Close();
        //     _isEquipOpen = false;
        //     Debug.Log("[SkillBookDebug] 장착 UI 닫음");
        // }
        // else
        // {
        //     _equipUI.Open();
        //     _isEquipOpen = true;
        //     Debug.Log("[SkillBookDebug] 장착 UI 열음");
        // }
    }

    /// <summary>가챠 UI 열기/닫기.</summary>
    private void ToggleGachaUI()
    {
        if (_gachaUI == null)
        {
            _gachaUI = FindFirstObjectByType<SkillGachaController>(FindObjectsInactive.Include);
        }

        if (_gachaUI == null)
        {
            Debug.LogError("[SkillBookDebug] 씬에 SkillGachaController가 없습니다");
            return;
        }

        if (_isGachaOpen)
        {
            _gachaUI.Close();
            _isGachaOpen = false;
            Debug.Log("[SkillBookDebug] 가챠 UI 닫음");
        }
        else
        {
            _gachaUI.Open();
            _isGachaOpen = true;
            Debug.Log("[SkillBookDebug] 가챠 UI 열음");
        }
    }

    /// <summary>마도서를 인벤토리에 지급.</summary>
    private void GiveBook()
    {
        if (CheckReady() == false)
        {
            return;
        }

        PlayerInventory.Instance.AddItem(_skillBook, _giveAmount);
        Debug.Log($"[SkillBookDebug] {_skillBook.itemName} x{_giveAmount} 지급");
    }

    /// <summary>마도서 사용 후 결과 출력.</summary>
    private void UseBook()
    {
        if (CheckReady() == false)
        {
            return;
        }

        SkillBookResult result = SkillBookUseService.Use(_skillBook);

        if (result.Success == false)
        {
            Debug.LogWarning("[SkillBookDebug] 사용 실패 (보유 수량 부족 또는 초기화 문제)");
            return;
        }

        if (result.LearnedSkill != null)
        {
            Debug.Log(
                $"[SkillBookDebug] 습득 성공 → {result.LearnedSkill.SkillName} " +
                $"(Tier {result.LearnedSkill.Tier})");
        }
        else
        {
            Debug.Log($"[SkillBookDebug] 습득할 스킬 없음 → 골드 +{result.RewardGold}");
        }
    }

    /// <summary>현재 보유(습득) 스킬 목록 출력.</summary>
    private void PrintLearnedSkills()
    {
        if (SkillInventory.Instance == null)
        {
            Debug.LogError("[SkillBookDebug] SkillInventory가 없습니다. SystemManagers에 추가하세요");
            return;
        }

        IReadOnlyList<SkillData> learned = SkillInventory.Instance.LearnedSkills;

        Debug.Log($"[SkillBookDebug] ===== 보유 스킬 {learned.Count}개 =====");

        for (int i = 0; i < learned.Count; i++)
        {
            SkillData s = learned[i];

            if (s != null)
            {
                Debug.Log($"  [{i}] {s.SkillName} (Tier {s.Tier})");
            }
        }
    }

    /// <summary>마도서 후보 스킬의 티어별 상태 출력.</summary>
    private void PrintCandidateStatus()
    {
        if (CheckReady() == false)
        {
            return;
        }

        SkillData[] pool = _skillBook.CandidateSkills;

        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning($"[SkillBookDebug] {_skillBook.itemName}에 후보 스킬이 비어 있습니다");
            return;
        }

        int inRange = 0;
        int notLearned = 0;
        _buffer.Clear();

        for (int i = 0; i < pool.Length; i++)
        {
            SkillData s = pool[i];

            if (s == null || _skillBook.IsInTierRange(s) == false)
            {
                continue;
            }

            inRange++;

            if (SkillInventory.Instance.HasSkill(s) == false)
            {
                notLearned++;
                _buffer.Add(s);
            }
        }

        Debug.Log(
            $"[SkillBookDebug] {_skillBook.itemName} " +
            $"(티어 {_skillBook.MinTier}~{_skillBook.MaxTier}) / " +
            $"후보 {pool.Length}개 중 범위내 {inRange}개, 미습득 {notLearned}개");

        for (int i = 0; i < _buffer.Count && i < 10; i++)
        {
            Debug.Log($"  뽑기 가능: {_buffer[i].SkillName} (Tier {_buffer[i].Tier})");
        }
    }

    /// <summary>필요한 참조가 준비됐는지 확인.</summary>
    private bool CheckReady()
    {
        if (_skillBook == null)
        {
            Debug.LogError("[SkillBookDebug] 테스트할 마도서를 인스펙터에 지정하세요");
            return false;
        }

        if (SkillInventory.Instance == null)
        {
            Debug.LogError("[SkillBookDebug] SkillInventory가 없습니다. SystemManagers/InventoryManager에 추가하세요");
            return false;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("[SkillBookDebug] PlayerInventory가 없습니다");
            return false;
        }

        return true;
    }
}

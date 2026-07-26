using UnityEngine;
using UnityEngine.InputSystem;

/// 테스트 전용 — 스탯 UI 나오기 전까지 파티 성장 확인용. 빌드 전 삭제.
/// 씬의 아무 오브젝트에 하나만 부착 (Party 오브젝트 추천). Party가 세팅돼 있어야 동작.
///
/// X: 파티 전원 경험치 +100 | ←/→: 조작할 캐릭터 선택
/// 1~6: 선택 캐릭터 스탯 투자 | R: 선택 캐릭터 스탯 초기화
/// Q: 선택 캐릭터 무기 장착/해제 | Tab: 파티 전원 스탯 로그
public class StatDebugCheat : MonoBehaviour
{
    [SerializeField] private EquipItemData _testWeaponData;   // 장비 SO (Weapon 슬롯, 05.HJH Item 시스템)

    private EquipmentInstance _testWeapon;   // 0강 테스트 개체
    private int _index;                      // 선택된 캐릭터 (0=아리엘)

    private StatController Selected => Party.Instance.Members[_index];

    private void Awake()
    {
        if (_testWeaponData != null)
            _testWeapon = new EquipmentInstance(_testWeaponData, 0, null);   // 강화 테이블 없이 기본 스탯
    }

    /// <summary>
    /// kb.~~~~.wasPressedThisFrame
    /// 이것들을 나중에 UI 부분에서 onClick으로 바꾸면 될 것 같다.
    /// 예시: hpButton.onClick.AddListener(() => _current.AllocatePoint(StatType.MaxHP));
    /// </summary>
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || Party.Instance == null || Party.Instance.Members.Count == 0) return;

        // ── 파티 전원 경험치 ──
        if (kb.xKey.wasPressedThisFrame)
        {
            foreach (var m in Party.Instance.Members)
            {
                int beforeLevel = m.Level;
                int beforeExp = m.Exp;
                m.AddExp(100);

                // 레벨도 경험치도 안 변함 = 만렙이라 경험치가 버려진 것
                if (m.Level == beforeLevel && m.Exp == beforeExp)
                    Debug.Log($"[{m.name}] 이미 최고 레벨을 달성하였습니다!");
                else
                    Debug.Log($"[{m.name}] Lv.{m.Level} (Exp: {m.Exp}/{m.ExpToNextLevel}) 잔여P {m.AvailablePoints}");
            }
        }

        // ── 캐릭터 선택 (정보창 화살표와 같은 순환) ──
        int count = Party.Instance.Members.Count;
        if (kb.rightArrowKey.wasPressedThisFrame) SelectMember((_index + 1) % count);
        if (kb.leftArrowKey.wasPressedThisFrame) SelectMember((_index - 1 + count) % count);

        // ── 선택 캐릭터 조작 ──
        if (kb.digit1Key.wasPressedThisFrame) Allocate(StatType.MaxHP);
        if (kb.digit2Key.wasPressedThisFrame) Allocate(StatType.MaxMP);
        if (kb.digit3Key.wasPressedThisFrame) Allocate(StatType.SpellPower);
        if (kb.digit4Key.wasPressedThisFrame) Allocate(StatType.Intelligence);
        if (kb.digit5Key.wasPressedThisFrame) Allocate(StatType.Defense);
        if (kb.digit6Key.wasPressedThisFrame) Allocate(StatType.Speed);

        if (kb.rKey.wasPressedThisFrame)
        {
            bool ok = Selected.TryResetAllocations();
            Debug.Log(ok ? $"[{Selected.name}] 스탯 초기화 완료. 잔여P {Selected.AvailablePoints}" : "초기화 실패 (골드 부족)");
        }

        if (kb.qKey.wasPressedThisFrame) ToggleWeapon();

        /// 나중에 UI 연동시 _current.OnStatsChanged += Refresh; 쓰면 알아서 값을 갱신
        if (kb.tabKey.wasPressedThisFrame)
            foreach (var m in Party.Instance.Members) LogStats(m);
    }

    private void SelectMember(int index)
    {
        _index = index;
        Debug.Log($"── 선택: [{Selected.name}] Lv.{Selected.Level}, 잔여P {Selected.AvailablePoints} ──");
    }

    private void Allocate(StatType type)
    {
        bool ok = Selected.AllocatePoint(type);
        ///<summary>
        /// 여기 있는 Debug.Log를 나중에 UI에서 "포인트 부족" 안내 표시가 되는 UI로 연동
        ///</summary>
        Debug.Log(ok
            ? $"[{Selected.name}] {type} +1 → {Selected.GetStat(type)} (잔여 {Selected.AvailablePoints})"
            : $"[{Selected.name}] 포인트 부족으로 {type} 투자 실패");
    }

    private void LogStats(StatController c)
    {
        /// <summary>
        /// 나중에 밑의 예시와 같이 UI에 연동하도록 변경
        /// 예 :
        /// levelText.text  = $"Lv.{c.Level}";
        /// spellText.text  = c.GetStat(StatType.SpellPower).ToString();
        /// pointText.text  = $"남은 포인트: {c.AvailablePoints}";
        /// slotText.text   = $"주문 슬롯: {c.SpellSlotCount}";
        /// </summary>
        Debug.Log($"[{c.name}] Lv.{c.Level} Exp: {c.Exp}/{c.ExpToNextLevel} 잔여P {c.AvailablePoints} | " +
                  $"HP {c.GetStat(StatType.MaxHP)} MP {c.GetStat(StatType.MaxMP)} " +
                  $"마력 {c.GetStat(StatType.SpellPower)} 지능 {c.GetStat(StatType.Intelligence)} " +
                  $"방어 {c.GetStat(StatType.Defense)} 속도 {c.GetStat(StatType.Speed)} 운 {c.GetStat(StatType.Luck)} | " +
                  $"주문 슬롯 {c.SpellSlotCount}");
    }

    private void ToggleWeapon()
    {
        if (_testWeapon == null)
        {
            Debug.LogWarning("StatDebugCheat: Test Weapon Data 슬롯에 장비 SO(Weapon)를 연결하세요.");
            return;
        }

        var slot = _testWeapon.baseData.equipSlotType;

        if (Selected.GetEquipped(slot) == _testWeapon)
        {
            Selected.Unequip(slot);
            Debug.Log($"[{Selected.name}] {_testWeapon.baseData.itemName} 해제 → 마력 {Selected.GetStat(StatType.SpellPower)}");
        }
        else
        {
            int preview = Selected.PreviewStat(_testWeapon, StatType.SpellPower);
            bool ok = Selected.Equip(_testWeapon);
            Debug.Log(ok
                ? $"[{Selected.name}] {_testWeapon.baseData.itemName} 장착 → 마력 {Selected.GetStat(StatType.SpellPower)} (미리보기 {preview})"
                : $"[{Selected.name}] 장착 실패 — 착용 레벨({_testWeapon.baseData.requiredLevel}) 미달");
        }
    }
}

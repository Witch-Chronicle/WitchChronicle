using System;
using System.Collections.Generic;
using UnityEngine;

/// 스탯 적용 창구
/// CharacterStats의 공개 함수만 호출해서 경험치·포인트 분배·장비 보정을 적용
///
/// 스탯 시스템 연동 가이드(회의 결정) 기준:
///   영구 성장 = AllocatePoint() → TryUseStatPoint()
///   장비 보정 = StatModifier (Flat / Equipment / itemId) — AddBonusStat 사용 안 함
///   최종 스탯 읽기 = GetStat(), 주문 슬롯 = SpellSlotCount (CharacterStats 위임)
[RequireComponent(typeof(CharacterStats))]
public class StatController : MonoBehaviour
{
    [SerializeField] private CharacterStats _stats;
    [SerializeField] private GrowthConfig _growthConfig;   // CharacterStats에 연결한 것과 같은 에셋

    [Header("스탯 초기화")]
    [SerializeField] private int _resetCost = 500;   // 초기화 비용 (골드)

    private readonly Dictionary<EquipSlotType, EquipmentInstance> _equipped = new();

    // ── 외부 공개 창구 (UI·전투 담당은 아래 프로퍼티/함수만 쓰면 됨) ──

    /// 내부 CharacterStats 직접 접근용. 가급적 아래 창구를 쓰고, 특별한 경우에만 사용.
    public CharacterStats Stats => _stats;

    /// 현재 레벨 (UI 표시용)
    public int Level => _stats.Level;

    /// 현재 레벨에서 쌓은 경험치 (UI 경험치 바용)
    public int Exp => _stats.Exp;

    /// 다음 레벨까지 필요한 경험치. 만렙이거나 GrowthConfig 미연결이면 0. (UI 경험치 바: Exp/ExpToNextLevel)
    public int ExpToNextLevel => _growthConfig != null ? _growthConfig.ExpToNext(Level) : 0;

    /// 잔여 스탯 포인트 (UI 표시용)
    public int AvailablePoints => _stats.AvailableStatPoints;

    /// 스탯 초기화 비용 (UI 표시용)
    public int ResetCost => _resetCost;

    /// 주문 슬롯 수 — 계산은 CharacterStats(지능 연동)가 담당, 여기는 위임만
    public int SpellSlotCount => _stats.SpellSlotCount;

    /// 최종 스탯 조회 — UI 수치 표시 공용 창구 (전투 계산은 CharacterStats.CombatStats 사용)
    public int GetStat(StatType type) => _stats.GetStat(type);

    /// 스탯·장비 변동 알림. UI는 이 이벤트만 구독하면 됨 (내부 이벤트로 중계).
    public event Action OnStatsChanged
    {
        add => _stats.OnStatsChanged += value;
        remove => _stats.OnStatsChanged -= value;
    }

    private void Awake()
    {
        if (_stats == null) _stats = GetComponent<CharacterStats>();
    }

    // ── 경험치 ──

    /// 전투 승리 시 호출. 레벨업 시 포인트 지급은 CharacterStats가 처리.
    public void AddExp(int amount) => _stats.AddExp(amount);

    // ── 스탯 포인트 분배 ──

    /// 스탯 [+] 버튼에서 호출. 포인트가 있으면 즉시 반영, 성공 여부 반환.
    public bool AllocatePoint(StatType type) => _stats.TryUseStatPoint(type);

    /// [초기화] 버튼에서 호출. 골드를 소모하고 투자한 포인트를 전액 회수.
    /// 골드가 부족하면 false 반환, 스탯 변화 없음.
    public bool TryResetAllocations()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[StatController] PlayerInventory.Instance가 null입니다.");
            return false;
        }

        bool isSuccess = PlayerInventory.Instance.TrySpendGold(_resetCost);
        if (!isSuccess)
        {
            Debug.Log($"[StatController] 골드 부족으로 스탯 초기화 실패 (필요: {_resetCost})");
            return false;
        }

        _stats.ResetAllocatedStats();
        return true;
    }

    // ── 장비 (StatModifier 방식 — 가이드 3-4 ~ 3-6) ──

    /// 슬롯별 현재 장착 장비 (null = 빈 슬롯). 장비 UI 표시용.
    public EquipmentInstance GetEquipped(EquipSlotType slot) =>
        _equipped.TryGetValue(slot, out var equipment) ? equipment : null;

    /// 장착. 같은 슬롯에 기존 장비가 있으면 자동 해제 후 장착.
    /// 착용 레벨 미달이면 false.
    public bool Equip(EquipmentInstance equipment)
    {
        if (equipment?.baseData == null) return false;
        if (Level < equipment.baseData.requiredLevel) return false;

        EquipSlotType slot = equipment.baseData.equipSlotType;
        Unequip(slot);
        _stats.AddModifiers(CreateEquipmentModifiers(equipment));
        _equipped[slot] = equipment;
        return true;
    }

    /// 해제. 해당 장비 ID의 Equipment modifier만 제거 (직접 빼기 계산 안 함 — 가이드 3-5)
    public void Unequip(EquipSlotType slot)
    {
        if (!_equipped.TryGetValue(slot, out var current) || current == null) return;
        _stats.RemoveModifiersBySource(StatModifierSourceType.Equipment, EquipmentId(current));
        _equipped.Remove(slot);
    }

    /// 장착 중인 장비가 강화됐을 때 보정 재등록 (가이드 3-6).
    /// 강화 UI(③)가 강화 성공 후 호출 — cachedStats가 갱신된 상태여야 함.
    public void RefreshEquipment(EquipSlotType slot)
    {
        if (!_equipped.TryGetValue(slot, out var equipment) || equipment == null) return;
        _stats.RemoveModifiersBySource(StatModifierSourceType.Equipment, EquipmentId(equipment));
        _stats.AddModifiers(CreateEquipmentModifiers(equipment));
    }

    /// 장착 시 스탯 변동 미리보기 (장비 UI용).
    /// 장비 보정이 Flat이라는 전제의 근사값 — 퍼센트 버프가 걸린 전투 중에는 오차 가능.
    public int PreviewStat(EquipmentInstance equipment, StatType type)
    {
        int current = GetStat(type);
        var replaced = GetEquipped(equipment.baseData.equipSlotType);
        int removed = replaced != null ? GetStatFromSet(replaced.cachedStats, type) : 0;
        return current - removed + GetStatFromSet(equipment.cachedStats, type);
    }

    /// cachedStats(강화 반영된 최종 장비 스탯) → Flat StatModifier 목록 변환
    private static List<StatModifier> CreateEquipmentModifiers(EquipmentInstance equipment)
    {
        string id = EquipmentId(equipment);
        var stats = equipment.cachedStats;
        var modifiers = new List<StatModifier>();

        void Add(StatType type, int value)
        {
            if (value != 0)
                modifiers.Add(new StatModifier(type, value, StatModifierType.Flat,
                                               StatModifierSourceType.Equipment, id));
        }

        Add(StatType.MaxHP, stats.hp);
        Add(StatType.MaxMP, stats.mp);
        Add(StatType.SpellPower, stats.spellPower);
        Add(StatType.Intelligence, stats.intelligence);
        Add(StatType.Defense, stats.defense);
        Add(StatType.Speed, stats.speed);
        Add(StatType.Luck, stats.luck);
        return modifiers;
    }

    private static string EquipmentId(EquipmentInstance equipment) =>
        equipment.baseData.itemId.ToString();

    private static int GetStatFromSet(in EquipStatCalculator.StatSet set, StatType type) => type switch
    {
        StatType.MaxHP => set.hp,
        StatType.MaxMP => set.mp,
        StatType.SpellPower => set.spellPower,
        StatType.Intelligence => set.intelligence,
        StatType.Defense => set.defense,
        StatType.Speed => set.speed,
        StatType.Luck => set.luck,
        _ => 0
    };
}

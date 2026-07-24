using System;
using UnityEngine;
using System.Collections.Generic;


public class CharacterStats : MonoBehaviour
{
    [Header("Base Data")]
    [SerializeField] private CharacterBaseStats _baseStats;
    [SerializeField] private GrowthConfig _growthConfig;   // 경험치 곡선 (밸런스 시트 연동)

    [Header("Runtime")]
    [SerializeField] private int _level = 1;
    [SerializeField] private int _exp;
    [SerializeField] private int _availableStatPoints;

    [Header("Allocated Stats")]
    [SerializeField] private StatBlock _allocatedStats = new StatBlock();

    [Header("Upgrade / Equipment Bonus")]
    [SerializeField] private StatBlock _bonusStats = new StatBlock();

    [Header("Stat Modifiers")]
    [SerializeField] private List<StatModifier> _statModifiers = new List<StatModifier>();

    [Header("Battle Stats")]
    [SerializeField] private CombatStatBlock _combatStats = new CombatStatBlock();

    [Header("Spell Slot Settings")]
    [SerializeField] private int _baseSpellSlotCount = 3;
    [SerializeField] private int _intelligencePerAdditionalSlot = 10;
    [SerializeField] private int _maxSpellSlotCount = 6;

    private StatBlock _finalStats;

    public event Action OnStatsChanged;

    public string CharacterId => _baseStats != null ? _baseStats.CharacterId : string.Empty;
    public string CharacterName => _baseStats != null ? _baseStats.CharacterName : string.Empty;

    public int Level => _level;
    public int Exp => _exp;
    public int AvailableStatPoints => _availableStatPoints;

    public StatBlock FinalStats => _finalStats;

    // 전투용 최종 스탯 확인용 프로퍼티
    public CombatStatBlock CombatStats => _combatStats;

    public int CombatMaxHp => _combatStats.MaxHp;
    public int CombatMaxMp => _combatStats.MaxMp;

    public float CombatAttackPower => _combatStats.AttackPower;
    public float CombatMagicPower => _combatStats.MagicPower;
    public float CombatDefensePower => _combatStats.Defense;
    public float CombatMagicDefensePower => _combatStats.MagicDefense;
    public float CombatSpeed => _combatStats.Speed;
    public float CombatLuck => _combatStats.Luck;

    public int SpellSlotCount => CalculateSpellSlotCount();

    // 현재 할당된 스탯 포인트 확인용 프로퍼티
    public int AllocatedHp => _allocatedStats.Get(StatType.MaxHP);
    public int AllocatedMp => _allocatedStats.Get(StatType.MaxMP);
    public int AllocatedSpellPower => _allocatedStats.Get(StatType.SpellPower);
    public int AllocatedDefense => _allocatedStats.Get(StatType.Defense);
    public int AllocatedIntelligence => _allocatedStats.Get(StatType.Intelligence);
    public int AllocatedSpeed => _allocatedStats.Get(StatType.Speed);
    public int AllocatedLuck => _allocatedStats.Get(StatType.Luck);


    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 캐릭터SO를 기반으로 초기 상태 설정
    /// </summary>
    public void Initialize()
    {
        if (_baseStats == null)
        {
            Debug.LogError($"{name}에 CharacterBaseStats가 없습니다.");
            return;
        }

        _level = _baseStats.StartLevel;
        RecalculateStats();
    }

    /// <summary>
    /// 현재 캐릭터의 최종 스탯을 반환
    /// 전투, UI, 강화에서 이 함수를 통해 스탯 확인
    /// </summary>
    /// <param name="type">확인할 스탯</param>
    /// <returns></returns>
    public int GetStat(StatType type)
    {
        if (_finalStats == null)
        {
            RecalculateStats();
        }

        if (_finalStats == null)
        {
            return 0;
        }

        return _finalStats.Get(type);
    }

    /// <summary>
    /// 남은 스탯 포인트를 사용해 포인트 투자
    /// 성공시 최종 스탯 다시 계산
    /// </summary>
    /// <param name="type">강화할 스탯</param>
    /// <param name="amount">강화할 정도</param>
    /// <returns></returns>
    public bool TryUseStatPoint(StatType type, int amount = 1)
    {
        if (amount <= 0) return false;
        if (_availableStatPoints < amount) return false;

        _availableStatPoints -= amount;
        _allocatedStats.Add(type, amount);

        RecalculateStats();
        return true;
    }

    // LEGACY
    // AddModifier로 대체해야함
    /// <summary>
    /// 장비 강화나 버프 등으로 얻은 추가 스탯 합산용
    /// </summary>
    /// <param name="type"></param>
    /// <param name="amount"></param>
    public void AddBonusStat(StatType type, int amount)
    {
        _bonusStats.Add(type, amount);
        RecalculateStats();
    }

    /// <summary>
    /// 외부 시스템에서 생성한 스탯 보정값을 등록
    /// 장비, 버프, 디버프, 음식 효과 등이 이 함수를 통해 스탯에 영향
    /// </summary>
    /// <param name="modifier">등록할 스탯 보정값</param>
    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null)
        {
            return;
        }

        _statModifiers.Add(modifier);
        RecalculateStats();
    }

    /// <summary>
    /// 여러 개의 스탯 보정값을 한 번에 등록
    /// 장비처럼 여러 스탯을 동시에 올리는 경우 사용
    /// </summary>
    /// <param name="modifiers">등록할 스탯 보정값 목록</param>
    public void AddModifiers(IEnumerable<StatModifier> modifiers)
    {
        if (modifiers == null)
        {
            return;
        }

        foreach (StatModifier modifier in modifiers)
        {
            if (modifier == null)
            {
                continue;
            }

            _statModifiers.Add(modifier);
        }

        RecalculateStats();
    }

    /// <summary>
    /// 특정 출처에서 발생한 스탯 보정값들을 제거
    /// 장비 해제, 버프 종료, 음식 효과 종료 등에 사용
    /// </summary>
    /// <param name="sourceType">제거할 보정값 출처 타입</param>
    /// <param name="sourceId">제거할 보정값 출처 ID</param>
    public void RemoveModifiersBySource(StatModifierSourceType sourceType, string sourceId)
    {
        _statModifiers.RemoveAll(modifier =>
            modifier.SourceType == sourceType &&
            modifier.SourceId == sourceId);

        RecalculateStats();
    }

    /// <summary>
    /// 특정 출처 타입에 해당하는 모든 스탯 보정값을 제거
    /// 전투 종료 시 버프, 디버프, 상태이상 보정을 정리할 때 사용
    /// </summary>
    /// <param name="sourceType">제거할 보정값 출처 타입입</param>
    public void ClearModifiersBySourceType(StatModifierSourceType sourceType)
    {
        _statModifiers.RemoveAll(modifier => modifier.SourceType == sourceType);
        RecalculateStats();
    }

    /// <summary>
    /// 전투 중에만 유지되는 스탯 보정값을 모두 제거
    /// 전투 종료 시 호출
    /// </summary>
    public void ClearBattleModifiers()
    {
        _statModifiers.RemoveAll(modifier =>
            modifier.SourceType == StatModifierSourceType.Buff ||
            modifier.SourceType == StatModifierSourceType.Debuff ||
            modifier.SourceType == StatModifierSourceType.StatusEffect);

        RecalculateStats();
    }

    /// <summary>
    /// 투자한 스탯을 모두 초기화
    /// 투자했던 포인트 돌려줌
    /// 스탯 초기화할때 사용
    /// </summary>
    public void ResetAllocatedStats()
    {
        int refundPoints = 0;

        refundPoints += _allocatedStats.maxHP;
        refundPoints += _allocatedStats.maxMP;
        refundPoints += _allocatedStats.magicPower;
        refundPoints += _allocatedStats.intelligence;
        refundPoints += _allocatedStats.defense;
        refundPoints += _allocatedStats.speed;
        refundPoints += _allocatedStats.luck;

        _allocatedStats = new StatBlock();
        _availableStatPoints += refundPoints;

        RecalculateStats();
    }

    /// <summary>
    /// 경험치 추가
    /// 필요 경험치를 넘으면 레벨업
    /// </summary>
    /// <param name="amount"></param>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        _exp += amount;

        // 만렙 가드: 만렙에서 필요 경험치가 0이 되면 무한 루프에 빠지는 것 방지
        int maxLevel = _growthConfig != null ? _growthConfig.MaxLevel : 30;
        while (_level < maxLevel && _exp >= GetRequiredExp())
        {
            _exp -= GetRequiredExp();
            LevelUp();
        }
        if (_level >= maxLevel) _exp = 0;   // 만렙 초과 경험치는 버림

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 레벨업을 담당
    /// 레벨이 올라가면 스탯 포인트 지급
    /// </summary>
    private void LevelUp()
    {
        _level++;
        _availableStatPoints += _baseStats.StatPointPerLevel;

        RecalculateStats();
    }

    /// <summary>
    /// 현재 레벨에서 필요한 경험치 계산
    /// </summary>
    /// <returns></returns>
    private int GetRequiredExp()
    {
        // GrowthConfig(밸런스 시트 곡선) 연결 시 그 값을 사용, 미연결이면 기존 임시 공식
        return _growthConfig != null
            ? _growthConfig.ExpToNext(_level)
            : 100 + (_level - 1) * 50;
    }

    /// <summary>
    /// 스탯 변화후 최종 스탯 재계산용
    /// </summary>
    private void RecalculateStats()
    {
        if (_baseStats == null) return;

        _finalStats = _baseStats.BaseStats.Clone();

        ApplyLevelGrowth();
        ApplyAllocatedStats();
        ApplyBonusStats();
        ApplyStatModifiers();

        RecalculateCombatStats();

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 레벨에 따른 자동 성장치를 최종 스탯에 합산
    /// 임시
    /// </summary>
    private void ApplyLevelGrowth()
    {
        int levelBonus = _level - 1;

        _finalStats.maxHP += levelBonus * 10;
        _finalStats.maxMP += levelBonus * 3;
        _finalStats.magicPower += levelBonus * 2;
        _finalStats.defense += levelBonus * 1;
        _finalStats.speed += levelBonus * 1;
    }

    /// <summary>
    /// 직접 투자한 스탯포인트를 실제 스탯 증가량으로 변환
    /// </summary>
    private void ApplyAllocatedStats()
    {
        _finalStats.maxHP += _allocatedStats.maxHP * 10;
        _finalStats.maxMP += _allocatedStats.maxMP * 5;
        _finalStats.magicPower += _allocatedStats.magicPower * 2;
        _finalStats.intelligence += _allocatedStats.intelligence * 2;
        _finalStats.defense += _allocatedStats.defense * 2;
        _finalStats.speed += _allocatedStats.speed * 1;
        _finalStats.luck += _allocatedStats.luck * 1;
    }

    /// <summary>
    /// 장비, 강화, 버프 등으로 얻은 보너스 스탯을 최종 스탯
    /// </summary>
    private void ApplyBonusStats()
    {
        _finalStats.maxHP += _bonusStats.maxHP;
        _finalStats.maxMP += _bonusStats.maxMP;
        _finalStats.magicPower += _bonusStats.magicPower;
        _finalStats.intelligence += _bonusStats.intelligence;
        _finalStats.defense += _bonusStats.defense;
        _finalStats.speed += _bonusStats.speed;
        _finalStats.luck += _bonusStats.luck;
    }

    /// <summary>
    /// FinalStats를 기반으로 전투 계산에 사용할 BattleStats를 갱신
    /// 플레이어에게 보이는 성장 스탯을 전투용 파생 스탯으로 변환
    /// </summary>
    private void RecalculateCombatStats()
    {
        if (_combatStats == null)
        {
            _combatStats = new CombatStatBlock();
        }

        if (_finalStats == null)
        {
            _combatStats.SetValues(1, 0, 0f, 0f, 0f, 0f, 0f, 0f);
            return;
        }

        int maxHp = _finalStats.maxHP;
        int maxMp = _finalStats.maxMP;

        float attackPower = _finalStats.magicPower * 0.5f;
        float magicPower = _finalStats.magicPower;

        float defense = _finalStats.defense;
        float magicDefense = _finalStats.intelligence;

        float speed = _finalStats.speed;
        float luck = _finalStats.luck;

        _combatStats.SetValues(
            maxHp,
            maxMp,
            attackPower,
            magicPower,
            defense,
            magicDefense,
            speed,
            luck);
    }

    /// <summary>
    /// 등록된 StatModifier 목록을 최종 스탯에 반영합니다.
    /// Flat → PercentAdd → PercentMultiply 순서로 계산합니다.
    /// </summary>
    private void ApplyStatModifiers()
    {
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            int baseValue = _finalStats.Get(type);

            float flatValue = GetModifierTotal(type, StatModifierType.Flat);
            float percentAddValue = GetModifierTotal(type, StatModifierType.PercentAdd);
            float percentMultiplyValue = GetPercentMultiplyTotal(type);

            float modifiedValue = baseValue + flatValue;
            modifiedValue *= 1f + percentAddValue;
            modifiedValue *= percentMultiplyValue;

            int finalValue = Mathf.RoundToInt(modifiedValue);
            int delta = finalValue - baseValue;

            if (delta != 0)
            {
                _finalStats.Add(type, delta);
            }
        }
    }

    /// <summary>
    /// 특정 스탯과 보정 방식에 해당하는 보정값 총합을 반환합니다.
    /// </summary>
    /// <param name="statType">계산할 스탯 종류입니다.</param>
    /// <param name="modifierType">계산할 보정 방식입니다.</param>
    /// <returns>조건에 맞는 보정값의 총합입니다.</returns>
    private float GetModifierTotal(StatType statType, StatModifierType modifierType)
    {
        float total = 0f;

        for (int i = 0; i < _statModifiers.Count; i++)
        {
            StatModifier modifier = _statModifiers[i];

            if (modifier.StatType != statType)
            {
                continue;
            }

            if (modifier.ModifierType != modifierType)
            {
                continue;
            }

            total += modifier.Value;
        }

        return total;
    }

    /// <summary>
    /// 특정 스탯에 적용되는 곱연산 보정값을 계산합니다.
    /// PercentMultiply modifier의 Value는 0.5라면 x1.5, -0.2라면 x0.8로 처리됩니다.
    /// </summary>
    /// <param name="statType">계산할 스탯 종류입니다.</param>
    /// <returns>최종 곱연산 배율입니다.</returns>
    private float GetPercentMultiplyTotal(StatType statType)
    {
        float totalMultiplier = 1f;

        for (int i = 0; i < _statModifiers.Count; i++)
        {
            StatModifier modifier = _statModifiers[i];

            if (modifier.StatType != statType)
            {
                continue;
            }

            if (modifier.ModifierType != StatModifierType.PercentMultiply)
            {
                continue;
            }

            totalMultiplier *= 1f + modifier.Value;
        }

        return totalMultiplier;
    }

    /// <summary>
    /// 현재 지능 스탯을 기반으로 장착 가능한 주문 슬롯 수를 계산합니다.
    /// 거점의 스킬 장착 UI와 전투 진입 전 스킬 구성에서 사용됩니다.
    /// </summary>
    private int CalculateSpellSlotCount()
    {
        int intelligence = GetStat(StatType.Intelligence);

        if (_intelligencePerAdditionalSlot <= 0)
        {
            return _baseSpellSlotCount;
        }

        int slotCount = _baseSpellSlotCount + intelligence / _intelligencePerAdditionalSlot;
        return Mathf.Min(_maxSpellSlotCount, slotCount);
    }
}
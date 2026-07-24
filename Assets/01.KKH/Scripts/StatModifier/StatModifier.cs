using System;

/// <summary>
/// 특정 스탯에 적용되는 하나의 보정값을 나타냄
/// 장비, 강화, 버프, 디버프 등 다양한 시스템에서 생성되어 CharacterStats에 등록 -> 최종 스탯
/// </summary>
[Serializable]
public class StatModifier
{
    private readonly StatType _statType;
    private readonly float _value;
    private readonly StatModifierType _modifierType;
    private readonly StatModifierSourceType _sourceType;
    private readonly string _sourceId;

    public StatType StatType => _statType;
    public float Value => _value;
    public StatModifierType ModifierType => _modifierType;
    public StatModifierSourceType SourceType => _sourceType;
    public string SourceId => _sourceId;

    /// <summary>
    /// 스탯 보정 데이터를 생성
    /// </summary>
    /// <param name="statType">보정할 스탯 종류</param>
    /// <param name="value">보정 수치</param>
    /// <param name="modifierType">보정 적용 방식</param>
    /// <param name="sourceType">보정이 발생한 시스템 종류</param>
    /// <param name="sourceId">보정 출처를 식별하는 ID</param>
    public StatModifier(
        StatType statType,
        float value,
        StatModifierType modifierType,
        StatModifierSourceType sourceType,
        string sourceId)
    {
        _statType = statType;
        _value = value;
        _modifierType = modifierType;
        _sourceType = sourceType;
        _sourceId = sourceId;
    }
}
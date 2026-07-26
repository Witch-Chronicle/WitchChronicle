using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 UI 표시용 색상 팔레트. ElementType/SkillEffectType/DamageType/Tier별 색상을 인스펙터에서 관리.
/// 프로젝트에 하나만 만들어두고 모든 스킬 UI가 이걸 참조.
/// </summary>
[CreateAssetMenu(menuName = "Witch Chronicle/Skill Presentation Palette")]
public class SkillPresentationPalette : ScriptableObject
{
    [Serializable]
    public class ElementColorEntry
    {
        public ElementType elementType;
        public Color color = Color.white;
    }

    [Serializable]
    public class SkillTypeColorEntry
    {
        public SkillEffectType skillType;
        public Color color = Color.white;
    }

    [Serializable]
    public class DamageTypeColorEntry
    {
        public DamageType damageType;
        public Color color = Color.white;
    }

    [Serializable]
    public class TierColorEntry
    {
        public int tier;
        public Color color = Color.white;
    }

    [Header("Element (물리/화염/얼음/번개/물/독/암흑)")]
    [SerializeField] private List<ElementColorEntry> _elementColors = new List<ElementColorEntry>();

    [Header("Skill Type (공격/Hp회복/Mp회복/버프/디버프/상태이상/부활)")]
    [SerializeField] private List<SkillTypeColorEntry> _skillTypeColors = new List<SkillTypeColorEntry>();

    [Header("Damage Type (물리/마법/고정)")]
    [SerializeField] private List<DamageTypeColorEntry> _damageTypeColors = new List<DamageTypeColorEntry>();

    [Header("Tier (1~4)")]
    [SerializeField] private List<TierColorEntry> _tierColors = new List<TierColorEntry>();

    [Header("Fallback (테이블에 없는 값일 때)")]
    [SerializeField] private Color _defaultColor = Color.white;

    public Color GetElementColor(ElementType type)
    {
        foreach (var entry in _elementColors)
        {
            if (entry.elementType == type) return entry.color;
        }
        return _defaultColor;
    }

    public Color GetSkillTypeColor(SkillEffectType type)
    {
        foreach (var entry in _skillTypeColors)
        {
            if (entry.skillType == type) return entry.color;
        }
        return _defaultColor;
    }

    public Color GetDamageTypeColor(DamageType type)
    {
        foreach (var entry in _damageTypeColors)
        {
            if (entry.damageType == type) return entry.color;
        }
        return _defaultColor;
    }

    public Color GetTierColor(int tier)
    {
        foreach (var entry in _tierColors)
        {
            if (entry.tier == tier) return entry.color;
        }
        return _defaultColor;
    }
}
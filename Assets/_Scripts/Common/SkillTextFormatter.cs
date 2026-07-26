public static class SkillTextFormatter
{
    public static string GetTierText(int tier) => $"티어 {tier}";

    public static string GetDamageTypeText(DamageType type)
    {
        switch (type)
        {
            case DamageType.Physical: return "물리";
            case DamageType.Magical: return "마법";
            case DamageType.Fixed: return "고정";
            default: return string.Empty;
        }
    }

    public static string GetSkillTypeText(SkillEffectType type)
    {
        switch (type)
        {
            case SkillEffectType.Damage: return "공격";
            case SkillEffectType.Heal: return "회복(HP)";
            case SkillEffectType.HealMp: return "회복(MP)";
            case SkillEffectType.Buff: return "버프";
            case SkillEffectType.Debuff: return "디버프";
            case SkillEffectType.StatusEffect: return "상태이상";
            case SkillEffectType.Revive: return "부활";
            default: return string.Empty;
        }
    }

    public static string GetElementTypeText(ElementType type)
    {
        switch (type)
        {
            case ElementType.None: return string.Empty;
            case ElementType.Physical: return "물리";
            case ElementType.Fire: return "화염";
            case ElementType.Ice: return "얼음";
            case ElementType.Lightning: return "번개";
            case ElementType.Water: return "물";
            case ElementType.Poison: return "독";
            case ElementType.Dark: return "암흑";
            default: return string.Empty;
        }
    }
}
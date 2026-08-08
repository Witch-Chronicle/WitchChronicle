using System;

/// <summary>
/// SkillEquipOwnedSlot 하나를 그리는 데 필요한 데이터를 한데 묶은 구조체입니다.
/// IsSelected/EquippedByName은 셀이 재사용되므로 매번 최신 상태로 다시 계산해서 넣어줘야 합니다.
/// </summary>
public readonly struct SkillOwnedSlotEntry
{
    public readonly SkillData Skill;
    public readonly bool IsSelected;
    public readonly string EquippedByName;
    public readonly Action<SkillData> OnClicked;

    public SkillOwnedSlotEntry(SkillData skill, bool isSelected, string equippedByName, Action<SkillData> onClicked)
    {
        Skill = skill;
        IsSelected = isSelected;
        EquippedByName = equippedByName;
        OnClicked = onClicked;
    }
}
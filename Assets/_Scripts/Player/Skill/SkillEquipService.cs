using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 장착 처리.
/// 실제 저장은 캐릭터별 PlayerSkillLoadout이 하고, 여기서는
/// 슬롯 단위 조작(지정 슬롯에 넣기/빼기)과 파티 조회를 담당한다.
/// </summary>
public static class SkillEquipService
{
    /// <summary>활성 파티원 목록을 가져온다.</summary>
    public static void GetPartyMembers(List<PersistentCharacterUnit> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("[SkillEquip] PersistentCharacterManager가 없습니다");
            return;
        }

        PersistentCharacterManager.Instance.GetActivePartyMembers(result);
    }

    /// <summary>해당 캐릭터의 장착 슬롯 수.</summary>
    public static int GetSlotCount(PersistentCharacterUnit character)
    {
        PlayerSkillLoadout loadout = GetLoadout(character);

        return loadout != null ? loadout.GetMaxSkillSlotCount() : 0;
    }

    /// <summary>
    /// 슬롯 인덱스에 장착된 스킬. 비어 있으면 null.
    /// </summary>
    public static SkillData GetEquippedAt(PersistentCharacterUnit character, int slotIndex)
    {
        PlayerSkillLoadout loadout = GetLoadout(character);

        if (loadout == null || slotIndex < 0)
        {
            return null;
        }

        IReadOnlyList<SkillData> equipped = loadout.EquippedSkills;

        return slotIndex < equipped.Count ? equipped[slotIndex] : null;
    }

    /// <summary>
    /// 지정 슬롯에 스킬을 장착한다. 그 자리에 이미 스킬이 있으면 교체한다.
    /// </summary>
    /// <param name="character">대상 캐릭터</param>
    /// <param name="slotIndex">슬롯 인덱스</param>
    /// <param name="skill">장착할 스킬</param>
    /// <returns>성공 여부</returns>
    public static bool EquipAt(PersistentCharacterUnit character, int slotIndex, SkillData skill)
    {
        PlayerSkillLoadout loadout = GetLoadout(character);

        if (loadout == null || skill == null)
        {
            return false;
        }

        if (slotIndex < 0 || slotIndex >= loadout.GetMaxSkillSlotCount())
        {
            return false;
        }

        // 다른 슬롯에 이미 같은 스킬이 있으면 먼저 해제(중복 장착 방지)
        loadout.UnequipSkill(skill);

        // 대상 슬롯 비우기
        if (GetEquippedAt(character, slotIndex) != null)
        {
            loadout.UnequipSkillAt(slotIndex);
        }

        // PlayerSkillLoadout은 리스트 끝에만 추가되므로,
        // 원하는 자리에 넣기 위해 기존 목록을 복원하며 재구성한다.
        List<SkillData> current = new List<SkillData>(loadout.EquippedSkills);

        while (current.Count <= slotIndex)
        {
            current.Add(null);
        }

        current[slotIndex] = skill;

        loadout.ClearEquippedSkills();

        for (int i = 0; i < current.Count; i++)
        {
            if (current[i] != null)
            {
                loadout.TryEquipSkill(current[i]);
            }
        }

        Debug.Log($"[SkillEquip] {character.CharacterName} 슬롯{slotIndex} → {skill.SkillName}");
        return true;
    }

    /// <summary>지정 슬롯의 스킬을 해제한다.</summary>
    public static bool UnequipAt(PersistentCharacterUnit character, int slotIndex)
    {
        PlayerSkillLoadout loadout = GetLoadout(character);

        if (loadout == null)
        {
            return false;
        }

        SkillData skill = GetEquippedAt(character, slotIndex);

        if (skill == null)
        {
            return false;
        }

        loadout.UnequipSkill(skill);

        Debug.Log($"[SkillEquip] {character.CharacterName} 슬롯{slotIndex} 해제 ({skill.SkillName})");
        return true;
    }

    /// <summary>해당 캐릭터가 이 스킬을 이미 장착 중인지.</summary>
    public static bool IsEquipped(PersistentCharacterUnit character, SkillData skill)
    {
        PlayerSkillLoadout loadout = GetLoadout(character);

        if (loadout == null || skill == null)
        {
            return false;
        }

        IReadOnlyList<SkillData> equipped = loadout.EquippedSkills;

        for (int i = 0; i < equipped.Count; i++)
        {
            if (equipped[i] == skill)
            {
                return true;
            }
        }

        return false;
    }

    private static PlayerSkillLoadout GetLoadout(PersistentCharacterUnit character)
    {
        return character != null ? character.PlayerSkillLoadout : null;
    }
}

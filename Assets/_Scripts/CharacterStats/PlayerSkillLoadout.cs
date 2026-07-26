using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어의 현재 장착된 스킬 목록 관리
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class PlayerSkillLoadout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterStats _characterStats;

    [Header("Equipped Skills")]
    [SerializeField] private List<SkillData> _equippedSkills = new List<SkillData>();

    public IReadOnlyList<SkillData> EquippedSkills => _equippedSkills;

    private void Awake()
    {
        if (_characterStats == null || _characterStats.gameObject != gameObject)
        {
            _characterStats = GetComponent<CharacterStats>();
        }

        ValidateEquippedSkills();
    }

    /// <summary>
    /// 현재 장착 가능한 최대 스킬 슬롯 수 반환
    /// </summary>
    /// <returns>현재 스킬 슬롯 수</returns>
    public int GetMaxSkillSlotCount()
    {
        if (_characterStats == null)
            return 0;

        return _characterStats.SpellSlotCount;
    }

    /// <summary>
    /// 장착 스킬 목록 반환
    /// 함수를 통해 null이나 슬롯 초과분은 제외
    /// </summary>
    /// <returns>전투에서 사용할 수 있는 스킬 목록</returns>
    public IReadOnlyList<SkillData> GetBattleSkillList()
    {
        ValidateEquippedSkills();
        return _equippedSkills;
    }

    /// <summary>
    /// 스킬 장착 -> 외부에서 이용해 스킬 장착하기
    /// </summary>
    /// <param name="skillData">장착할 스킬</param>
    /// <returns>성공여부</returns>
    public bool TryEquipSkill(SkillData skillData)
    {
        if (skillData == null)
            return false;

        if (_equippedSkills.Contains(skillData))
            return false;

        if (_equippedSkills.Count >= GetMaxSkillSlotCount())
            return false;

        _equippedSkills.Add(skillData);
        return true;
    }

    /// <summary>
    /// 스킬 해제 -> 외부에서 이용해 스킬 해제하기
    /// </summary>
    /// <param name="skillData"></param>
    public void UnequipSkill(SkillData skillData)
    {
        if (skillData == null)
            return;

        _equippedSkills.Remove(skillData);
    }

    /// <summary>
    /// 슬롯 인덱스를 기준으로 스킬 해제
    /// </summary>
    /// <param name="slotIndex">해제할 인덱스</param>
    public void UnequipSkillAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _equippedSkills.Count)
            return;

        _equippedSkills.RemoveAt(slotIndex);
    }

    /// <summary>
    /// 장착 스킬 전체 해제
    /// </summary>
    public void ClearEquippedSkills()
    {
        _equippedSkills.Clear();
    }
    
    /// <summary>
    /// 장착 스킬 목록 검증
    /// null이거나 슬롯 초과하면 없앰
    /// </summary>
    private void ValidateEquippedSkills()
    {
        RemoveNullSkills();
        TrimSkillsOverSlotCount();
    }

    /// <summary>
    /// null 스킬 제거
    /// </summary>
    private void RemoveNullSkills()
    {
        for (int i = _equippedSkills.Count - 1; i >= 0; i--)
        {
            if (_equippedSkills[i] != null)
                continue;

            _equippedSkills.RemoveAt(i);
        }
    }

    /// <summary>
    /// 슬롯 초과 스킬 제거
    /// </summary>
    private void TrimSkillsOverSlotCount()
    {
        int maxSlotCount = GetMaxSkillSlotCount();

        if (maxSlotCount <= 0)
        {
            _equippedSkills.Clear();
            return;
        }

        while (_equippedSkills.Count > maxSlotCount)
        {
            _equippedSkills.RemoveAt(_equippedSkills.Count - 1);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 파티가 습득한 스킬 목록(보유 스킬).
/// 장착(PlayerSkillLoadout)과는 별개로, "배웠는가"만 관리한다.
/// 캐릭터별이 아니라 파티 공용이다.
/// </summary>
public class SkillInventory : MonoBehaviour
{
    public static SkillInventory Instance { get; private set; }

    [Header("보유 스킬")]
    [Tooltip("게임 시작 시 이미 배운 상태로 둘 스킬")]
    [SerializeField] private List<SkillData> _learnedSkills = new List<SkillData>();

    /// <summary>보유 중인 스킬 목록.</summary>
    public IReadOnlyList<SkillData> LearnedSkills => _learnedSkills;

    /// <summary>스킬을 새로 습득했을 때 발생.</summary>
    public event Action<SkillData> OnSkillLearned;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RemoveNullSkills();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>해당 스킬을 이미 배웠는지 확인.</summary>
    public bool HasSkill(SkillData skill)
    {
        return skill != null && _learnedSkills.Contains(skill);
    }

    /// <summary>
    /// 스킬 습득. 이미 배운 스킬이면 false.
    /// </summary>
    /// <param name="skill">습득할 스킬</param>
    /// <returns>새로 배웠으면 true</returns>
    public bool TryLearnSkill(SkillData skill)
    {
        if (skill == null || _learnedSkills.Contains(skill))
        {
            return false;
        }

        _learnedSkills.Add(skill);
        Debug.Log($"[SkillInventory] 스킬 습득: {skill.SkillName} (Tier {skill.Tier})");

        OnSkillLearned?.Invoke(skill);
        return true;
    }

    /// <summary>보유 스킬 중 조건에 맞는 것을 result에 담는다.</summary>
    public void GetLearnedSkills(List<SkillData> result, Predicate<SkillData> filter = null)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        for (int i = 0; i < _learnedSkills.Count; i++)
        {
            SkillData skill = _learnedSkills[i];

            if (skill == null)
            {
                continue;
            }

            if (filter == null || filter(skill))
            {
                result.Add(skill);
            }
        }
    }

    private void RemoveNullSkills()
    {
        for (int i = _learnedSkills.Count - 1; i >= 0; i--)
        {
            if (_learnedSkills[i] == null)
            {
                _learnedSkills.RemoveAt(i);
            }
        }
    }
}

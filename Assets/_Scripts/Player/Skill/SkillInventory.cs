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

    private readonly List<PersistentCharacterUnit> _partyBuffer = new List<PersistentCharacterUnit>();

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

    private void Start()
    {
        SyncPartyEquippedSkills();
    }

    /// <summary>
    /// 파티원이 이미 장착 중인 스킬(캐릭터 프리팹에 넣어둔 시작 스킬)을 보유 목록에 흡수한다.
    /// 이게 없으면 시작 스킬을 해제했을 때 목록에 없어서 다시 장착할 수 없다.
    /// 습득 연출이 뜨면 안 되므로 OnSkillLearned는 발생시키지 않는다.
    /// </summary>
    public void SyncPartyEquippedSkills()
    {
        if (PersistentCharacterManager.Instance == null)
        {
            return;
        }

        PersistentCharacterManager.Instance.GetActivePartyMembers(_partyBuffer);

        for (int i = 0; i < _partyBuffer.Count; i++)
        {
            PersistentCharacterUnit member = _partyBuffer[i];

            if (member == null || member.PlayerSkillLoadout == null)
            {
                continue;
            }

            IReadOnlyList<SkillData> equipped = member.PlayerSkillLoadout.EquippedSkills;

            for (int j = 0; j < equipped.Count; j++)
            {
                SkillData skill = equipped[j];

                if (skill != null && _learnedSkills.Contains(skill) == false)
                {
                    _learnedSkills.Add(skill);
                }
            }
        }
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

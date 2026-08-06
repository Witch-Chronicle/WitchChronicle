using System.Collections.Generic;
using UnityEngine;

/// <summary>마도서 사용 결과. RolledSkill은 신규/중복 여부와 관계없이 실제 당첨 스킬이다.</summary>
public struct SkillBookResult
{
    public bool Success;
    public SkillData RolledSkill;
    public SkillData LearnedSkill;
    public bool IsDuplicate;
    public int RewardGold;
    public int RolledTier;
}

/// <summary>
/// 마도서 판정 담당.
/// 1) 티어 추첨 → 2) 해당 티어 전체 스킬에서 균등 추첨 →
/// 3) 미보유면 습득, 보유 중이면 골드로 전환한다.
/// 인벤토리 차감도 이 클래스에서 한 번만 처리한다.
/// </summary>
public static class SkillBookUseService
{
    private static readonly List<SkillData> _tierPool = new List<SkillData>();

    public static SkillBookResult Use(SkillBookItemData book)
    {
        SkillBookResult result = new SkillBookResult();

        if (book == null)
        {
            Debug.LogWarning("[SkillBook] SkillBookItemData가 null입니다.");
            return result;
        }

        if (SkillInventory.Instance == null)
        {
            Debug.LogError("[SkillBook] SkillInventory가 씬에 없습니다.");
            return result;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("[SkillBook] PlayerInventory가 없습니다.");
            return result;
        }

        // 결과 후보를 먼저 검증한다. 설정 오류 때문에 빈 티어가 뽑혔을 때
        // 마도서만 사라지는 상황을 막기 위해 검증 후 차감한다.
        int tier = book.RollTier();
        BuildTierPool(book, tier);

        if (_tierPool.Count == 0)
        {
            Debug.LogError($"[SkillBook] {book.itemName}: {tier}티어 후보 스킬이 없습니다. 마도서는 소모하지 않습니다.");
            return result;
        }

        if (PlayerInventory.Instance.TryConsumeItem(book, 1) == false)
        {
            Debug.Log($"[SkillBook] {book.itemName} 보유 수량이 부족합니다.");
            return result;
        }

        SkillData rolledSkill = _tierPool[Random.Range(0, _tierPool.Count)];
        bool isDuplicate = SkillInventory.Instance.HasSkill(rolledSkill);

        result.Success = true;
        result.RolledTier = tier;
        result.RolledSkill = rolledSkill;
        result.IsDuplicate = isDuplicate;

        if (isDuplicate)
        {
            int gold = Mathf.Max(0, book.DuplicateGold);

            if (gold > 0)
            {
                PlayerInventory.Instance.AddGold(gold);
            }

            result.RewardGold = gold;
            Debug.Log($"[SkillBook] {book.itemName} → {tier}티어 {rolledSkill.SkillName} 중복, {gold} G 지급");
        }
        else
        {
            // 프로젝트의 TryLearnSkill 반환형이 void/bool 어느 쪽이어도 호환되도록
            // 반환값에는 의존하지 않는다. 바로 위 HasSkill 판정이 신규 여부의 기준이다.
            SkillInventory.Instance.TryLearnSkill(rolledSkill);
            result.LearnedSkill = rolledSkill;
            Debug.Log($"[SkillBook] {book.itemName} → {tier}티어 {rolledSkill.SkillName} 습득");
        }

        PlayerInventory.Instance.RaiseInventoryChanged();
        return result;
    }

    private static void BuildTierPool(SkillBookItemData book, int tier)
    {
        _tierPool.Clear();

        SkillData[] pool = book.CandidateSkills;

        if (pool == null)
        {
            return;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            SkillData skill = pool[i];

            if (skill == null || skill.Tier != tier)
            {
                continue;
            }

            _tierPool.Add(skill);
        }
    }
}
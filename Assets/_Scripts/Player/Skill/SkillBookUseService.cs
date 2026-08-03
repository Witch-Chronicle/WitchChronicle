using System.Collections.Generic;
using UnityEngine;

/// <summary>마도서 사용 결과.</summary>
public struct SkillBookResult
{
    /// <summary>사용 자체가 성공했는지(보유 수량 부족 등이면 false)</summary>
    public bool Success;

    /// <summary>새로 습득한 스킬. 중복 보상으로 대체된 경우 null</summary>
    public SkillData LearnedSkill;

    /// <summary>중복이라 대신 받은 골드</summary>
    public int RewardGold;

    /// <summary>이번에 당첨된 티어(연출에서 등급 표시용)</summary>
    public int RolledTier;
}

/// <summary>
/// 마도서(그리모어) 사용 처리.
/// 티어 범위 안의 미습득 스킬 중 하나를 무작위로 습득시키고,
/// 더 배울 것이 없으면 골드로 대체 보상한다.
/// </summary>
public static class SkillBookUseService
{
    private static readonly List<SkillData> _candidates = new List<SkillData>();

    /// <summary>
    /// 마도서를 사용한다. 인벤토리에서 1개 차감 후 효과를 적용한다.
    /// </summary>
    /// <param name="book">사용할 마도서</param>
    /// <returns>사용 결과</returns>
    public static SkillBookResult Use(SkillBookItemData book)
    {
        SkillBookResult result = new SkillBookResult();

        if (book == null)
        {
            Debug.LogWarning("[SkillBook] 마도서 데이터가 null입니다");
            return result;
        }

        if (SkillInventory.Instance == null)
        {
            Debug.LogError("[SkillBook] SkillInventory가 씬에 없습니다");
            return result;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("[SkillBook] PlayerInventory가 없습니다");
            return result;
        }

        // 인벤토리에서 1개 차감
        if (PlayerInventory.Instance.TryConsumeItem(book, 1) == false)
        {
            Debug.Log($"[SkillBook] {book.itemName} 보유 수량 부족");
            return result;
        }

        result.Success = true;

        // 1) 가중치로 티어를 먼저 뽑는다
        int tier = book.RollTier();
        result.RolledTier = tier;

        // 2) 그 티어에서 아직 배우지 않은 스킬만 후보로
        BuildCandidates(book, tier);

        if (_candidates.Count > 0)
        {
            SkillData picked = _candidates[Random.Range(0, _candidates.Count)];
            SkillInventory.Instance.TryLearnSkill(picked);
            result.LearnedSkill = picked;

            Debug.Log($"[SkillBook] {book.itemName} → {tier}티어 당첨, {picked.SkillName} 습득");
        }
        else
        {
            // 뽑힌 티어에 배울 게 없으면 골드로 대체 보상 (확률 유지 방식)
            int gold = Mathf.Max(0, book.DuplicateGold);

            if (gold > 0)
            {
                PlayerInventory.Instance.AddGold(gold);
            }

            result.RewardGold = gold;

            Debug.Log($"[SkillBook] {book.itemName} → {tier}티어 당첨했지만 모두 습득 상태, 골드 {gold} 지급");
        }

        PlayerInventory.Instance.RaiseInventoryChanged();
        return result;
    }

    /// <summary>지정 티어 + 미습득 조건으로 후보 목록을 만든다.</summary>
    /// <param name="book">마도서</param>
    /// <param name="tier">뽑힌 티어</param>
    private static void BuildCandidates(SkillBookItemData book, int tier)
    {
        _candidates.Clear();

        SkillData[] pool = book.CandidateSkills;

        if (pool == null)
        {
            return;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            SkillData skill = pool[i];

            if (skill == null)
            {
                continue;
            }

            if (skill.Tier != tier)
            {
                continue;
            }

            if (SkillInventory.Instance.HasSkill(skill))
            {
                continue;
            }

            _candidates.Add(skill);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 공격 단일 타격 정보
/// </summary>
public readonly struct ConstellationPathHitEntry
{
    public BattleUnit Target { get; }
    public int RoundIndex { get; }
    public int SequenceIndex { get; }

    /// <summary>
    /// 단일 타격 정보 생성
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="roundIndex">공격 라운드</param>
    /// <param name="sequenceIndex">전체 실행 순서</param>
    public ConstellationPathHitEntry(BattleUnit target, int roundIndex, int sequenceIndex)
    {
        Target = target;
        RoundIndex = Mathf.Max(0, roundIndex);
        SequenceIndex = Mathf.Max(0, sequenceIndex);
    }
}

/// <summary>
/// 별자리 공격 타격 순서 생성
/// </summary>
public static class ConstellationPathHitSchedule
{
    /// <summary>
    /// 공격 방식에 따른 타격 목록 생성
    /// </summary>
    /// <param name="targets">공격 대상 목록</param>
    /// <param name="hitCountPerTarget">대상별 타격 횟수</param>
    /// <param name="attackPattern">공격 분배 방식</param>
    /// <param name="result">생성 결과</param>
    public static void Build(
        IReadOnlyList<BattleUnit> targets,
        int hitCountPerTarget,
        ConstellationPathAttackPattern attackPattern,
        List<ConstellationPathHitEntry> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        if (targets == null || hitCountPerTarget <= 0)
        {
            return;
        }

        List<BattleUnit> validTargets = CollectValidTargets(targets);

        if (validTargets.Count == 0)
        {
            return;
        }

        switch (attackPattern)
        {
            case ConstellationPathAttackPattern.AllTargetsSimultaneous:
                BuildSimultaneousSchedule(validTargets, hitCountPerTarget, result);
                break;

            case ConstellationPathAttackPattern.AllTargetsBalancedSequence:
                BuildBalancedSequenceSchedule(validTargets, hitCountPerTarget, result);
                break;

            case ConstellationPathAttackPattern.SingleTargetSequence:
                BuildSingleTargetSchedule(validTargets[0], hitCountPerTarget, result);
                break;
        }
    }

    /// <summary>
    /// 생존 대상 목록 생성
    /// </summary>
    /// <param name="targets">원본 대상 목록</param>
    /// <returns>생존 대상 목록</returns>
    private static List<BattleUnit> CollectValidTargets(IReadOnlyList<BattleUnit> targets)
    {
        List<BattleUnit> validTargets = new List<BattleUnit>();

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit target = targets[i];

            if (target == null || target.IsAlive == false || validTargets.Contains(target))
            {
                continue;
            }

            validTargets.Add(target);
        }

        return validTargets;
    }

    /// <summary>
    /// 라운드별 전체 동시 타격 목록 생성
    /// </summary>
    /// <param name="targets">공격 대상 목록</param>
    /// <param name="hitCountPerTarget">대상별 타격 횟수</param>
    /// <param name="result">생성 결과</param>
    private static void BuildSimultaneousSchedule(
        IReadOnlyList<BattleUnit> targets,
        int hitCountPerTarget,
        List<ConstellationPathHitEntry> result)
    {
        int sequenceIndex = 0;

        for (int roundIndex = 0; roundIndex < hitCountPerTarget; roundIndex++)
        {
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                result.Add(new ConstellationPathHitEntry(targets[targetIndex], roundIndex, sequenceIndex));
                sequenceIndex++;
            }
        }
    }

    /// <summary>
    /// 균등 랜덤 순차 타격 목록 생성
    /// 각 대상의 공격 횟수는 동일하게 유지하고 전체 공격 순서를 무작위화
    /// </summary>
    /// <param name="targets">공격 대상 목록</param>
    /// <param name="hitCountPerTarget">대상별 타격 횟수</param>
    /// <param name="result">생성 결과</param>
    private static void BuildBalancedSequenceSchedule(
        IReadOnlyList<BattleUnit> targets,
        int hitCountPerTarget,
        List<ConstellationPathHitEntry> result)
    {
        List<BattleUnit> attackPool =
            new List<BattleUnit>(
                targets.Count * hitCountPerTarget);

        // 각 대상을 정확히 동일 횟수만큼 공격 풀에 추가
        for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
        {
            for (int hitIndex = 0; hitIndex < hitCountPerTarget; hitIndex++)
            {
                attackPool.Add(
                    targets[targetIndex]);
            }
        }

        // 전체 공격 순서를 한 번에 무작위화
        Shuffle(attackPool);

        for (int sequenceIndex = 0; sequenceIndex < attackPool.Count; sequenceIndex++)
        {
            result.Add(
                new ConstellationPathHitEntry(
                    attackPool[sequenceIndex],
                    sequenceIndex,
                    sequenceIndex));
        }
    }

    /// <summary>
    /// 단일 대상 연속 타격 목록 생성
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <param name="hitCount">타격 횟수</param>
    /// <param name="result">생성 결과</param>
    private static void BuildSingleTargetSchedule(
        BattleUnit target,
        int hitCount,
        List<ConstellationPathHitEntry> result)
    {
        for (int i = 0; i < hitCount; i++)
        {
            result.Add(new ConstellationPathHitEntry(target, i, i));
        }
    }

    /// <summary>
    /// 대상 목록 무작위 정렬
    /// </summary>
    /// <param name="targets">정렬 대상 목록</param>
    private static void Shuffle(List<BattleUnit> targets)
    {
        for (int i = targets.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            BattleUnit temporaryTarget = targets[i];
            targets[i] = targets[randomIndex];
            targets[randomIndex] = temporaryTarget;
        }
    }
}
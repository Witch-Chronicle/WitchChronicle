using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 단일 박자 판정 결과
/// </summary>
public readonly struct ConstellationBeatResult
{
    public int BeatIndex { get; }
    public ConstellationJudgementType Judgement { get; }
    public float TimingError { get; }
    public float Score { get; }

    public bool IsSuccess =>
        Judgement != ConstellationJudgementType.Miss;

    /// <summary>
    /// 단일 박자 판정 결과 생성
    /// </summary>
    public ConstellationBeatResult(
        int beatIndex,
        ConstellationJudgementType judgement,
        float timingError,
        float score)
    {
        BeatIndex = beatIndex;
        Judgement = judgement;
        TimingError = Mathf.Abs(timingError);
        Score = Mathf.Clamp(score, 0f, 100f);
    }
}

/// <summary>
/// 별자리 전체 실행 결과
/// </summary>
public readonly struct ConstellationResult
{
    public bool IsSuccess { get; }
    public float Score { get; }
    public IReadOnlyList<ConstellationBeatResult> BeatResults { get; }

    /// <summary>
    /// 별자리 전체 실행 결과 생성
    /// </summary>
    public ConstellationResult(
        bool isSuccess,
        float score,
        IReadOnlyList<ConstellationBeatResult> beatResults)
    {
        IsSuccess = isSuccess;
        Score = Mathf.Clamp(score, 0f, 100f);

        BeatResults = beatResults ??
            Array.Empty<ConstellationBeatResult>();
    }
}
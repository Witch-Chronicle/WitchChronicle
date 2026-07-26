using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 입력 컨트롤러
/// 입력 순서와 타이밍 판정 관리
/// </summary>
public class ConstellationInputController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField]
    private ConstellationSequenceController _sequenceController;

    [SerializeField]
    private ConstellationUIController _uiController;

    private readonly List<ConstellationBeatResult> _beatResults =
        new List<ConstellationBeatResult>();

    private ConstellationSequenceData _sequenceData;
    private int _nextBeatIndex;
    private bool _isRunning;

    public event Action<ConstellationBeatResult> OnBeatJudged;
    public event Action<ConstellationResult> OnConstellationCompleted;

    /// <summary>
    /// 입력과 시퀀스 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_sequenceController != null)
        {
            _sequenceController.OnSequenceStarted +=
                HandleSequenceStarted;

            _sequenceController.OnSequenceCompleted +=
                HandleSequenceCompleted;
        }

        if (_uiController != null)
        {
            _uiController.OnStarClicked +=
                HandleStarClicked;
        }
    }

    /// <summary>
    /// 입력과 시퀀스 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_sequenceController != null)
        {
            _sequenceController.OnSequenceStarted -=
                HandleSequenceStarted;

            _sequenceController.OnSequenceCompleted -=
                HandleSequenceCompleted;
        }

        if (_uiController != null)
        {
            _uiController.OnStarClicked -=
                HandleStarClicked;
        }
    }

    /// <summary>
    /// 현재 Beat 시간 초과 검사
    /// </summary>
    private void Update()
    {
        if (!_isRunning ||
            _sequenceData == null ||
            _nextBeatIndex >= _sequenceData.BeatCount)
        {
            return;
        }

        ConstellationBeatData currentBeat =
            _sequenceData.Beats[_nextBeatIndex];

        float missTime =
            currentBeat.ImpactTime +
            currentBeat.GoodWindow;

        if (_sequenceController.ElapsedTime <= missTime)
        {
            return;
        }

        RegisterMiss(_nextBeatIndex);
    }

    /// <summary>
    /// 시퀀스 시작 입력 상태 초기화
    /// </summary>
    private void HandleSequenceStarted(
        ConstellationSequenceData sequenceData)
    {
        _sequenceData = sequenceData;

        _beatResults.Clear();

        _nextBeatIndex = 0;
        _isRunning = true;
    }

    /// <summary>
    /// 별 클릭 순서와 타이밍 판정
    /// </summary>
    private void HandleStarClicked(int beatIndex)
    {
        if (!_isRunning ||
            _sequenceData == null)
        {
            return;
        }

        if (beatIndex != _nextBeatIndex)
        {
            Debug.Log(
                $"잘못된 입력 순서: {beatIndex + 1}번 별" +
                $"\n현재 입력 대상: {_nextBeatIndex + 1}번 별",
                this);

            return;
        }

        ConstellationBeatData beat =
            _sequenceData.Beats[beatIndex];

        float timingError =
            _sequenceController.ElapsedTime -
            beat.ImpactTime;

        ConstellationJudgementType judgement =
            CalculateJudgement(
                Mathf.Abs(timingError),
                beat);

        float score =
            CalculateScore(
                Mathf.Abs(timingError),
                beat,
                judgement);

        RegisterResult(
            beatIndex,
            judgement,
            timingError,
            score);
    }

    /// <summary>
    /// 시간 오차 기반 판정 계산
    /// </summary>
    private ConstellationJudgementType CalculateJudgement(
        float absoluteTimingError,
        ConstellationBeatData beat)
    {
        if (absoluteTimingError <= beat.PerfectWindow)
        {
            return ConstellationJudgementType.Perfect;
        }

        if (absoluteTimingError <= beat.GoodWindow)
        {
            return ConstellationJudgementType.Good;
        }

        return ConstellationJudgementType.Miss;
    }

    /// <summary>
    /// 시간 오차 기반 점수 계산
    /// </summary>
    private float CalculateScore(
        float absoluteTimingError,
        ConstellationBeatData beat,
        ConstellationJudgementType judgement)
    {
        switch (judgement)
        {
            case ConstellationJudgementType.Perfect:
                {
                    float perfectProgress =
                        Mathf.InverseLerp(
                            0f,
                            beat.PerfectWindow,
                            absoluteTimingError);

                    return Mathf.Lerp(
                        100f,
                        90f,
                        perfectProgress);
                }

            case ConstellationJudgementType.Good:
                {
                    float goodProgress =
                        Mathf.InverseLerp(
                            beat.PerfectWindow,
                            beat.GoodWindow,
                            absoluteTimingError);

                    return Mathf.Lerp(
                        89f,
                        50f,
                        goodProgress);
                }

            default:
                return 0f;
        }
    }

    /// <summary>
    /// 시간 초과 Miss 등록
    /// </summary>
    private void RegisterMiss(int beatIndex)
    {
        ConstellationBeatData beat =
            _sequenceData.Beats[beatIndex];

        float timingError =
            _sequenceController.ElapsedTime -
            beat.ImpactTime;

        RegisterResult(
            beatIndex,
            ConstellationJudgementType.Miss,
            timingError,
            0f);
    }

    /// <summary>
    /// 단일 Beat 판정 결과 등록
    /// </summary>
    private void RegisterResult(
        int beatIndex,
        ConstellationJudgementType judgement,
        float timingError,
        float score)
    {
        ConstellationBeatResult result =
            new ConstellationBeatResult(
                beatIndex,
                judgement,
                timingError,
                score);

        _beatResults.Add(result);

        _uiController.ResolveStar(beatIndex);

        _nextBeatIndex++;

        Debug.Log(
            $"Beat {beatIndex} 판정: {judgement}" +
            $"\n시간 오차: {timingError:+0.000;-0.000;0.000}초" +
            $"\n점수: {score:F1}",
            this);

        OnBeatJudged?.Invoke(result);
    }

    /// <summary>
    /// 시퀀스 종료 및 최종 결과 생성
    /// </summary>
    private void HandleSequenceCompleted(
        ConstellationSequenceData sequenceData)
    {
        if (!_isRunning)
        {
            return;
        }

        while (_nextBeatIndex < sequenceData.BeatCount)
        {
            RegisterMiss(_nextBeatIndex);
        }

        _isRunning = false;

        ConstellationResult result =
            BuildFinalResult();

        Debug.Log(
            $"별자리 최종 결과" +
            $"\n성공 여부: {result.IsSuccess}" +
            $"\n최종 점수: {result.Score:F1}",
            this);

        OnConstellationCompleted?.Invoke(result);
    }

    /// <summary>
    /// 전체 Beat 기반 최종 결과 생성
    /// </summary>
    private ConstellationResult BuildFinalResult()
    {
        if (_beatResults.Count == 0)
        {
            return new ConstellationResult(
                false,
                0f,
                Array.Empty<ConstellationBeatResult>());
        }

        float totalScore = 0f;
        bool isSuccess = true;

        for (int i = 0; i < _beatResults.Count; i++)
        {
            ConstellationBeatResult result =
                _beatResults[i];

            totalScore += result.Score;

            if (!result.IsSuccess)
            {
                isSuccess = false;
            }
        }

        float averageScore =
            totalScore / _beatResults.Count;

        return new ConstellationResult(
            isSuccess,
            averageScore,
            new List<ConstellationBeatResult>(_beatResults));
    }
}
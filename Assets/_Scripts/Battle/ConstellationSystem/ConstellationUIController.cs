using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 UI 컨트롤러
/// 시퀀스 이벤트 기반 별 생성과 입력 전달
/// </summary>
public class ConstellationUIController : MonoBehaviour
{
    [Header("Sequence")]
    [SerializeField]
    private ConstellationSequenceController _sequenceController;

    [Header("UI")]
    [SerializeField] private RectTransform _constellationPanel;
    [SerializeField] private ConstellationStarView _starPrefab;

    [Header("Connection Line")]
    [SerializeField] private RectTransform _lineContainer;
    [SerializeField] private ConstellationLineView _linePrefab;

    [Header("Completion")]
    [SerializeField, Min(0f)]
    private float _successCompletionDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float _failureHoldDuration = 0.25f;

    private readonly Dictionary<int, ConstellationStarView> _starViews =
        new Dictionary<int, ConstellationStarView>();

    private readonly List<ConstellationLineView> _lineViews =
    new List<ConstellationLineView>();

    private readonly Dictionary<
        int,
        ConstellationJudgementType> _judgements =
        new Dictionary<
            int,
            ConstellationJudgementType>();

    private ConstellationSequenceData _currentSequenceData;

    private bool _hasPlayedCompletionFlash;

    private Coroutine _completionRoutine;

    public event Action<int> OnStarClicked;
    public event Action OnCompletionPresentationFinished;
    public event Action OnSuccessFlashStarted;

    /// <summary>
    /// 시퀀스 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_sequenceController == null)
        {
            return;
        }

        _sequenceController.OnSequenceStarted +=
            HandleSequenceStarted;

        _sequenceController.OnStarShowRequested +=
            HandleStarShowRequested;

        _sequenceController.OnImpactReached +=
            HandleImpactReached;

        _sequenceController.OnSequenceCompleted +=
            HandleSequenceCompleted;
    }

    /// <summary>
    /// 시퀀스 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_sequenceController != null)
        {
            _sequenceController.OnSequenceStarted -=
                HandleSequenceStarted;

            _sequenceController.OnStarShowRequested -=
                HandleStarShowRequested;

            _sequenceController.OnImpactReached -=
                HandleImpactReached;

            _sequenceController.OnSequenceCompleted -=
                HandleSequenceCompleted;
        }

        ClearStars();
        ClearLines();

        _judgements.Clear();
    }

    /// <summary>
    /// 시퀀스 시작 UI 초기화
    /// </summary>
    /// <param name="sequenceData">시작 시퀀스 데이터</param>
    private void HandleSequenceStarted(
        ConstellationSequenceData sequenceData)
    {
        StopCompletionPresentation();

        _currentSequenceData =
            sequenceData;

        _hasPlayedCompletionFlash =
            false;

        ClearStars();
        ClearLines();

        _judgements.Clear();

        if (_lineContainer != null)
        {
            _lineContainer.SetAsFirstSibling();
        }
    }

    /// <summary>
    /// 별 표시 요청 처리
    /// </summary>
    private void HandleStarShowRequested(
        int beatIndex,
        ConstellationBeatData beat)
    {
        CreateStar(beatIndex, beat);
    }

    /// <summary>
    /// 투사체 충돌 시점 처리
    /// </summary>
    private void HandleImpactReached(
        int beatIndex,
        ConstellationBeatData beat)
    {
        if (!_starViews.TryGetValue(
                beatIndex,
                out ConstellationStarView starView))
        {
            return;
        }

        starView.ReachImpact();
    }

    /// <summary>
    /// 시퀀스 종료 별 상태 유지
    /// </summary>
    /// <param name="sequenceData">종료 시퀀스 데이터</param>
    private void HandleSequenceCompleted(
        ConstellationSequenceData sequenceData)
    {
        // 최종 별자리 연출을 위해 현재 별 유지
    }

    /// <summary>
    /// 지정 위치에 별 생성
    /// </summary>
    private void CreateStar(
        int beatIndex,
        ConstellationBeatData beat)
    {
        if (_constellationPanel == null ||
            _starPrefab == null)
        {
            Debug.LogWarning(
                "별자리 UI 참조가 연결되지 않았습니다.",
                this);

            return;
        }

        if (_starViews.ContainsKey(beatIndex))
        {
            return;
        }

        ConstellationStarView starView =
            Instantiate(
                _starPrefab,
                _constellationPanel);

        RectTransform starRectTransform =
            starView.GetComponent<RectTransform>();

        Vector2 normalizedPosition =
            new Vector2(
                Mathf.Clamp01(
                    beat.NormalizedStarPosition.x),
                Mathf.Clamp01(
                    beat.NormalizedStarPosition.y));

        starRectTransform.anchorMin =
            normalizedPosition;

        starRectTransform.anchorMax =
            normalizedPosition;

        starRectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        starRectTransform.anchoredPosition =
            Vector2.zero;

        starView.Initialize(
            beatIndex,
            beat,
            _sequenceController);

        starView.OnClicked +=
            HandleStarClicked;

        _starViews.Add(
            beatIndex,
            starView);
    }

    /// <summary>
    /// 별 클릭 이벤트 전달
    /// </summary>
    private void HandleStarClicked(int beatIndex)
    {
        OnStarClicked?.Invoke(beatIndex);
    }

    /// <summary>
    /// 지정 별 판정 결과 반영
    /// </summary>
    /// <param name="beatIndex">박자 인덱스</param>
    /// <param name="judgement">판정 결과</param>
    public void ResolveStar(
        int beatIndex,
        ConstellationJudgementType judgement)
    {
        if (!_starViews.TryGetValue(
                beatIndex,
                out ConstellationStarView starView))
        {
            return;
        }

        _judgements[beatIndex] =
            judgement;

        starView.OnClicked -=
            HandleStarClicked;

        starView.Resolve(judgement);

        if (judgement ==
            ConstellationJudgementType.Miss)
        {
            return;
        }

        // 마지막 연결선 생성 후 전체 섬광 판정
        TryCreateConnectionLine(
            beatIndex);

        TryPlayImmediateCompletionFlash(
            beatIndex);
    }

    /// <summary>
    /// 현재 생성된 별 전체 제거
    /// </summary>
    private void ClearStars()
    {
        foreach (
            ConstellationStarView starView
            in _starViews.Values)
        {
            if (starView == null)
            {
                continue;
            }

            starView.OnClicked -=
                HandleStarClicked;

            Destroy(starView.gameObject);
        }

        _starViews.Clear();
    }

    /// <summary>
    /// 이전 성공 별과 현재 별 연결 시도
    /// </summary>
    /// <param name="beatIndex">현재 박자 인덱스</param>
    private void TryCreateConnectionLine(
        int beatIndex)
    {
        int previousBeatIndex =
            beatIndex - 1;

        if (previousBeatIndex < 0)
        {
            return;
        }

        if (!_judgements.TryGetValue(
                previousBeatIndex,
                out ConstellationJudgementType previousJudgement))
        {
            return;
        }

        if (previousJudgement ==
            ConstellationJudgementType.Miss)
        {
            return;
        }

        if (!_starViews.TryGetValue(
                previousBeatIndex,
                out ConstellationStarView previousStar))
        {
            return;
        }

        if (!_starViews.TryGetValue(
                beatIndex,
                out ConstellationStarView currentStar))
        {
            return;
        }

        CreateConnectionLine(
            previousStar,
            currentStar);
    }

    /// <summary>
    /// 두 성공 별 사이 연결선 생성
    /// </summary>
    /// <param name="startStar">시작 별</param>
    /// <param name="endStar">종료 별</param>
    private void CreateConnectionLine(
        ConstellationStarView startStar,
        ConstellationStarView endStar)
    {
        if (_lineContainer == null ||
            _linePrefab == null ||
            startStar == null ||
            endStar == null)
        {
            return;
        }

        ConstellationLineView lineView =
            Instantiate(
                _linePrefab,
                _lineContainer);

        lineView.Initialize(
            startStar.RectTransform,
            endStar.RectTransform,
            _lineContainer);

        _lineViews.Add(lineView);
    }

    /// <summary>
    /// 현재 연결선 전체 제거
    /// </summary>
    private void ClearLines()
    {
        for (int i = 0;
             i < _lineViews.Count;
             i++)
        {
            ConstellationLineView lineView =
                _lineViews[i];

            if (lineView == null)
            {
                continue;
            }

            Destroy(lineView.gameObject);
        }

        _lineViews.Clear();
    }

    /// <summary>
    /// 별자리 최종 결과 연출 시작
    /// </summary>
    /// <param name="result">별자리 최종 결과</param>
    public void PlayCompletionPresentation(
        ConstellationResult result)
    {
        StopCompletionPresentation();

        _completionRoutine =
            StartCoroutine(
                PlayCompletionPresentationRoutine(
                    result));
    }

    /// <summary>
    /// 별자리 최종 결과 연출 정지
    /// </summary>
    public void StopCompletionPresentation()
    {
        if (_completionRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _completionRoutine);

        _completionRoutine = null;
    }

    /// <summary>
    /// 별자리 최종 결과 연출 진행
    /// </summary>
    /// <param name="result">별자리 최종 결과</param>
    private IEnumerator PlayCompletionPresentationRoutine(
        ConstellationResult result)
    {
        if (result.IsSuccess)
        {
            // 즉시 섬광이 실행되지 않은 경우의 안전 처리
            if (!_hasPlayedCompletionFlash)
            {
                _hasPlayedCompletionFlash = true;

                PlayCompletionFlashOnAll();

                OnSuccessFlashStarted?.Invoke();
            }

            yield return new WaitForSecondsRealtime(
                _successCompletionDuration);
        }
        else
        {
            yield return new WaitForSecondsRealtime(
                _failureHoldDuration);
        }

        _completionRoutine = null;

        OnCompletionPresentationFinished?.Invoke();
    }

    /// <summary>
    /// 마지막 성공 입력 즉시 완성 섬광 실행
    /// </summary>
    /// <param name="beatIndex">판정 완료 박자 인덱스</param>
    private void TryPlayImmediateCompletionFlash(
        int beatIndex)
    {
        if (_hasPlayedCompletionFlash)
        {
            return;
        }

        if (_currentSequenceData == null)
        {
            return;
        }

        int lastBeatIndex =
            _currentSequenceData.BeatCount - 1;

        if (beatIndex != lastBeatIndex)
        {
            return;
        }

        for (int i = 0;
             i < _currentSequenceData.BeatCount;
             i++)
        {
            if (!_judgements.TryGetValue(
                    i,
                    out ConstellationJudgementType judgement))
            {
                return;
            }

            if (judgement ==
                ConstellationJudgementType.Miss)
            {
                return;
            }
        }

        _hasPlayedCompletionFlash = true;

        PlayCompletionFlashOnAll();

        OnSuccessFlashStarted?.Invoke();
    }

    /// <summary>
    /// 전체 별과 연결선 완성 섬광 실행
    /// </summary>
    private void PlayCompletionFlashOnAll()
    {
        foreach (
            ConstellationStarView starView
            in _starViews.Values)
        {
            if (starView == null)
            {
                continue;
            }

            starView.PlayCompletionFlash();
        }

        for (int i = 0;
             i < _lineViews.Count;
             i++)
        {
            ConstellationLineView lineView =
                _lineViews[i];

            if (lineView == null)
            {
                continue;
            }

            lineView.PlayCompletionFlash();
        }
    }
}
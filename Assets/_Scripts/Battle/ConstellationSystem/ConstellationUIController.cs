using System;
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

    private readonly Dictionary<int, ConstellationStarView> _starViews =
        new Dictionary<int, ConstellationStarView>();

    public event Action<int> OnStarClicked;

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
    }

    /// <summary>
    /// 시퀀스 시작 UI 초기화
    /// </summary>
    private void HandleSequenceStarted(
        ConstellationSequenceData sequenceData)
    {
        ClearStars();
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
    /// 시퀀스 종료 UI 정리
    /// </summary>
    private void HandleSequenceCompleted(
        ConstellationSequenceData sequenceData)
    {
        ClearStars();
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
    /// 지정 별 판정 완료
    /// </summary>
    public void ResolveStar(int beatIndex)
    {
        if (!_starViews.TryGetValue(
                beatIndex,
                out ConstellationStarView starView))
        {
            return;
        }

        starView.OnClicked -=
            HandleStarClicked;

        starView.Resolve();

        _starViews.Remove(beatIndex);
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
}
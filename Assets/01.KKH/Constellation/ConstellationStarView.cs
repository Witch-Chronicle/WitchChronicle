using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 별자리 단일 별 UI
/// 입력 목표와 접근 링 시각화
/// </summary>
public class ConstellationStarView :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private RectTransform _approachRing;
    [SerializeField] private TMP_Text _orderText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Approach")]
    [SerializeField, Min(1f)] private float _ringStartScale = 2.5f;
    [SerializeField, Min(0f)] private float _resolveDisplayDuration = 0.1f;

    private ConstellationSequenceController _sequenceController;
    private ConstellationBeatData _beatData;
    private bool _isApproaching;
    private bool _isResolved;

    public int BeatIndex { get; private set; }

    public event Action<int> OnClicked;

    /// <summary>
    /// 접근 링 크기 갱신
    /// </summary>
    private void Update()
    {
        if (!_isApproaching ||
            _sequenceController == null ||
            _beatData == null)
        {
            return;
        }

        UpdateApproachRing();
    }

    /// <summary>
    /// 포인터 클릭 전달
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isResolved)
        {
            return;
        }

        OnClicked?.Invoke(BeatIndex);
    }

    /// <summary>
    /// 별 UI 초기화
    /// </summary>
    public void Initialize(
        int beatIndex,
        ConstellationBeatData beatData,
        ConstellationSequenceController sequenceController)
    {
        BeatIndex = beatIndex;

        _beatData = beatData;
        _sequenceController = sequenceController;

        _isApproaching = true;
        _isResolved = false;

        if (_orderText != null)
        {
            _orderText.text = (beatIndex + 1).ToString();
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        if (_approachRing != null)
        {
            _approachRing.localScale =
                Vector3.one * _ringStartScale;
        }
    }

    /// <summary>
    /// 투사체 충돌 시점 도달 처리
    /// </summary>
    public void ReachImpact()
    {
        _isApproaching = false;

        if (_approachRing != null)
        {
            _approachRing.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// 별 판정 완료 처리
    /// </summary>
    public void Resolve()
    {
        if (_isResolved)
        {
            return;
        }

        _isResolved = true;
        _isApproaching = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        Destroy(gameObject, _resolveDisplayDuration);
    }

    /// <summary>
    /// 접근 링 진행도 반영
    /// </summary>
    private void UpdateApproachRing()
    {
        if (_approachRing == null)
        {
            return;
        }

        float remainingTime =
            _beatData.ImpactTime -
            _sequenceController.ElapsedTime;

        float normalizedRemainingTime =
            remainingTime / _beatData.StarLeadTime;

        float progress =
            1f - Mathf.Clamp01(normalizedRemainingTime);

        float currentScale =
            Mathf.Lerp(
                _ringStartScale,
                1f,
                progress);

        _approachRing.localScale =
            Vector3.one * currentScale;
    }
}
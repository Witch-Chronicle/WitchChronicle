using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// TransitionPanel 전담. SystemUI(DontDestroyOnLoad) 하위에 위치해서 모든 씬 전환에 공통으로 사용됨.
/// - CoverScreen(): 화면 공개 상태 -> 전부 오른쪽에서 들어와 화면을 덮음
/// - RevealScreen(): 덮인 상태 -> 전부 왼쪽으로 사라지며 화면 공개
/// </summary>
public class TransitionController : MonoBehaviour
{
    [Header("Raycast 차단용 (선택)")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Strips (위에서 아래 순서로 등록)")]
    [SerializeField] private List<RectTransform> _strips = new List<RectTransform>();

    [Header("Animation")]
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _maxStaggerDelay = 0.25f;
    [SerializeField] private Ease _ease = Ease.InOutQuad;

    private readonly List<float> _visiblePosX = new List<float>();
    private readonly List<float> _leftHiddenPosX = new List<float>();
    private readonly List<float> _rightHiddenPosX = new List<float>();

    private bool _isInitialized;
    private bool _isCovered; // 현재 화면이 덮여있는 상태인지 (중복 호출 방지용)

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        foreach (var strip in _strips)
        {
            if (strip == null)
            {
                _visiblePosX.Add(0f);
                _leftHiddenPosX.Add(0f);
                _rightHiddenPosX.Add(0f);
                continue;
            }

            float visibleX = strip.anchoredPosition.x;
            float width = strip.rect.width;

            _visiblePosX.Add(visibleX);
            _leftHiddenPosX.Add(visibleX - width);
            _rightHiddenPosX.Add(visibleX + width);
        }

        _isInitialized = true;
    }

    /// <summary>
    /// 화면을 덮음 (씬 전환 시작 전 호출). 완료되면 onComplete 호출.
    /// </summary>
    public void CoverScreen(System.Action onComplete = null)
    {
        EnsureInitialized();

        if (_isCovered)
        {
            onComplete?.Invoke();
            return;
        }

        _isCovered = true;

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        RunStripAnimation(_leftHiddenPosX, _visiblePosX, onComplete, setStartImmediate: true);
    }

    /// <summary>
    /// 화면을 공개 (씬 로드 완료 후 호출). 완료되면 onComplete 호출.
    /// </summary>
    public void RevealScreen(System.Action onComplete = null)
    {
        EnsureInitialized();

        if (_isCovered == false)
        {
            onComplete?.Invoke();
            return;
        }

        _isCovered = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        RunStripAnimation(_visiblePosX, _rightHiddenPosX, onComplete, setStartImmediate: false);
    }

    private void RunStripAnimation(
        List<float> startPosXOverride,
        List<float> targetPosX,
        System.Action onComplete,
        bool setStartImmediate)
    {
        if (_strips.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int remaining = _strips.Count;

        void HandleStripComplete()
        {
            remaining--;
            if (remaining <= 0)
            {
                onComplete?.Invoke();
            }
        }

        for (int i = 0; i < _strips.Count; i++)
        {
            RectTransform strip = _strips[i];

            if (strip == null)
            {
                HandleStripComplete();
                continue;
            }

            strip.DOKill();

            if (setStartImmediate)
            {
                SetStripPosXImmediate(i, startPosXOverride[i]);
            }

            strip.DOAnchorPosX(targetPosX[i], _duration)
                .SetEase(_ease)
                .SetDelay(Random.Range(0f, _maxStaggerDelay))
                .OnComplete(HandleStripComplete);
        }
    }

    private void SetStripPosXImmediate(int index, float posX)
    {
        RectTransform strip = _strips[index];
        if (strip == null) return;

        strip.anchoredPosition = new Vector2(posX, strip.anchoredPosition.y);
    }

    /// <summary>
    /// 애니메이션 없이 즉시 "덮인" 상태로 세팅. Boot 씬처럼 씬 로드 전에
    /// 미리 화면을 덮어두고 싶을 때 사용 (RevealScreen()으로 자연스럽게 걷어낼 수 있게).
    /// </summary>
    public void SetCoveredImmediate()
    {
        EnsureInitialized();

        _isCovered = true;

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        for (int i = 0; i < _strips.Count; i++)
        {
            SetStripPosXImmediate(i, _visiblePosX[i]);
        }
    }
}
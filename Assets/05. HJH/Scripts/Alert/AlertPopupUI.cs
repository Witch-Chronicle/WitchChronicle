using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 개별 Alert Popup의 표시, 이동, 수명, 종료 연출을 담당합니다.
/// </summary>
public class AlertPopupUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _alertTxt;

    private Sequence _lifeSequence;
    private Tween _moveTween;
    private Tween _dismissTween;

    private Action<AlertPopupUI> _onLifeTimeExpired;
    private Action<AlertPopupUI> _onDismissCompleted;

    private bool _isDismissing;

    public RectTransform RectTransform => _rectTransform;
    public bool IsDismissing => _isDismissing;

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (_rectTransform == null)
        {
            _rectTransform = transform as RectTransform;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_alertTxt == null)
        {
            _alertTxt = GetComponentInChildren<TMP_Text>();
        }
    }

    /// <summary>
    /// 풀에서 꺼낸 Popup을 초기화합니다.
    /// </summary>
    public void Initialize(
        AlertRequest request,
        Vector2 startPosition,
        Vector2 targetPosition,
        float enterDuration,
        float enterOffset,
        Ease enterEase,
        Action<AlertPopupUI> onLifeTimeExpired)
    {
        ResolveReferences();
        KillTweens();

        _isDismissing = false;
        _onLifeTimeExpired = onLifeTimeExpired;
        _onDismissCompleted = null;

        if (_alertTxt != null)
        {
            _alertTxt.text = request.Message;
        }

        gameObject.SetActive(true);

        if (_rectTransform != null)
        {
            Vector2 enterStartPosition =
                startPosition + Vector2.up * enterOffset;

            _rectTransform.anchoredPosition = enterStartPosition;
            _rectTransform.localScale = Vector3.one;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        _lifeSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (_rectTransform != null)
        {
            _lifeSequence.Join(
                _rectTransform
                    .DOAnchorPos(targetPosition, enterDuration)
                    .SetEase(enterEase)
            );
        }

        if (_canvasGroup != null)
        {
            _lifeSequence.Join(
                _canvasGroup
                    .DOFade(1f, enterDuration)
                    .SetEase(Ease.OutQuad)
            );
        }

        /*
         * LifeTime은 등장 애니메이션이 끝난 뒤부터 계산합니다.
         */
        _lifeSequence.AppendInterval(
            Mathf.Max(0.1f, request.LifeTime)
        );

        _lifeSequence.OnComplete(() =>
        {
            _lifeSequence = null;
            _onLifeTimeExpired?.Invoke(this);
        });
    }

    /// <summary>
    /// 기존 팝업을 새로운 슬롯 위치로 이동시킵니다.
    /// </summary>
    public void MoveTo(
        Vector2 targetPosition,
        float duration,
        Ease ease)
    {
        if (_isDismissing || _rectTransform == null)
        {
            return;
        }

        _moveTween?.Kill();

        _moveTween = _rectTransform
            .DOAnchorPos(targetPosition, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => _moveTween = null);
    }

    /// <summary>
    /// LifeTime 종료 또는 최대 개수 초과로 Popup을 제거합니다.
    /// </summary>
    public void Dismiss(
        float fadeOutDuration,
        float exitMoveDistance,
        Ease exitEase,
        Action<AlertPopupUI> onDismissCompleted)
    {
        if (_isDismissing)
        {
            return;
        }

        _isDismissing = true;
        _onDismissCompleted = onDismissCompleted;

        _lifeSequence?.Kill();
        _lifeSequence = null;

        _moveTween?.Kill();
        _moveTween = null;

        _dismissTween?.Kill();

        Sequence dismissSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (_canvasGroup != null)
        {
            dismissSequence.Join(
                _canvasGroup
                    .DOFade(0f, fadeOutDuration)
                    .SetEase(Ease.InQuad)
            );
        }

        if (_rectTransform != null && exitMoveDistance != 0f)
        {
            Vector2 exitPosition =
                _rectTransform.anchoredPosition +
                Vector2.down * exitMoveDistance;

            dismissSequence.Join(
                _rectTransform
                    .DOAnchorPos(exitPosition, fadeOutDuration)
                    .SetEase(exitEase)
            );
        }

        _dismissTween = dismissSequence;

        dismissSequence.OnComplete(() =>
        {
            _dismissTween = null;
            _onDismissCompleted?.Invoke(this);
        });
    }

    /// <summary>
    /// 풀로 반환될 때 Popup 상태를 초기화합니다.
    /// </summary>
    public void ResetPopup()
    {
        KillTweens();

        _isDismissing = false;
        _onLifeTimeExpired = null;
        _onDismissCompleted = null;

        if (_alertTxt != null)
        {
            _alertTxt.text = string.Empty;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale = Vector3.one;
        }

        gameObject.SetActive(false);
    }

    private void KillTweens()
    {
        _lifeSequence?.Kill();
        _lifeSequence = null;

        _moveTween?.Kill();
        _moveTween = null;

        _dismissTween?.Kill();
        _dismissTween = null;

        if (_rectTransform != null)
        {
            _rectTransform.DOKill();
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.DOKill();
        }
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
using System;
using UnityEngine;
using DG.Tweening;

public class UIPanelAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float _duration = 0.15f;
    [SerializeField] private Vector3 _closedScale = new Vector3(0.9f, 0.9f, 1f);

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Tween _scaleTween;
    private Tween _fadeTween;
    private bool _isInitialized;

    public bool IsOpen { get; private set; }

    /// <summary>
    /// 이 패널이 완전히 닫혔을 때 호출됨 (Close() 애니메이션 완료 시점, SetClosedImmediate 호출 시점 둘 다).
    /// 다른 UI가 "이 패널이 닫히면 나도 같이 정리해야 하는" 경우 구독.
    /// </summary>
    public event Action OnClosed;

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _isInitialized = true;
    }

    public void Open()
    {
        EnsureInitialized();

        IsOpen = true;
        KillTweens();

        gameObject.SetActive(true);
        _rectTransform.localScale = _closedScale;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _scaleTween = _rectTransform.DOScale(Vector3.one, _duration).SetEase(Ease.Linear).SetUpdate(true);
        _fadeTween = _canvasGroup.DOFade(1f, _duration).SetEase(Ease.Linear).SetUpdate(true)
            .OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            });
    }

    public void Close()
    {
        EnsureInitialized();

        IsOpen = false;
        KillTweens();

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _scaleTween = _rectTransform.DOScale(_closedScale, _duration).SetEase(Ease.Linear).SetUpdate(true);
        _fadeTween = _canvasGroup.DOFade(0f, _duration).SetEase(Ease.Linear).SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                OnClosed?.Invoke();
            });
    }



    public void SetClosedImmediate()
    {
        EnsureInitialized();

        IsOpen = false;
        KillTweens();

        _rectTransform.localScale = _closedScale;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);

        OnClosed?.Invoke();
    }

    private void KillTweens()
    {
        _scaleTween?.Kill();
        _fadeTween?.Kill();
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
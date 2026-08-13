using System;
using UnityEngine;
using DG.Tweening;

public class UIPanelAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float _duration = 0.15f;

    private CanvasGroup _canvasGroup;
    private Tween _fadeTween;
    private bool _isInitialized;

    public bool IsOpen { get; private set; }

    /// <summary>
    /// 패널이 완전히 닫혔을 때 호출됩니다.
    /// Close 애니메이션 완료 시점과 SetClosedImmediate 호출 시점에 실행됩니다.
    /// </summary>
    public event Action OnClosed;

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
        {
            Debug.LogError(
                $"[{nameof(UIPanelAnimator)}] CanvasGroup이 없습니다.",
                this
            );

            enabled = false;
            return;
        }

        _isInitialized = true;
    }

    public void Open()
    {
        EnsureInitialized();

        if (!_isInitialized)
        {
            return;
        }

        IsOpen = true;
        KillTween();

        gameObject.SetActive(true);

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _fadeTween = _canvasGroup
            .DOFade(1f, _duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            });
    }

    public void Close()
    {
        EnsureInitialized();

        if (!_isInitialized)
        {
            return;
        }

        IsOpen = false;
        KillTween();

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _fadeTween = _canvasGroup
            .DOFade(0f, _duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                OnClosed?.Invoke();
            });
    }

    public void SetClosedImmediate()
    {
        EnsureInitialized();

        if (!_isInitialized)
        {
            return;
        }

        IsOpen = false;
        KillTween();

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        gameObject.SetActive(false);

        OnClosed?.Invoke();
    }

    private void KillTween()
    {
        _fadeTween?.Kill();
        _fadeTween = null;
    }

    private void OnDestroy()
    {
        KillTween();
    }
}
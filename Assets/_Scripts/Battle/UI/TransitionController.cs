using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 전체 화면 UI Image + Timing Mask Shader를 이용한 씬 전환 컨트롤러.
/// 기존 공개 함수명과 호출 방식은 유지합니다.
/// </summary>
public class TransitionController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _transitionImage;

    [Header("Animation")]
    [SerializeField] private float _duration = 0.65f;
    [SerializeField] private Ease _coverEase = Ease.InOutCubic;
    [SerializeField] private Ease _revealEase = Ease.InOutCubic;
    [SerializeField] private bool _ignoreTimeScale = true;

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int ModeId = Shader.PropertyToID("_Mode");

    private Material _runtimeMaterial;
    private Tween _activeTween;
    private bool _isInitialized;
    private bool _isCovered;

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_transitionImage == null)
        {
            Debug.LogError("[TransitionController] Transition Image가 연결되지 않았습니다.", this);
            return;
        }

        if (_transitionImage.material == null || _transitionImage.material.shader == null)
        {
            Debug.LogError("[TransitionController] Transition Image에 Mask Material을 연결하세요.", this);
            return;
        }

        // 프로젝트 에셋 Material의 값을 직접 변경하지 않도록 런타임 복사본을 사용합니다.
        _runtimeMaterial = new Material(_transitionImage.material);
        _runtimeMaterial.name = _transitionImage.material.name + " (Runtime)";
        _transitionImage.material = _runtimeMaterial;
        _transitionImage.color = Color.white;

        _isInitialized = true;
    }

    /// <summary>화면을 왼쪽에서 오른쪽 방향으로 덮습니다.</summary>
    public void CoverScreen(Action onComplete = null)
    {
        EnsureInitialized();

        if (_isInitialized == false)
        {
            onComplete?.Invoke();
            return;
        }

        if (_isCovered)
        {
            onComplete?.Invoke();
            return;
        }

        KillCurrentAnimation();
        _isCovered = true;
        SetRaycastBlock(true);
        _transitionImage.enabled = true;

        SetShaderState(mode: 0f, progress: 0f);
        _activeTween = DOTween.To(
                () => _runtimeMaterial.GetFloat(ProgressId),
                value => _runtimeMaterial.SetFloat(ProgressId, value),
                1f,
                _duration)
            .SetEase(_coverEase)
            .SetUpdate(_ignoreTimeScale)
            .OnComplete(() =>
            {
                _runtimeMaterial.SetFloat(ProgressId, 1f);
                _activeTween = null;
                onComplete?.Invoke();
            });
    }

    /// <summary>검정 화면을 왼쪽에서 오른쪽 방향으로 걷어냅니다.</summary>
    public void RevealScreen(Action onComplete = null)
    {
        EnsureInitialized();

        if (_isInitialized == false)
        {
            onComplete?.Invoke();
            return;
        }

        if (_isCovered == false)
        {
            onComplete?.Invoke();
            return;
        }

        KillCurrentAnimation();
        _isCovered = false;
        SetRaycastBlock(true);
        _transitionImage.enabled = true;

        SetShaderState(mode: 1f, progress: 0f);
        _activeTween = DOTween.To(
                () => _runtimeMaterial.GetFloat(ProgressId),
                value => _runtimeMaterial.SetFloat(ProgressId, value),
                1f,
                _duration)
            .SetEase(_revealEase)
            .SetUpdate(_ignoreTimeScale)
            .OnComplete(() =>
            {
                _runtimeMaterial.SetFloat(ProgressId, 1f);
                _transitionImage.enabled = false;
                SetRaycastBlock(false);
                _activeTween = null;
                onComplete?.Invoke();
            });
    }

    /// <summary>애니메이션 없이 즉시 완전한 검정 상태로 설정합니다.</summary>
    public void SetCoveredImmediate()
    {
        EnsureInitialized();
        if (_isInitialized == false) return;

        KillCurrentAnimation();
        _isCovered = true;
        _transitionImage.enabled = true;
        SetShaderState(mode: 0f, progress: 1f);
        SetRaycastBlock(true);
    }

    /// <summary>애니메이션 없이 즉시 완전히 공개된 상태로 설정합니다.</summary>
    public void SetRevealedImmediate()
    {
        EnsureInitialized();
        if (_isInitialized == false) return;

        KillCurrentAnimation();
        _isCovered = false;
        SetShaderState(mode: 0f, progress: 0f);
        _transitionImage.enabled = false;
        SetRaycastBlock(false);
    }

    private void SetShaderState(float mode, float progress)
    {
        _runtimeMaterial.SetFloat(ModeId, mode);
        _runtimeMaterial.SetFloat(ProgressId, progress);
    }

    private void SetRaycastBlock(bool value)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.interactable = value;
        _canvasGroup.blocksRaycasts = value;
    }

    private void KillCurrentAnimation()
    {
        if (_activeTween == null) return;
        _activeTween.Kill(false);
        _activeTween = null;
    }

    private void OnDestroy()
    {
        KillCurrentAnimation();

        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
        }
    }
}

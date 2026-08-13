using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 방어막 시각 연출 관리
/// 생성, 피격 반응, 파괴, 종료 처리
/// </summary>
public class ConstellationPathBarrierPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _anchor;
    [SerializeField] private GameObject _barrierPrefab;
    [SerializeField] private GameObject _breakVfxPrefab;

    [Header("Transform")]
    [SerializeField] private Vector3 _localPosition = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 _barrierScale = new Vector3(2.5f, 2.5f, 2.5f);

    [Header("Create")]
    [SerializeField, Min(0f)] private float _showDelay = 0.05f;
    [SerializeField, Min(0.01f)] private float _showDuration = 0.2f;
    [SerializeField, Min(1f)] private float _showStartScaleMultiplier = 1.25f;

    [Header("Block")]
    [SerializeField, Min(0.01f)] private float _blockDuration = 0.12f;
    [SerializeField, Min(1f)] private float _blockScaleMultiplier = 1.1f;
    [SerializeField, Range(0f, 1f)] private float _blockFlashStrength = 0.9f;
    [SerializeField, ColorUsage(true, true)] private Color _blockFlashColor = Color.white;

    [Header("Break")]
    [SerializeField, Min(0.01f)] private float _breakDuration = 0.18f;
    [SerializeField, Min(1f)] private float _breakScaleMultiplier = 1.08f;
    [SerializeField, Min(0.1f)] private float _breakVfxLifetime = 2f;

    [Header("End")]
    [SerializeField, Min(0.01f)] private float _hideDuration = 0.15f;

    private GameObject _barrierInstance;
    private Coroutine _barrierRoutine;

    private readonly List<RendererColorBinding> _rendererBindings =
        new List<RendererColorBinding>();

    private readonly MaterialPropertyBlock _propertyBlock =
        new MaterialPropertyBlock();

    /// <summary>
    /// Renderer 원본 색상 정보
    /// </summary>
    private sealed class RendererColorBinding
    {
        public Renderer Renderer;
        public int ColorPropertyId;
        public Color BaseColor;
    }

    /// <summary>
    /// 비활성화 시 방어막 즉시 정리
    /// </summary>
    private void OnDisable()
    {
        DestroyBarrierImmediate();
    }

    /// <summary>
    /// 방어막 생성 연출 시작
    /// 알파 증가 및 크기 수축
    /// </summary>
    /// <param name="onComplete">생성 완료 콜백</param>
    public void ShowBarrier(Action onComplete = null)
    {
        if (_barrierPrefab == null)
        {
            onComplete?.Invoke();
            return;
        }

        StopBarrierRoutine();
        DestroyBarrierImmediate();

        Transform parent = _anchor != null
            ? _anchor
            : transform;

        _barrierInstance = Instantiate(
            _barrierPrefab,
            parent);

        _barrierInstance.transform.localPosition =
            _localPosition;

        _barrierInstance.transform.localRotation =
            Quaternion.identity;

        CacheRendererBindings();

        _barrierRoutine =
            StartCoroutine(
                ShowBarrierRoutine(onComplete));
    }

    /// <summary>
    /// 방어 성공 충격 연출
    /// 흰색 플래시 및 크기 펄스
    /// </summary>
    public void PlayBlock()
    {
        if (_barrierInstance == null)
        {
            return;
        }

        StopBarrierRoutine();

        _barrierRoutine =
            StartCoroutine(
                BlockRoutine());
    }

    /// <summary>
    /// 방어막 파괴 연출
    /// 파괴 VFX 및 페이드 아웃
    /// </summary>
    public void BreakBarrier()
    {
        if (_barrierInstance == null)
        {
            return;
        }

        StopBarrierRoutine();

        if (_breakVfxPrefab != null)
        {
            GameObject breakVfx =
                Instantiate(
                    _breakVfxPrefab,
                    _barrierInstance.transform.position,
                    Quaternion.identity);

            Destroy(
                breakVfx,
                _breakVfxLifetime);
        }

        _barrierRoutine =
            StartCoroutine(
                BreakRoutine());
    }

    /// <summary>
    /// 잔여 방어막 종료 연출
    /// 파괴 없이 페이드 아웃
    /// </summary>
    public void HideBarrier()
    {
        if (_barrierInstance == null)
        {
            return;
        }

        StopBarrierRoutine();

        _barrierRoutine =
            StartCoroutine(
                HideRoutine());
    }

    /// <summary>
    /// 방어막 생성 연출
    /// </summary>
    private IEnumerator ShowBarrierRoutine(
        Action onComplete)
    {
        Vector3 startScale =
            _barrierScale *
            _showStartScaleMultiplier;

        _barrierInstance.transform.localScale =
            startScale;

        SetBarrierVisual(
            0f,
            0f);

        if (_showDelay > 0f)
        {
            float delayElapsed = 0f;

            while (delayElapsed < _showDelay)
            {
                delayElapsed +=
                    Time.unscaledDeltaTime;

                yield return null;
            }
        }

        float elapsedTime = 0f;

        while (elapsedTime < _showDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    _showDuration);

            float eased =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f);

            if (_barrierInstance != null)
            {
                _barrierInstance.transform.localScale =
                    Vector3.Lerp(
                        startScale,
                        _barrierScale,
                        eased);

                SetBarrierVisual(
                    eased,
                    0f);
            }

            yield return null;
        }

        if (_barrierInstance != null)
        {
            _barrierInstance.transform.localScale =
                _barrierScale;

            SetBarrierVisual(
                1f,
                0f);
        }

        _barrierRoutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 방어막 피격 펄스 연출
    /// </summary>
    private IEnumerator BlockRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _blockDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    _blockDuration);

            float pulse =
                Mathf.Sin(
                    progress *
                    Mathf.PI);

            if (_barrierInstance != null)
            {
                _barrierInstance.transform.localScale =
                    _barrierScale *
                    Mathf.Lerp(
                        1f,
                        _blockScaleMultiplier,
                        pulse);

                SetBarrierVisual(
                    1f,
                    pulse *
                    _blockFlashStrength);
            }

            yield return null;
        }

        if (_barrierInstance != null)
        {
            _barrierInstance.transform.localScale =
                _barrierScale;

            SetBarrierVisual(
                1f,
                0f);
        }

        _barrierRoutine = null;
    }

    /// <summary>
    /// 방어막 파괴 페이드 연출
    /// </summary>
    private IEnumerator BreakRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _breakDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    _breakDuration);

            if (_barrierInstance != null)
            {
                _barrierInstance.transform.localScale =
                    _barrierScale *
                    Mathf.Lerp(
                        1f,
                        _breakScaleMultiplier,
                        progress);

                SetBarrierVisual(
                    1f - progress,
                    0f);
            }

            yield return null;
        }

        DestroyBarrierImmediate();
    }

    /// <summary>
    /// 방어막 자연 종료 페이드
    /// </summary>
    private IEnumerator HideRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _hideDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    _hideDuration);

            SetBarrierVisual(
                1f - progress,
                0f);

            yield return null;
        }

        DestroyBarrierImmediate();
    }

    /// <summary>
    /// 방어막 Renderer 원본 색상 저장
    /// </summary>
    private void CacheRendererBindings()
    {
        _rendererBindings.Clear();

        if (_barrierInstance == null)
        {
            return;
        }

        Renderer[] renderers =
            _barrierInstance.GetComponentsInChildren<Renderer>(
                true);

        int baseColorId =
            Shader.PropertyToID(
                "_BaseColor");

        int colorId =
            Shader.PropertyToID(
                "_Color");

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer =
                renderers[i];

            Material material =
                renderer.sharedMaterial;

            if (material == null)
            {
                continue;
            }

            int propertyId;

            if (material.HasProperty(baseColorId))
            {
                propertyId = baseColorId;
            }
            else if (material.HasProperty(colorId))
            {
                propertyId = colorId;
            }
            else
            {
                continue;
            }

            RendererColorBinding binding =
                new RendererColorBinding
                {
                    Renderer = renderer,
                    ColorPropertyId = propertyId,
                    BaseColor = material.GetColor(
                        propertyId)
                };

            _rendererBindings.Add(
                binding);
        }
    }

    /// <summary>
    /// 방어막 알파 및 플래시 적용
    /// </summary>
    /// <param name="alpha">표시 알파</param>
    /// <param name="flashStrength">흰색 플래시 강도</param>
    private void SetBarrierVisual(
        float alpha,
        float flashStrength)
    {
        alpha =
            Mathf.Clamp01(alpha);

        flashStrength =
            Mathf.Clamp01(
                flashStrength);

        for (int i = 0; i < _rendererBindings.Count; i++)
        {
            RendererColorBinding binding =
                _rendererBindings[i];

            if (binding.Renderer == null)
            {
                continue;
            }

            Color color =
                Color.Lerp(
                    binding.BaseColor,
                    _blockFlashColor,
                    flashStrength);

            color.a =
                binding.BaseColor.a *
                alpha;

            binding.Renderer.GetPropertyBlock(
                _propertyBlock);

            _propertyBlock.SetColor(
                binding.ColorPropertyId,
                color);

            binding.Renderer.SetPropertyBlock(
                _propertyBlock);
        }
    }

    /// <summary>
    /// 방어막 코루틴 중단
    /// </summary>
    private void StopBarrierRoutine()
    {
        if (_barrierRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _barrierRoutine);

        _barrierRoutine = null;
    }

    /// <summary>
    /// 방어막 즉시 제거
    /// </summary>
    private void DestroyBarrierImmediate()
    {
        StopBarrierRoutine();

        if (_barrierInstance != null)
        {
            Destroy(
                _barrierInstance);

            _barrierInstance = null;
        }

        _rendererBindings.Clear();
    }
}
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 별자리 단일 별 UI
/// 등장, 입력, 판정 상태와 성공 타격 연출
/// </summary>
public class ConstellationStarView :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private RectTransform _visualRoot;
    [SerializeField] private RectTransform _approachRing;
    [SerializeField] private TMP_Text _orderText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _starImage;
    [SerializeField] private Image _glowImage;
    [SerializeField] private Image _burstImage;

    [Header("Sprites")]
    [SerializeField] private Sprite _pendingSprite;
    [SerializeField] private Sprite _successSprite;
    [SerializeField] private Sprite _missSprite;

    [Header("Approach")]
    [SerializeField, Min(1f)]
    private float _ringStartScale = 2.5f;

    [Header("Fade In")]
    [SerializeField, Min(0f)]
    private float _fadeInDuration = 0.18f;

    [SerializeField, Range(0.1f, 1f)]
    private float _fadeInStartScale = 0.8f;

    [Header("Success Impact")]
    [SerializeField, Min(0f)]
    private float _successImpactDuration = 0.22f;

    [SerializeField, Min(1f)]
    private float _perfectPunchScale = 1.45f;

    [SerializeField, Min(1f)]
    private float _goodPunchScale = 1.28f;

    [SerializeField, Min(1f)]
    private float _glowExpandScale = 1.25f;

    [SerializeField, Range(0f, 1f)]
    private float _resolvedGlowAlpha = 0.55f;

    [Header("Success Burst")]
    [SerializeField, Min(0f)]
    private float _burstStartScale = 0.65f;

    [SerializeField, Min(1f)]
    private float _burstEndScale = 1.85f;

    [Header("Success Shake")]
    [SerializeField, Min(0f)]
    private float _shakeDuration = 0.08f;

    [SerializeField, Min(0f)]
    private float _shakeStrength = 7f;

    [Header("Completion Flash")]
    [SerializeField, Min(0f)]
    private float _completionFlashDuration = 0.42f;

    [SerializeField, Min(1)]
    private int _completionPulseCount = 2;

    [SerializeField, Min(1f)]
    private float _completionPulseScale = 1.22f;

    [SerializeField, Min(1f)]
    private float _completionGlowScale = 1.55f;

    private ConstellationSequenceController _sequenceController;
    private ConstellationBeatData _beatData;

    private Coroutine _appearanceRoutine;

    private Vector2 _visualBasePosition;
    private bool _isApproaching;
    private bool _isResolved;

    public int BeatIndex { get; private set; }

    public RectTransform RectTransform =>
        transform as RectTransform;

    public event Action<int> OnClicked;

    /// <summary>
    /// 내부 UI 참조 초기화
    /// </summary>
    private void Awake()
    {
        if (_visualRoot == null)
        {
            _visualRoot =
                transform as RectTransform;
        }
    }

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
    /// 비활성화 시 연출 정지
    /// </summary>
    private void OnDisable()
    {
        StopAppearanceRoutine();
    }

    /// <summary>
    /// 포인터 클릭 전달
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터</param>
    public void OnPointerClick(
        PointerEventData eventData)
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
    /// <param name="beatIndex">박자 인덱스</param>
    /// <param name="beatData">박자 데이터</param>
    /// <param name="sequenceController">시퀀스 컨트롤러</param>
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

        StopAppearanceRoutine();

        if (_visualRoot != null)
        {
            _visualBasePosition =
                _visualRoot.anchoredPosition;

            _visualRoot.localScale =
                Vector3.one * _fadeInStartScale;
        }

        if (_orderText != null)
        {
            _orderText.gameObject.SetActive(true);
            _orderText.text =
                (beatIndex + 1).ToString();
        }

        if (_starImage != null)
        {
            _starImage.sprite =
                _pendingSprite;
        }

        if (_glowImage != null)
        {
            _glowImage.rectTransform.localScale =
                Vector3.one;

            SetImageAlpha(
                _glowImage,
                0f);
        }

        if (_burstImage != null)
        {
            _burstImage.rectTransform.localScale =
                Vector3.one * _burstStartScale;

            SetImageAlpha(
                _burstImage,
                0f);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        if (_approachRing != null)
        {
            _approachRing.gameObject.SetActive(true);

            _approachRing.localScale =
                Vector3.one * _ringStartScale;
        }

        _appearanceRoutine =
            StartCoroutine(PlayFadeIn());
    }

    /// <summary>
    /// 투사체 충돌 시점 처리
    /// </summary>
    public void ReachImpact()
    {
        _isApproaching = false;

        if (_approachRing != null)
        {
            _approachRing.localScale =
                Vector3.one;
        }
    }

    /// <summary>
    /// 별 판정 결과 반영
    /// </summary>
    /// <param name="judgement">판정 결과</param>
    public void Resolve(
        ConstellationJudgementType judgement)
    {
        if (_isResolved)
        {
            return;
        }

        _isResolved = true;
        _isApproaching = false;

        StopAppearanceRoutine();
        ResetVisualTransform();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_approachRing != null)
        {
            _approachRing.gameObject.SetActive(false);
        }

        if (_orderText != null)
        {
            _orderText.gameObject.SetActive(false);
        }

        if (judgement ==
            ConstellationJudgementType.Miss)
        {
            ApplyMissAppearance();
            return;
        }

        ApplySuccessAppearance(
            judgement);
    }

    /// <summary>
    /// 등장 페이드 인 재생
    /// </summary>
    private IEnumerator PlayFadeIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _fadeInDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _fadeInDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _fadeInDuration;

            progress =
                Mathf.Clamp01(progress);

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha =
                    easedProgress;
            }

            if (_visualRoot != null)
            {
                float currentScale =
                    Mathf.Lerp(
                        _fadeInStartScale,
                        1f,
                        easedProgress);

                _visualRoot.localScale =
                    Vector3.one *
                    currentScale;
            }

            yield return null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        ResetVisualTransform();

        _appearanceRoutine = null;
    }

    /// <summary>
    /// 성공 상태 적용
    /// </summary>
    /// <param name="judgement">성공 판정</param>
    private void ApplySuccessAppearance(
        ConstellationJudgementType judgement)
    {
        if (_starImage != null)
        {
            _starImage.sprite =
                _successSprite;
        }

        _appearanceRoutine =
            StartCoroutine(
                PlaySuccessImpact(
                    judgement));
    }

    /// <summary>
    /// 실패 상태 적용
    /// </summary>
    private void ApplyMissAppearance()
    {
        if (_starImage != null)
        {
            _starImage.sprite =
                _missSprite;
        }

        if (_glowImage != null)
        {
            _glowImage.rectTransform.localScale =
                Vector3.one;

            SetImageAlpha(
                _glowImage,
                0f);
        }

        if (_burstImage != null)
        {
            SetImageAlpha(
                _burstImage,
                0f);
        }

        ResetVisualTransform();
    }

    /// <summary>
    /// 성공 타격 연출 재생
    /// </summary>
    /// <param name="judgement">성공 판정</param>
    private IEnumerator PlaySuccessImpact(
        ConstellationJudgementType judgement)
    {
        float elapsedTime = 0f;

        float impactStrength =
            judgement ==
            ConstellationJudgementType.Perfect
                ? 1f
                : 0.72f;

        float punchScale =
            judgement ==
            ConstellationJudgementType.Perfect
                ? _perfectPunchScale
                : _goodPunchScale;

        if (_burstImage != null)
        {
            _burstImage.rectTransform.localScale =
                Vector3.one *
                _burstStartScale;

            SetImageAlpha(
                _burstImage,
                impactStrength);
        }

        while (elapsedTime <
               _successImpactDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _successImpactDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _successImpactDuration;

            progress =
                Mathf.Clamp01(progress);

            float punchProgress =
                Mathf.Sin(
                    progress *
                    Mathf.PI);

            UpdateSuccessPunch(
                punchScale,
                punchProgress);

            UpdateSuccessShake(
                elapsedTime,
                impactStrength);

            UpdateSuccessGlow(
                punchProgress,
                impactStrength);

            UpdateSuccessBurst(
                progress,
                impactStrength);

            yield return null;
        }

        ResetVisualTransform();

        if (_glowImage != null)
        {
            _glowImage.rectTransform.localScale =
                Vector3.one;

            SetImageAlpha(
                _glowImage,
                _resolvedGlowAlpha);
        }

        if (_burstImage != null)
        {
            SetImageAlpha(
                _burstImage,
                0f);
        }

        _appearanceRoutine = null;
    }

    /// <summary>
    /// 성공 펀치 크기 반영
    /// </summary>
    /// <param name="punchScale">최대 크기</param>
    /// <param name="progress">펀치 진행도</param>
    private void UpdateSuccessPunch(
        float punchScale,
        float progress)
    {
        if (_visualRoot == null)
        {
            return;
        }

        float currentScale =
            Mathf.Lerp(
                1f,
                punchScale,
                progress);

        _visualRoot.localScale =
            Vector3.one *
            currentScale;
    }

    /// <summary>
    /// 성공 흔들림 반영
    /// </summary>
    /// <param name="elapsedTime">경과 시간</param>
    /// <param name="impactStrength">연출 강도</param>
    private void UpdateSuccessShake(
        float elapsedTime,
        float impactStrength)
    {
        if (_visualRoot == null)
        {
            return;
        }

        if (_shakeDuration <= 0f ||
            elapsedTime >= _shakeDuration)
        {
            _visualRoot.anchoredPosition =
                _visualBasePosition;

            return;
        }

        float remainingStrength =
            1f -
            elapsedTime /
            _shakeDuration;

        Vector2 shakeOffset =
            UnityEngine.Random.insideUnitCircle *
            _shakeStrength *
            remainingStrength *
            impactStrength;

        _visualRoot.anchoredPosition =
            _visualBasePosition +
            shakeOffset;
    }

    /// <summary>
    /// 성공 발광 반영
    /// </summary>
    /// <param name="progress">발광 진행도</param>
    /// <param name="impactStrength">연출 강도</param>
    private void UpdateSuccessGlow(
        float progress,
        float impactStrength)
    {
        if (_glowImage == null)
        {
            return;
        }

        float glowScale =
            Mathf.Lerp(
                1f,
                _glowExpandScale,
                progress *
                impactStrength);

        _glowImage.rectTransform.localScale =
            Vector3.one *
            glowScale;

        float glowAlpha =
            Mathf.Lerp(
                _resolvedGlowAlpha,
                1f,
                progress *
                impactStrength);

        SetImageAlpha(
            _glowImage,
            glowAlpha);
    }

    /// <summary>
    /// 성공 확산광 반영
    /// </summary>
    /// <param name="progress">확산 진행도</param>
    /// <param name="impactStrength">연출 강도</param>
    private void UpdateSuccessBurst(
        float progress,
        float impactStrength)
    {
        if (_burstImage == null)
        {
            return;
        }

        float burstScale =
            Mathf.Lerp(
                _burstStartScale,
                _burstEndScale,
                progress);

        _burstImage.rectTransform.localScale =
            Vector3.one *
            burstScale;

        float burstAlpha =
            (1f - progress) *
            impactStrength;

        SetImageAlpha(
            _burstImage,
            burstAlpha);
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
            remainingTime /
            _beatData.StarLeadTime;

        float progress =
            1f -
            Mathf.Clamp01(
                normalizedRemainingTime);

        float currentScale =
            Mathf.Lerp(
                _ringStartScale,
                1f,
                progress);

        _approachRing.localScale =
            Vector3.one *
            currentScale;
    }

    /// <summary>
    /// 시각 루트 위치와 크기 초기화
    /// </summary>
    private void ResetVisualTransform()
    {
        if (_visualRoot == null)
        {
            return;
        }

        _visualRoot.anchoredPosition =
            _visualBasePosition;

        _visualRoot.localScale =
            Vector3.one;
    }

    /// <summary>
    /// 이미지 투명도 변경
    /// </summary>
    /// <param name="image">대상 이미지</param>
    /// <param name="alpha">투명도</param>
    private void SetImageAlpha(
        Image image,
        float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color =
            image.color;

        color.a =
            Mathf.Clamp01(alpha);

        image.color =
            color;
    }

    /// <summary>
    /// 현재 외형 코루틴 정지
    /// </summary>
    private void StopAppearanceRoutine()
    {
        if (_appearanceRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _appearanceRoutine);

        _appearanceRoutine = null;
    }

    /// <summary>
    /// 별자리 완성 발광 재생
    /// </summary>
    public void PlayCompletionFlash()
    {
        if (_isResolved == false)
        {
            return;
        }

        StopAppearanceRoutine();
        ResetVisualTransform();

        _appearanceRoutine =
            StartCoroutine(
                PlayCompletionFlashRoutine());
    }

    /// <summary>
    /// 별자리 완성 발광 진행
    /// </summary>
    private IEnumerator PlayCompletionFlashRoutine()
    {
        float elapsedTime = 0f;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        while (elapsedTime < _completionFlashDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _completionFlashDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _completionFlashDuration;

            progress =
                Mathf.Clamp01(progress);

            float pulse =
                Mathf.Sin(
                    progress *
                    Mathf.PI *
                    _completionPulseCount);

            pulse =
                Mathf.Abs(pulse);

            if (_visualRoot != null)
            {
                float currentScale =
                    Mathf.Lerp(
                        1f,
                        _completionPulseScale,
                        pulse);

                _visualRoot.localScale =
                    Vector3.one *
                    currentScale;
            }

            if (_glowImage != null)
            {
                float glowScale =
                    Mathf.Lerp(
                        1f,
                        _completionGlowScale,
                        pulse);

                _glowImage.rectTransform.localScale =
                    Vector3.one *
                    glowScale;

                float glowAlpha =
                    Mathf.Lerp(
                        _resolvedGlowAlpha,
                        1f,
                        pulse);

                SetImageAlpha(
                    _glowImage,
                    glowAlpha);
            }

            if (_burstImage != null)
            {
                float burstScale =
                    Mathf.Lerp(
                        1f,
                        _burstEndScale,
                        pulse);

                _burstImage.rectTransform.localScale =
                    Vector3.one *
                    burstScale;

                SetImageAlpha(
                    _burstImage,
                    pulse);
            }

            yield return null;
        }

        ResetVisualTransform();

        if (_glowImage != null)
        {
            _glowImage.rectTransform.localScale =
                Vector3.one;

            SetImageAlpha(
                _glowImage,
                _resolvedGlowAlpha);
        }

        if (_burstImage != null)
        {
            SetImageAlpha(
                _burstImage,
                0f);
        }

        _appearanceRoutine = null;
    }
}
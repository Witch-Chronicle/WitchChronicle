using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 경로형 별자리 단일 노드 UI
/// 등장, 상태 표시, 클릭과 입력 피드백 관리
/// </summary>
public class ConstellationPathNodeView :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("References")]
    [SerializeField]
    private RectTransform _rectTransform;

    [SerializeField]
    private RectTransform _visualRoot;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private Image _starImage;

    [SerializeField]
    private Image _ambientGlowImage;

    [SerializeField]
    private Image _availableGlowImage;

    [SerializeField]
    private Image _impactFlashImage;

    [Header("Sprites")]
    [SerializeField]
    private Sprite _idleSprite;

    [SerializeField]
    private Sprite _completedSprite;

    [Header("Reveal")]
    [SerializeField, Min(0f)]
    private float _revealDuration = 0.18f;

    [SerializeField, Range(0.1f, 1f)]
    private float _revealStartScale = 0.7f;

    [Header("Locked")]
    [SerializeField, Range(0f, 1f)]
    private float _lockedAmbientAlpha = 0.12f;

    [Header("Available")]
    [SerializeField, Range(0f, 1f)]
    private float _availableAmbientAlpha = 0.22f;

    [SerializeField, Range(0f, 1f)]
    private float _availableGlowMinAlpha = 0.18f;

    [SerializeField, Range(0f, 1f)]
    private float _availableGlowMaxAlpha = 0.42f;

    [SerializeField, Min(0.1f)]
    private float _availablePulseDuration = 1.1f;

    [SerializeField, Min(1f)]
    private float _availablePulseScale = 1.08f;

    [Header("Completed")]
    [SerializeField, Range(0f, 1f)]
    private float _completedAmbientAlpha = 0.4f;

    [Header("Accepted Feedback")]
    [SerializeField, Min(0f)]
    private float _acceptedDuration = 0.18f;

    [SerializeField, Min(1f)]
    private float _acceptedPunchScale = 1.25f;

    [SerializeField, Min(0.1f)]
    private float _impactFlashStartScale = 0.6f;

    [SerializeField, Min(1f)]
    private float _impactFlashEndScale = 1.65f;

    [Header("Invalid Feedback")]
    [SerializeField, Min(0f)]
    private float _invalidDuration = 0.2f;

    [SerializeField, Min(0f)]
    private float _invalidShakeStrength = 8f;

    [SerializeField, Min(1f)]
    private float _invalidShakeFrequency = 5f;

    [SerializeField]
    private Color _invalidColor =
        new Color(1f, 0.25f, 0.35f, 1f);

    [Header("Success Resolution")]
    [SerializeField, Min(0f)]
    private float _resolutionPulseDuration = 0.2f;

    [SerializeField, Min(1f)]
    private float _resolutionPulseScale = 1.3f;

    [SerializeField, Min(0f)]
    private float _successDisappearDuration = 0.38f;

    [SerializeField, Min(1f)]
    private float _successDisappearScale = 1.12f;

    [Header("Failure Resolution")]
    [SerializeField, Min(0f)]
    private float _failureDisappearDuration = 0.36f;

    [SerializeField, Min(0f)]
    private float _failureScatterDistance = 90f;

    [SerializeField, Min(0f)]
    private float _failureRotation = 25f;

    private Coroutine _revealRoutine;
    private Coroutine _pulseRoutine;
    private Coroutine _feedbackRoutine;
    private Coroutine _resolutionRoutine;

    private ConstellationPathNodeState _nodeState;

    private Vector2 _visualBasePosition;
    private Color _starBaseColor = Color.white;

    private bool _isRevealed;
    private bool _interactionEnabled;

    public string NodeId { get; private set; }

    public RectTransform RectTransform =>
        _rectTransform;

    public ConstellationPathNodeState State =>
        _nodeState;

    public event Action<string> OnClicked;

    /// <summary>
    /// 내부 참조와 기본값 초기화
    /// </summary>
    private void Awake()
    {
        if (_rectTransform == null)
        {
            _rectTransform =
                transform as RectTransform;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (_visualRoot != null)
        {
            _visualBasePosition =
                _visualRoot.anchoredPosition;
        }

        if (_starImage != null)
        {
            _starBaseColor =
                _starImage.color;
        }
    }

    /// <summary>
    /// 비활성화 시 연출 정지
    /// </summary>
    private void OnDisable()
    {
        StopRevealRoutine();
        StopPulseRoutine();
        StopFeedbackRoutine();
        StopResolutionRoutine();

        ResetVisualTransform();
    }

    /// <summary>
    /// 포인터 클릭 입력 전달
    /// </summary>
    /// <param name="eventData">포인터 이벤트 데이터</param>
    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (!_isRevealed ||
            !_interactionEnabled)
        {
            return;
        }

        OnClicked?.Invoke(
            NodeId);
    }

    /// <summary>
    /// 노드 UI 초기화
    /// </summary>
    /// <param name="nodeData">노드 데이터</param>
    public void Initialize(
        ConstellationPathNodeData nodeData)
    {
        if (nodeData == null)
        {
            return;
        }

        NodeId =
            nodeData.NodeId;

        _isRevealed = false;

        StopRevealRoutine();
        StopPulseRoutine();
        StopFeedbackRoutine();

        if (_rectTransform != null)
        {
            Vector2 normalizedPosition =
                nodeData.NormalizedPosition;

            _rectTransform.anchorMin =
                normalizedPosition;

            _rectTransform.anchorMax =
                normalizedPosition;

            _rectTransform.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            _rectTransform.anchoredPosition =
                Vector2.zero;

            _rectTransform.localScale =
                Vector3.one *
                _revealStartScale;
        }

        ResetVisualTransform();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        SetImageAlpha(
            _impactFlashImage,
            0f);

        SetInteractionEnabled(
            false);

        SetState(
            ConstellationPathNodeState.Locked);
    }

    /// <summary>
    /// 노드 등장 연출 재생
    /// </summary>
    public void PlayReveal()
    {
        StopRevealRoutine();

        _revealRoutine =
            StartCoroutine(
                PlayRevealRoutine());
    }

    /// <summary>
    /// 노드 상태 변경
    /// </summary>
    /// <param name="nodeState">변경 상태</param>
    public void SetState(
        ConstellationPathNodeState nodeState)
    {
        _nodeState =
            nodeState;

        switch (_nodeState)
        {
            case ConstellationPathNodeState.Locked:
                ApplyLockedAppearance();
                break;

            case ConstellationPathNodeState.Available:
                ApplyAvailableAppearance();
                break;

            case ConstellationPathNodeState.Completed:
                ApplyCompletedAppearance();
                break;
        }
    }

    /// <summary>
    /// 클릭 허용 상태 변경
    /// </summary>
    /// <param name="isEnabled">허용 여부</param>
    public void SetInteractionEnabled(
        bool isEnabled)
    {
        _interactionEnabled =
            isEnabled;

        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.interactable =
            isEnabled;

        _canvasGroup.blocksRaycasts =
            isEnabled;
    }

    /// <summary>
    /// 정상 입력 피드백 재생
    /// </summary>
    public void PlayAcceptedFeedback()
    {
        StopFeedbackRoutine();

        _feedbackRoutine =
            StartCoroutine(
                PlayAcceptedFeedbackRoutine());
    }

    /// <summary>
    /// 잘못된 입력 피드백 재생
    /// </summary>
    public void PlayInvalidFeedback()
    {
        StopFeedbackRoutine();

        _feedbackRoutine =
            StartCoroutine(
                PlayInvalidFeedbackRoutine());
    }

    /// <summary>
    /// 등장 연출 진행
    /// </summary>
    private IEnumerator PlayRevealRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _revealDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _revealDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _revealDuration;

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

            if (_rectTransform != null)
            {
                float currentScale =
                    Mathf.Lerp(
                        _revealStartScale,
                        1f,
                        easedProgress);

                _rectTransform.localScale =
                    Vector3.one *
                    currentScale;
            }

            yield return null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale =
                Vector3.one;
        }

        _isRevealed = true;
        _revealRoutine = null;
    }

    /// <summary>
    /// 정상 입력 타격 연출 진행
    /// </summary>
    private IEnumerator
        PlayAcceptedFeedbackRoutine()
    {
        float elapsedTime = 0f;

        if (_impactFlashImage != null)
        {
            _impactFlashImage
                .rectTransform
                .localScale =
                    Vector3.one *
                    _impactFlashStartScale;

            SetImageAlpha(
                _impactFlashImage,
                1f);
        }

        while (elapsedTime <
               _acceptedDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _acceptedDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _acceptedDuration;

            progress =
                Mathf.Clamp01(progress);

            float punch =
                Mathf.Sin(
                    progress *
                    Mathf.PI);

            if (_visualRoot != null)
            {
                float currentScale =
                    Mathf.Lerp(
                        1f,
                        _acceptedPunchScale,
                        punch);

                _visualRoot.localScale =
                    Vector3.one *
                    currentScale;
            }

            if (_impactFlashImage != null)
            {
                float flashScale =
                    Mathf.Lerp(
                        _impactFlashStartScale,
                        _impactFlashEndScale,
                        progress);

                _impactFlashImage
                    .rectTransform
                    .localScale =
                        Vector3.one *
                        flashScale;

                SetImageAlpha(
                    _impactFlashImage,
                    1f - progress);
            }

            yield return null;
        }

        SetImageAlpha(
            _impactFlashImage,
            0f);

        ResetVisualTransform();

        _feedbackRoutine = null;
    }

    /// <summary>
    /// 잘못된 입력 흔들림 연출 진행
    /// </summary>
    private IEnumerator
        PlayInvalidFeedbackRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime <
               _invalidDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _invalidDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _invalidDuration;

            progress =
                Mathf.Clamp01(progress);

            float remainingStrength =
                1f - progress;

            float shakeOffsetX =
                Mathf.Sin(
                    progress *
                    Mathf.PI *
                    2f *
                    _invalidShakeFrequency) *
                _invalidShakeStrength *
                remainingStrength;

            if (_visualRoot != null)
            {
                _visualRoot.anchoredPosition =
                    _visualBasePosition +
                    new Vector2(
                        shakeOffsetX,
                        0f);
            }

            if (_starImage != null)
            {
                _starImage.color =
                    Color.Lerp(
                        _invalidColor,
                        _starBaseColor,
                        progress);
            }

            yield return null;
        }

        ResetVisualTransform();

        if (_starImage != null)
        {
            _starImage.color =
                _starBaseColor;
        }

        _feedbackRoutine = null;
    }

    /// <summary>
    /// 잠금 상태 외형 적용
    /// </summary>
    private void ApplyLockedAppearance()
    {
        StopPulseRoutine();

        SetStarSprite(
            _idleSprite);

        SetImageAlpha(
            _ambientGlowImage,
            _lockedAmbientAlpha);

        SetImageAlpha(
            _availableGlowImage,
            0f);

        ResetAvailableGlowTransform();
    }

    /// <summary>
    /// 입력 가능 상태 외형 적용
    /// </summary>
    private void ApplyAvailableAppearance()
    {
        SetStarSprite(
            _idleSprite);

        SetImageAlpha(
            _ambientGlowImage,
            _availableAmbientAlpha);

        StartAvailablePulse();
    }

    /// <summary>
    /// 완료 상태 외형 적용
    /// </summary>
    private void ApplyCompletedAppearance()
    {
        StopPulseRoutine();

        SetStarSprite(
            _completedSprite);

        SetImageAlpha(
            _ambientGlowImage,
            _completedAmbientAlpha);

        SetImageAlpha(
            _availableGlowImage,
            0f);

        ResetAvailableGlowTransform();
    }

    /// <summary>
    /// 입력 가능 노드 맥동 시작
    /// </summary>
    private void StartAvailablePulse()
    {
        StopPulseRoutine();

        _pulseRoutine =
            StartCoroutine(
                PlayAvailablePulseRoutine());
    }

    /// <summary>
    /// 입력 가능 노드 맥동 진행
    /// </summary>
    private IEnumerator
        PlayAvailablePulseRoutine()
    {
        float elapsedTime = 0f;

        while (_nodeState ==
               ConstellationPathNodeState.Available)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float cycle =
                _availablePulseDuration <= 0f
                    ? 0f
                    : elapsedTime /
                      _availablePulseDuration;

            float pulse =
                Mathf.Sin(
                    cycle *
                    Mathf.PI *
                    2f) *
                0.5f +
                0.5f;

            float alpha =
                Mathf.Lerp(
                    _availableGlowMinAlpha,
                    _availableGlowMaxAlpha,
                    pulse);

            SetImageAlpha(
                _availableGlowImage,
                alpha);

            if (_availableGlowImage != null)
            {
                float scale =
                    Mathf.Lerp(
                        1f,
                        _availablePulseScale,
                        pulse);

                _availableGlowImage
                    .rectTransform
                    .localScale =
                        Vector3.one *
                        scale;
            }

            yield return null;
        }

        _pulseRoutine = null;
    }

    /// <summary>
    /// 별 이미지 변경
    /// </summary>
    /// <param name="sprite">적용 스프라이트</param>
    private void SetStarSprite(
        Sprite sprite)
    {
        if (_starImage == null ||
            sprite == null)
        {
            return;
        }

        _starImage.sprite =
            sprite;
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
    /// 시각 루트 Transform 초기화
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
    /// 입력 가능 발광 Transform 초기화
    /// </summary>
    private void ResetAvailableGlowTransform()
    {
        if (_availableGlowImage == null)
        {
            return;
        }

        _availableGlowImage
            .rectTransform
            .localScale =
                Vector3.one;
    }

    /// <summary>
    /// 최종 성공 순차 발광 재생
    /// </summary>
    public void PlayResolutionPulse()
    {
        StopResolutionRoutine();
        StopFeedbackRoutine();
        StopPulseRoutine();

        SetInteractionEnabled(
            false);

        _resolutionRoutine =
            StartCoroutine(
                PlayResolutionPulseRoutine());
    }

    /// <summary>
    /// 성공 별자리 소멸 연출 재생
    /// </summary>
    public void PlaySuccessDisappear()
    {
        StopResolutionRoutine();
        StopFeedbackRoutine();
        StopPulseRoutine();

        SetInteractionEnabled(
            false);

        _resolutionRoutine =
            StartCoroutine(
                PlaySuccessDisappearRoutine());
    }

    /// <summary>
    /// 실패 별 흩어짐 연출 재생
    /// </summary>
    /// <param name="direction">흩어지는 방향</param>
    /// <param name="delay">시작 지연 시간</param>
    public void PlayFailureDisappear(
        Vector2 direction,
        float delay)
    {
        StopResolutionRoutine();
        StopFeedbackRoutine();
        StopPulseRoutine();

        SetInteractionEnabled(
            false);

        _resolutionRoutine =
            StartCoroutine(
                PlayFailureDisappearRoutine(
                    direction,
                    delay));
    }

    /// <summary>
    /// 등장 연출 정지
    /// </summary>
    private void StopRevealRoutine()
    {
        if (_revealRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _revealRoutine);

        _revealRoutine = null;
    }

    /// <summary>
    /// 맥동 연출 정지
    /// </summary>
    private void StopPulseRoutine()
    {
        if (_pulseRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _pulseRoutine);

        _pulseRoutine = null;
    }

    /// <summary>
    /// 입력 피드백 연출 정지
    /// </summary>
    private void StopFeedbackRoutine()
    {
        if (_feedbackRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _feedbackRoutine);

        _feedbackRoutine = null;
    }

    /// <summary>
    /// 최종 성공 발광 진행
    /// </summary>
    private IEnumerator PlayResolutionPulseRoutine()
    {
        float elapsedTime = 0f;

        SetImageAlpha(
            _impactFlashImage,
            1f);

        while (elapsedTime <
               _resolutionPulseDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _resolutionPulseDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _resolutionPulseDuration;

            progress =
                Mathf.Clamp01(progress);

            float pulse =
                Mathf.Sin(
                    progress *
                    Mathf.PI);

            if (_visualRoot != null)
            {
                float currentScale =
                    Mathf.Lerp(
                        1f,
                        _resolutionPulseScale,
                        pulse);

                _visualRoot.localScale =
                    Vector3.one *
                    currentScale;
            }

            if (_impactFlashImage != null)
            {
                float flashScale =
                    Mathf.Lerp(
                        0.8f,
                        1.6f,
                        progress);

                _impactFlashImage
                    .rectTransform
                    .localScale =
                        Vector3.one *
                        flashScale;

                SetImageAlpha(
                    _impactFlashImage,
                    1f - progress);
            }

            yield return null;
        }

        SetImageAlpha(
            _impactFlashImage,
            0f);

        ResetVisualTransform();

        _resolutionRoutine = null;
    }

    /// <summary>
    /// 성공 별자리 페이드 아웃 진행
    /// </summary>
    private IEnumerator PlaySuccessDisappearRoutine()
    {
        float elapsedTime = 0f;

        float startAlpha =
            _canvasGroup != null
                ? _canvasGroup.alpha
                : 1f;

        while (elapsedTime <
               _successDisappearDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _successDisappearDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _successDisappearDuration;

            progress =
                Mathf.Clamp01(progress);

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f);

            if (_visualRoot != null)
            {
                float currentScale =
                    Mathf.Lerp(
                        1f,
                        _successDisappearScale,
                        easedProgress);

                _visualRoot.localScale =
                    Vector3.one *
                    currentScale;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        easedProgress);
            }

            yield return null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        _resolutionRoutine = null;
    }

    /// <summary>
    /// 실패 별 흩어짐 진행
    /// </summary>
    /// <param name="direction">흩어지는 방향</param>
    /// <param name="delay">시작 지연 시간</param>
    private IEnumerator PlayFailureDisappearRoutine(
        Vector2 direction,
        float delay)
    {
        if (delay > 0f)
        {
            float delayTime = 0f;

            while (delayTime < delay)
            {
                delayTime +=
                    Time.unscaledDeltaTime;

                yield return null;
            }
        }

        Vector2 normalizedDirection =
            direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : UnityEngine.Random
                    .insideUnitCircle
                    .normalized;

        float rotationDirection =
            UnityEngine.Random.value < 0.5f
                ? -1f
                : 1f;

        Vector2 startPosition =
            _rectTransform != null
                ? _rectTransform.anchoredPosition
                : Vector2.zero;

        float elapsedTime = 0f;

        float startAlpha =
            _canvasGroup != null
                ? _canvasGroup.alpha
                : 1f;

        while (elapsedTime <
               _failureDisappearDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _failureDisappearDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _failureDisappearDuration;

            progress =
                Mathf.Clamp01(progress);

            float easedProgress =
                progress *
                progress;

            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition =
                    startPosition +
                    normalizedDirection *
                    _failureScatterDistance *
                    easedProgress;

                _rectTransform.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        _failureRotation *
                        rotationDirection *
                        easedProgress);

                _rectTransform.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        1f,
                        0.72f,
                        easedProgress);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        progress);
            }

            yield return null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        _resolutionRoutine = null;
    }

    /// <summary>
    /// 최종 판정 연출 정지
    /// </summary>
    private void StopResolutionRoutine()
    {
        if (_resolutionRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _resolutionRoutine);

        _resolutionRoutine = null;
    }
}
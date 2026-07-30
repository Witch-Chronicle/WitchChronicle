using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경로형 별자리 연결선 UI
/// 두 노드 사이 배치와 그리기 연출
/// </summary>
[RequireComponent(
    typeof(RectTransform),
    typeof(CanvasGroup))]
public class ConstellationPathLineView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RectTransform _lineRect;

    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private Image _lineImage;

    [Header("Appearance")]
    [SerializeField, Min(1f)]
    private float _lineThickness = 6f;

    [SerializeField, Min(0f)]
    private float _drawDuration = 0.16f;

    [Header("Resolution")]
    [SerializeField, Min(0f)]
    private float _resolutionPulseDuration = 0.2f;

    [SerializeField, Min(1f)]
    private float _resolutionThicknessScale = 2f;

    [SerializeField]
    private Color _resolutionColor =
        Color.white;

    [SerializeField, Min(0f)]
    private float _successDisappearDuration = 0.38f;

    [SerializeField, Min(0f)]
    private float _failureDisappearDuration = 0.28f;

    private Coroutine _drawRoutine;

    private Coroutine _resolutionRoutine;

    private Color _baseLineColor = Color.white;

    public string StartNodeId { get; private set; }

    public string EndNodeId { get; private set; }

    /// <summary>
    /// 내부 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        if (_lineRect == null)
        {
            _lineRect =
                GetComponent<RectTransform>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (_lineImage == null)
        {
            _lineImage =
                GetComponent<Image>();
        }

        if (_lineImage != null)
        {
            _baseLineColor =
                _lineImage.color;
        }
    }

    /// <summary>
    /// 비활성화 시 연출 정지
    /// </summary>
    private void OnDisable()
    {
        StopDrawRoutine();
    }

    /// <summary>
    /// 두 노드 사이 연결선 초기화
    /// </summary>
    /// <param name="startNodeId">시작 노드 ID</param>
    /// <param name="endNodeId">종료 노드 ID</param>
    /// <param name="startNode">시작 노드</param>
    /// <param name="endNode">종료 노드</param>
    /// <param name="coordinateRoot">좌표 기준 루트</param>
    public void Initialize(
        string startNodeId,
        string endNodeId,
        RectTransform startNode,
        RectTransform endNode,
        RectTransform coordinateRoot)
    {
        if (startNode == null ||
            endNode == null ||
            coordinateRoot == null)
        {
            Destroy(
                gameObject);

            return;
        }

        StartNodeId =
            startNodeId;

        EndNodeId =
            endNodeId;

        ConfigureLine(
            startNode,
            endNode,
            coordinateRoot);

        StopDrawRoutine();

        _drawRoutine =
            StartCoroutine(
                PlayDrawRoutine());
    }

    /// <summary>
    /// 두 노드 사이 선 배치
    /// </summary>
    /// <param name="startNode">시작 노드</param>
    /// <param name="endNode">종료 노드</param>
    /// <param name="coordinateRoot">좌표 기준 루트</param>
    private void ConfigureLine(
        RectTransform startNode,
        RectTransform endNode,
        RectTransform coordinateRoot)
    {
        Vector3 startLocalPosition =
            coordinateRoot.InverseTransformPoint(
                startNode.position);

        Vector3 endLocalPosition =
            coordinateRoot.InverseTransformPoint(
                endNode.position);

        Vector2 startPosition =
            new Vector2(
                startLocalPosition.x,
                startLocalPosition.y);

        Vector2 endPosition =
            new Vector2(
                endLocalPosition.x,
                endLocalPosition.y);

        Vector2 direction =
            endPosition -
            startPosition;

        float distance =
            direction.magnitude;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x) *
            Mathf.Rad2Deg;

        _lineRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f);

        _lineRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f);

        _lineRect.pivot =
            new Vector2(
                0f,
                0.5f);

        _lineRect.anchoredPosition =
            startPosition;

        _lineRect.sizeDelta =
            new Vector2(
                distance,
                _lineThickness);

        _lineRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle);

        _lineRect.localScale =
            new Vector3(
                0f,
                1f,
                1f);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 최종 성공 연결선 발광 재생
    /// </summary>
    public void PlayResolutionPulse()
    {
        StopDrawRoutine();
        StopResolutionRoutine();

        _resolutionRoutine =
            StartCoroutine(
                PlayResolutionPulseRoutine());
    }

    /// <summary>
    /// 성공 연결선 소멸 재생
    /// </summary>
    public void PlaySuccessDisappear()
    {
        StopDrawRoutine();
        StopResolutionRoutine();

        _resolutionRoutine =
            StartCoroutine(
                PlayDisappearRoutine(
                    _successDisappearDuration,
                    false,
                    0f));
    }

    /// <summary>
    /// 실패 연결선 소멸 재생
    /// </summary>
    /// <param name="delay">시작 지연 시간</param>
    public void PlayFailureDisappear(
        float delay)
    {
        StopDrawRoutine();
        StopResolutionRoutine();

        _resolutionRoutine =
            StartCoroutine(
                PlayDisappearRoutine(
                    _failureDisappearDuration,
                    true,
                    delay));
    }

    /// <summary>
    /// 연결선 그리기 연출 진행
    /// </summary>
    private IEnumerator PlayDrawRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime <
               _drawDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                _drawDuration <= 0f
                    ? 1f
                    : elapsedTime /
                      _drawDuration;

            progress =
                Mathf.Clamp01(progress);

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f);

            _lineRect.localScale =
                new Vector3(
                    easedProgress,
                    1f,
                    1f);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha =
                    easedProgress;
            }

            yield return null;
        }

        _lineRect.localScale =
            Vector3.one;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        _drawRoutine = null;
    }

    /// <summary>
    /// 연결선 그리기 연출 정지
    /// </summary>
    private void StopDrawRoutine()
    {
        if (_drawRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _drawRoutine);

        _drawRoutine = null;
    }

    /// <summary>
    /// 연결선 최종 발광 진행
    /// </summary>
    private IEnumerator PlayResolutionPulseRoutine()
    {
        float elapsedTime = 0f;

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

            if (_lineRect != null)
            {
                float thicknessScale =
                    Mathf.Lerp(
                        1f,
                        _resolutionThicknessScale,
                        pulse);

                _lineRect.localScale =
                    new Vector3(
                        1f,
                        thicknessScale,
                        1f);
            }

            if (_lineImage != null)
            {
                _lineImage.color =
                    Color.Lerp(
                        _baseLineColor,
                        _resolutionColor,
                        pulse);
            }

            yield return null;
        }

        if (_lineRect != null)
        {
            _lineRect.localScale =
                Vector3.one;
        }

        if (_lineImage != null)
        {
            _lineImage.color =
                _baseLineColor;
        }

        _resolutionRoutine = null;
    }

    /// <summary>
    /// 연결선 소멸 진행
    /// </summary>
    /// <param name="duration">소멸 시간</param>
    /// <param name="shrinkLine">선 축소 여부</param>
    /// <param name="delay">시작 지연 시간</param>
    private IEnumerator PlayDisappearRoutine(
        float duration,
        bool shrinkLine,
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

        float elapsedTime = 0f;

        float startAlpha =
            _canvasGroup != null
                ? _canvasGroup.alpha
                : 1f;

        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                duration <= 0f
                    ? 1f
                    : elapsedTime /
                      duration;

            progress =
                Mathf.Clamp01(progress);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        progress);
            }

            if (shrinkLine &&
                _lineRect != null)
            {
                _lineRect.localScale =
                    new Vector3(
                        1f - progress,
                        1f,
                        1f);
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
    /// 판정 연출 정지
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
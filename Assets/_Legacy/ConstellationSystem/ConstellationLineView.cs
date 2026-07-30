using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 별자리 연결선 UI
/// 두 별 사이 선 배치와 생성 연출
/// </summary>
[RequireComponent(
    typeof(RectTransform),
    typeof(CanvasGroup))]
public class ConstellationLineView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _lineRect;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _lineImage;

    [Header("Appearance")]
    [SerializeField, Min(1f)]
    private float _lineThickness = 8f;

    [SerializeField, Min(0f)]
    private float _drawDuration = 0.15f;

    [Header("Completion Flash")]
    [SerializeField, Min(0f)]
    private float _completionFlashDuration = 0.42f;

    [SerializeField, Min(1)]
    private int _completionPulseCount = 2;

    [SerializeField, Min(1f)]
    private float _completionThicknessScale = 2f;

    [SerializeField]
    private Color _completionFlashColor = Color.white;

    private Coroutine _drawRoutine;
    private Coroutine _completionRoutine;
    private Color _baseLineColor = Color.white;

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
        StopCompletionRoutine();
    }

    /// <summary>
    /// 연결선 초기화
    /// </summary>
    /// <param name="startStar">시작 별</param>
    /// <param name="endStar">종료 별</param>
    /// <param name="coordinateRoot">좌표 기준 RectTransform</param>
    public void Initialize(
        RectTransform startStar,
        RectTransform endStar,
        RectTransform coordinateRoot)
    {
        if (startStar == null ||
            endStar == null ||
            coordinateRoot == null)
        {
            Destroy(gameObject);
            return;
        }

        ConfigureLine(
            startStar,
            endStar,
            coordinateRoot);

        StopDrawRoutine();

        _drawRoutine =
            StartCoroutine(PlayDraw());
    }

    /// <summary>
    /// 두 별 사이 연결선 배치
    /// </summary>
    /// <param name="startStar">시작 별</param>
    /// <param name="endStar">종료 별</param>
    /// <param name="coordinateRoot">좌표 기준 RectTransform</param>
    private void ConfigureLine(
        RectTransform startStar,
        RectTransform endStar,
        RectTransform coordinateRoot)
    {
        Vector3 startLocalPosition =
            coordinateRoot.InverseTransformPoint(
                startStar.position);

        Vector3 endLocalPosition =
            coordinateRoot.InverseTransformPoint(
                endStar.position);

        Vector2 startPosition =
            new Vector2(
                startLocalPosition.x,
                startLocalPosition.y);

        Vector2 endPosition =
            new Vector2(
                endLocalPosition.x,
                endLocalPosition.y);

        Vector2 direction =
            endPosition - startPosition;

        float distance =
            direction.magnitude;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x) *
            Mathf.Rad2Deg;

        _lineRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        _lineRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        _lineRect.pivot =
            new Vector2(0f, 0.5f);

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
    /// 연결선 생성 연출
    /// </summary>
    private IEnumerator PlayDraw()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _drawDuration)
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
    /// 연결선 연출 정지
    /// </summary>
    private void StopDrawRoutine()
    {
        if (_drawRoutine == null)
        {
            return;
        }

        StopCoroutine(_drawRoutine);
        _drawRoutine = null;
    }

    /// <summary>
    /// 별자리 완성 연결선 발광 재생
    /// </summary>
    public void PlayCompletionFlash()
    {
        StopDrawRoutine();
        StopCompletionRoutine();

        if (_lineRect != null)
        {
            _lineRect.localScale =
                Vector3.one;
        }

        _completionRoutine =
            StartCoroutine(
                PlayCompletionFlashRoutine());
    }

    /// <summary>
    /// 연결선 완성 발광 진행
    /// </summary>
    private IEnumerator PlayCompletionFlashRoutine()
    {
        float elapsedTime = 0f;

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

            if (_lineRect != null)
            {
                float thicknessScale =
                    Mathf.Lerp(
                        1f,
                        _completionThicknessScale,
                        pulse);

                _lineRect.localScale =
                    new Vector3(
                        1f,
                        thicknessScale,
                        1f);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha =
                    Mathf.Lerp(
                        0.75f,
                        1f,
                        pulse);
            }

            if (_lineImage != null)
            {
                _lineImage.color =
                    Color.Lerp(
                        _baseLineColor,
                        _completionFlashColor,
                        pulse);
            }

            yield return null;
        }

        if (_lineRect != null)
        {
            _lineRect.localScale =
                Vector3.one;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (_lineImage != null)
        {
            _lineImage.color =
                _baseLineColor;
        }

        _completionRoutine = null;
    }

    /// <summary>
    /// 완성 발광 연출 정지
    /// </summary>
    private void StopCompletionRoutine()
    {
        if (_completionRoutine == null)
        {
            return;
        }

        StopCoroutine(
            _completionRoutine);

        _completionRoutine = null;
    }
}
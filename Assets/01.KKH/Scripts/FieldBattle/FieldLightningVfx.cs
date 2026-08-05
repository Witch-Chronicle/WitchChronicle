using System.Collections;
using UnityEngine;

/// <summary>
/// 필드 번개 공격 이펙트
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class FieldLightningVfx : MonoBehaviour
{
    [Header("Lightning")]
    [SerializeField] private int _segmentCount = 12;
    [SerializeField] private float _jaggedness = 0.18f;
    [SerializeField] private float _duration = 0.15f;
    [SerializeField] private float _refreshInterval = 0.025f;

    private LineRenderer _lineRenderer;
    private Transform _startPoint;
    private Transform _endPoint;
    private Vector3 _fixedEndPosition;
    private bool _useFixedEndPosition;

    /// <summary>
    /// LineRenderer 참조 초기화
    /// </summary>
    private void Awake()
    {
        _lineRenderer =
            GetComponent<LineRenderer>();

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.enabled = false;
    }

    /// <summary>
    /// Transform 대상 번개 재생
    /// </summary>
    /// <param name="startPoint">시작 위치</param>
    /// <param name="endPoint">도착 위치</param>
    public void Play(
        Transform startPoint,
        Transform endPoint)
    {
        _startPoint = startPoint;
        _endPoint = endPoint;

        _useFixedEndPosition = false;

        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    /// <summary>
    /// 고정 좌표 대상 번개 재생
    /// </summary>
    /// <param name="startPoint">시작 위치</param>
    /// <param name="endPosition">도착 좌표</param>
    public void Play(
        Transform startPoint,
        Vector3 endPosition)
    {
        _startPoint = startPoint;
        _endPoint = null;

        _fixedEndPosition = endPosition;
        _useFixedEndPosition = true;

        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    /// <summary>
    /// 번개 이펙트 진행
    /// </summary>
    private IEnumerator PlayRoutine()
    {
        bool hasEndPoint =
            _useFixedEndPosition ||
            _endPoint != null;

        if (_lineRenderer == null ||
            _startPoint == null ||
            hasEndPoint == false)
        {
            Destroy(gameObject);
            yield break;
        }

        _lineRenderer.enabled = true;

        float elapsedTime = 0f;

        while (elapsedTime < _duration)
        {
            if (_startPoint == null ||
                (_useFixedEndPosition == false &&
                 _endPoint == null))
            {
                break;
            }

            UpdateLightningLine();

            if (_refreshInterval > 0f)
            {
                yield return new WaitForSeconds(
                    _refreshInterval);

                elapsedTime +=
                    _refreshInterval;
            }
            else
            {
                elapsedTime +=
                    Time.deltaTime;

                yield return null;
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 번개 선 위치 갱신
    /// </summary>
    private void UpdateLightningLine()
    {
        Vector3 startPosition =
            _startPoint.position;

        Vector3 endPosition =
            _useFixedEndPosition
                ? _fixedEndPosition
                : _endPoint.position;

        Vector3 direction =
            endPosition -
            startPosition;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
        {
            return;
        }

        direction.Normalize();

        Vector3 side =
            Vector3.Cross(
                direction,
                Vector3.up);

        if (side.sqrMagnitude <= 0.001f)
        {
            side =
                Vector3.Cross(
                    direction,
                    Vector3.right);
        }

        side.Normalize();

        Vector3 vertical =
            Vector3.Cross(
                direction,
                side).normalized;

        int pointCount =
            Mathf.Max(
                2,
                _segmentCount);

        _lineRenderer.positionCount =
            pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float ratio =
                i /
                (float)(pointCount - 1);

            Vector3 position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    ratio);

            if (i > 0 &&
                i < pointCount - 1)
            {
                float taper =
                    Mathf.Sin(
                        ratio *
                        Mathf.PI);

                float horizontalOffset =
                    Random.Range(
                        -_jaggedness,
                        _jaggedness) *
                    taper;

                float verticalOffset =
                    Random.Range(
                        -_jaggedness,
                        _jaggedness) *
                    taper;

                position +=
                    side *
                    horizontalOffset +
                    vertical *
                    verticalOffset;
            }

            _lineRenderer.SetPosition(
                i,
                position);
        }
    }
}
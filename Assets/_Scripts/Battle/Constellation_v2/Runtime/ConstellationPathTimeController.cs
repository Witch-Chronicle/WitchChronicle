using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 별자리 공격 시간 감속 및 복귀 제어
/// </summary>
public class ConstellationPathTimeController : MonoBehaviour
{
    [Header("Slow Motion")]
    [Tooltip("별자리 진행 중 유지할 초저속 시간 배율")]
    [SerializeField, Range(0.01f, 1f)] private float _slowMotionScale = 0.04f;

    [Tooltip("현재 속도에서 초저속 배율까지 감속하는 시간")]
    [SerializeField, Min(0f)] private float _slowDownDuration = 0.3f;

    [Header("Resume")]
    [Tooltip("초저속 상태에서 기존 속도로 복귀하는 시간")]
    [SerializeField, Min(0f)] private float _resumeDuration = 0.25f;

    private Coroutine _timeRoutine;

    private float _originalTimeScale = 1f;
    private float _originalFixedDeltaTime = 0.02f;
    private bool _hasCapturedTimeSettings;

    public bool IsTransitioning => _timeRoutine != null;
    public bool IsSlowMotion => _hasCapturedTimeSettings && Time.timeScale < _originalTimeScale;

    /// <summary>
    /// 비활성화 시 기존 시간 설정 복구
    /// </summary>
    private void OnDisable()
    {
        RestoreImmediate();
    }

    /// <summary>
    /// 게임 시간을 별자리 초저속 상태로 점진 감속
    /// </summary>
    /// <param name="onComplete">감속 완료 콜백</param>
    public void SlowDown(Action onComplete = null)
    {
        StopCurrentRoutine();
        CaptureTimeSettings();

        _timeRoutine = StartCoroutine(SlowDownRoutine(onComplete));
    }

    /// <summary>
    /// 초저속 상태에서 기존 시간 속도로 점진 복귀
    /// </summary>
    /// <param name="onComplete">시간 복귀 완료 콜백</param>
    public void Resume(Action onComplete = null)
    {
        StopCurrentRoutine();

        if (_hasCapturedTimeSettings == false)
        {
            onComplete?.Invoke();
            return;
        }

        _timeRoutine = StartCoroutine(ResumeRoutine(onComplete));
    }

    /// <summary>
    /// 시간 설정 즉시 복구
    /// </summary>
    public void RestoreImmediate()
    {
        StopCurrentRoutine();

        if (_hasCapturedTimeSettings == false) return;

        Time.timeScale = _originalTimeScale;
        Time.fixedDeltaTime = _originalFixedDeltaTime;

        _hasCapturedTimeSettings = false;
    }

    /// <summary>
    /// 초저속 시간 배율까지 감속
    /// </summary>
    /// <param name="onComplete">감속 완료 콜백</param>
    private IEnumerator SlowDownRoutine(Action onComplete)
    {
        yield return ChangeTimeScaleRoutine(
            Time.timeScale,
            _slowMotionScale,
            _slowDownDuration);

        _timeRoutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 기존 시간 속도까지 복귀
    /// </summary>
    /// <param name="onComplete">복귀 완료 콜백</param>
    private IEnumerator ResumeRoutine(Action onComplete)
    {
        yield return ChangeTimeScaleRoutine(
            Time.timeScale,
            _originalTimeScale,
            _resumeDuration);

        Time.timeScale = _originalTimeScale;
        Time.fixedDeltaTime = _originalFixedDeltaTime;

        _hasCapturedTimeSettings = false;
        _timeRoutine = null;

        onComplete?.Invoke();
    }

    /// <summary>
    /// 지정 시간 동안 시간 배율 보간
    /// </summary>
    /// <param name="startScale">시작 시간 배율</param>
    /// <param name="targetScale">목표 시간 배율</param>
    /// <param name="duration">보간 시간</param>
    private IEnumerator ChangeTimeScaleRoutine(
        float startScale,
        float targetScale,
        float duration)
    {
        if (duration <= 0f)
        {
            ApplyTimeScale(targetScale);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);

            // 빠르게 감속을 시작하고 목표 속도에 부드럽게 접근
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            ApplyTimeScale(
                Mathf.Lerp(startScale, targetScale, easedProgress));

            yield return null;
        }

        ApplyTimeScale(targetScale);
    }

    /// <summary>
    /// 현재 시간 설정 저장
    /// </summary>
    private void CaptureTimeSettings()
    {
        if (_hasCapturedTimeSettings) return;

        _originalTimeScale = Mathf.Max(0.0001f, Time.timeScale);
        _originalFixedDeltaTime = Time.fixedDeltaTime;
        _hasCapturedTimeSettings = true;
    }

    /// <summary>
    /// 시간 배율 및 고정 업데이트 간격 적용
    /// </summary>
    /// <param name="timeScale">적용 시간 배율</param>
    private void ApplyTimeScale(float timeScale)
    {
        Time.timeScale = Mathf.Max(0.0001f, timeScale);

        float scaleRatio = Time.timeScale / _originalTimeScale;
        Time.fixedDeltaTime = Mathf.Max(
            0.0001f,
            _originalFixedDeltaTime * scaleRatio);
    }

    /// <summary>
    /// 진행 중인 시간 전환 중단
    /// </summary>
    private void StopCurrentRoutine()
    {
        if (_timeRoutine == null) return;

        StopCoroutine(_timeRoutine);
        _timeRoutine = null;
    }
}
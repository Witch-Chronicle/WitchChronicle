using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 별자리 공격 시간 감속, 정지, 복귀 제어
/// </summary>
public class ConstellationPathTimeController : MonoBehaviour
{
    [Header("Slow Motion")]
    [Tooltip("시간 정지 직전 감속 배율")]
    [SerializeField, Range(0.01f, 1f)] private float _slowMotionScale = 0.15f;

    [Tooltip("현재 속도에서 감속 배율까지 도달하는 시간")]
    [SerializeField, Min(0f)] private float _slowDownDuration = 0.3f;

    [Tooltip("감속 상태 유지 후 완전히 정지하기까지의 시간")]
    [SerializeField, Min(0f)] private float _slowMotionHoldDuration = 0.08f;

    [Header("Resume")]
    [Tooltip("정지 상태에서 기존 속도로 복귀하는 시간")]
    [SerializeField, Min(0f)] private float _resumeDuration = 0.25f;

    private Coroutine _timeRoutine;

    private float _originalTimeScale = 1f;
    private float _originalFixedDeltaTime = 0.02f;
    private bool _hasCapturedTimeSettings;

    public bool IsPaused => Mathf.Approximately(Time.timeScale, 0f);
    public bool IsTransitioning => _timeRoutine != null;

    /// <summary>
    /// 비활성화 시 기존 시간 설정 복구
    /// </summary>
    private void OnDisable()
    {
        RestoreImmediate();
    }

    /// <summary>
    /// 시간 점진 감속 후 정지
    /// </summary>
    /// <param name="onComplete">시간 정지 완료 콜백</param>
    public void SlowDownAndPause(Action onComplete = null)
    {
        StopCurrentRoutine();
        CaptureTimeSettings();

        _timeRoutine = StartCoroutine(SlowDownAndPauseRoutine(onComplete));
    }

    /// <summary>
    /// 정지 상태에서 기존 시간 속도로 복귀
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

        if (_hasCapturedTimeSettings == false)
        {
            return;
        }

        Time.timeScale = _originalTimeScale;
        Time.fixedDeltaTime = _originalFixedDeltaTime;

        _hasCapturedTimeSettings = false;
    }

    /// <summary>
    /// 시간 감속 및 정지 진행
    /// </summary>
    /// <param name="onComplete">시간 정지 완료 콜백</param>
    private IEnumerator SlowDownAndPauseRoutine(Action onComplete)
    {
        yield return ChangeTimeScaleRoutine(Time.timeScale, _slowMotionScale, _slowDownDuration);

        if (_slowMotionHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(_slowMotionHoldDuration);
        }

        ApplyTimeScale(0f);

        _timeRoutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 기존 시간 속도 복귀 진행
    /// </summary>
    /// <param name="onComplete">시간 복귀 완료 콜백</param>
    private IEnumerator ResumeRoutine(Action onComplete)
    {
        yield return ChangeTimeScaleRoutine(Time.timeScale, _originalTimeScale, _resumeDuration);

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
    private IEnumerator ChangeTimeScaleRoutine(float startScale, float targetScale, float duration)
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
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            ApplyTimeScale(Mathf.Lerp(startScale, targetScale, easedProgress));

            yield return null;
        }

        ApplyTimeScale(targetScale);
    }

    /// <summary>
    /// 현재 시간 설정 저장
    /// </summary>
    private void CaptureTimeSettings()
    {
        if (_hasCapturedTimeSettings)
        {
            return;
        }

        _originalTimeScale = Mathf.Max(0.0001f, Time.timeScale);
        _originalFixedDeltaTime = Time.fixedDeltaTime;
        _hasCapturedTimeSettings = true;
    }

    /// <summary>
    /// 시간 배율과 고정 업데이트 간격 적용
    /// </summary>
    /// <param name="timeScale">적용 시간 배율</param>
    private void ApplyTimeScale(float timeScale)
    {
        Time.timeScale = Mathf.Max(0f, timeScale);

        float scaleRatio = Time.timeScale / _originalTimeScale;
        Time.fixedDeltaTime = Mathf.Max(0.0001f, _originalFixedDeltaTime * scaleRatio);
    }

    /// <summary>
    /// 진행 중인 시간 전환 중단
    /// </summary>
    private void StopCurrentRoutine()
    {
        if (_timeRoutine == null)
        {
            return;
        }

        StopCoroutine(_timeRoutine);
        _timeRoutine = null;
    }
}
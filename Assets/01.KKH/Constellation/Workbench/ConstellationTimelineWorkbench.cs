using UnityEngine;

/// <summary>
/// 별자리 타임라인 워크벤치
/// 시퀀스 이벤트 발생 시각 출력
/// </summary>
[RequireComponent(typeof(ConstellationSequenceController))]
public class ConstellationTimelineWorkbench : MonoBehaviour
{
    [SerializeField]
    private ConstellationSequenceController _sequenceController;

    /// <summary>
    /// 컴포넌트 자동 연결
    /// </summary>
    private void Reset()
    {
        _sequenceController =
            GetComponent<ConstellationSequenceController>();
    }

    /// <summary>
    /// 실행 전 컴포넌트 참조 확보
    /// </summary>
    private void Awake()
    {
        if (_sequenceController != null)
        {
            return;
        }

        _sequenceController =
            GetComponent<ConstellationSequenceController>();
    }

    /// <summary>
    /// 시퀀스 이벤트 구독
    /// </summary>
    private void OnEnable()
    {
        if (_sequenceController == null)
        {
            return;
        }

        _sequenceController.OnSequenceStarted +=
            HandleSequenceStarted;

        _sequenceController.OnProjectileLaunchRequested +=
            HandleProjectileLaunchRequested;

        _sequenceController.OnStarShowRequested +=
            HandleStarShowRequested;

        _sequenceController.OnImpactReached +=
            HandleImpactReached;

        _sequenceController.OnSequenceCompleted +=
            HandleSequenceCompleted;
    }

    /// <summary>
    /// 시퀀스 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (_sequenceController == null)
        {
            return;
        }

        _sequenceController.OnSequenceStarted -=
            HandleSequenceStarted;

        _sequenceController.OnProjectileLaunchRequested -=
            HandleProjectileLaunchRequested;

        _sequenceController.OnStarShowRequested -=
            HandleStarShowRequested;

        _sequenceController.OnImpactReached -=
            HandleImpactReached;

        _sequenceController.OnSequenceCompleted -=
            HandleSequenceCompleted;
    }

    /// <summary>
    /// 시퀀스 시작 로그
    /// </summary>
    private void HandleSequenceStarted(
        ConstellationSequenceData sequenceData)
    {
        Debug.Log(
            $"[0.00초] 시퀀스 시작: {sequenceData.SequenceId}",
            sequenceData);
    }

    /// <summary>
    /// 투사체 발사 로그
    /// </summary>
    private void HandleProjectileLaunchRequested(
        int beatIndex,
        ConstellationBeatData beat)
    {
        Debug.Log(
            $"[{_sequenceController.ElapsedTime:F2}초] " +
            $"Beat {beatIndex} 투사체 발사" +
            $"\n예정 시각: {beat.ProjectileLaunchTime:F2}초",
            _sequenceController);
    }

    /// <summary>
    /// 별 표시 로그
    /// </summary>
    private void HandleStarShowRequested(
        int beatIndex,
        ConstellationBeatData beat)
    {
        Debug.Log(
            $"[{_sequenceController.ElapsedTime:F2}초] " +
            $"Beat {beatIndex} 별 표시" +
            $"\n예정 시각: {beat.StarShowTime:F2}초" +
            $"\n별 위치: {beat.NormalizedStarPosition}",
            _sequenceController);
    }

    /// <summary>
    /// 투사체 충돌 로그
    /// </summary>
    private void HandleImpactReached(
        int beatIndex,
        ConstellationBeatData beat)
    {
        Debug.Log(
            $"[{_sequenceController.ElapsedTime:F2}초] " +
            $"Beat {beatIndex} 충돌" +
            $"\n예정 시각: {beat.ImpactTime:F2}초",
            _sequenceController);
    }

    /// <summary>
    /// 시퀀스 종료 로그
    /// </summary>
    private void HandleSequenceCompleted(
        ConstellationSequenceData sequenceData)
    {
        Debug.Log(
            $"[{_sequenceController.ElapsedTime:F2}초] " +
            $"시퀀스 종료: {sequenceData.SequenceId}",
            sequenceData);
    }
}
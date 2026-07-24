using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 시퀀스 실행기
/// 투사체와 별자리 UI의 공통 시간 관리
/// </summary>
public class ConstellationSequenceController : MonoBehaviour
{
    /// <summary>
    /// 타임라인 이벤트 종류
    /// </summary>
    private enum TimelineEventType
    {
        ProjectileLaunch,
        StarShow,
        Impact
    }

    private readonly struct TimelineEventData
    {
        public float Time { get; }
        public int BeatIndex { get; }
        public TimelineEventType EventType { get; }

        /// <summary>
        /// 타임라인 이벤트 생성
        /// </summary>
        public TimelineEventData(
            float time,
            int beatIndex,
            TimelineEventType eventType)
        {
            Time = time;
            BeatIndex = beatIndex;
            EventType = eventType;
        }
    }

    [Header("Sequence Test")]
    [SerializeField] private ConstellationSequenceData _sequenceData;
    [SerializeField] private bool _playOnStart = true;

    private readonly List<TimelineEventData> _timelineEvents = new List<TimelineEventData>();

    private int _nextEventIndex;
    private float _elapsedTime;
    private bool _isRunning;

    public ConstellationSequenceData SequenceData => _sequenceData;
    public float ElapsedTime => _elapsedTime;
    public bool IsRunning => _isRunning;

    public event Action<ConstellationSequenceData> OnSequenceStarted;

    public event Action<int, ConstellationBeatData> OnProjectileLaunchRequested;

    public event Action<int, ConstellationBeatData> OnStarShowRequested;

    public event Action<int, ConstellationBeatData> OnImpactReached;

    public event Action<ConstellationSequenceData> OnSequenceCompleted;

    /// <summary>
    /// 시작 시 자동 시퀀스 실행
    /// </summary>
    private void Start()
    {
        if (!_playOnStart)
            return;

        StartSequence();
    }

    /// <summary>
    /// 시퀀스 공동 시간 진행
    /// </summary>
    private void Update()
    {
        if (!_isRunning)
            return;

        _elapsedTime += Time.deltaTime;

        DispatchDueEvents();

        if (_nextEventIndex < _timelineEvents.Count)
            return;

        if (_elapsedTime < _sequenceData.Duration)
            return;

        CompleteSequence();
    }

    /// <summary>
    /// 컴포넌트 비활성화 시 시퀀스 정지
    /// </summary>
    private void OnDisable()
    {
        StopSequence();
    }

    /// <summary>
    /// 인스펙터 데이터 기반 시퀀스 시작
    /// </summary>
    [ContextMenu("Start Sequence")]
    public void StartSequence()
    {
        StartSequence(_sequenceData);
    }

    public void StartSequence(
        ConstellationSequenceData sequenceData)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("별자리 시퀀스는 PlayMode에서 실행해야 합니다.", this);
            return;
        }

        if (sequenceData == null)
        {
            Debug.LogWarning("별자리 시퀀스 데이터가 연결되지 않았습니다.", this);
            return;
        }

        if (!sequenceData.TryValidate(out string errorMessage))
        {
            Debug.LogWarning(
                $"별자리 시퀀스 시작 실패: {errorMessage}",
                sequenceData);

            return;
        }

        StopSequence();

        _sequenceData = sequenceData;
        _elapsedTime = 0f;
        _nextEventIndex = 0;

        BuildTimeline();

        _isRunning = true;

        OnSequenceStarted?.Invoke(_sequenceData);
    }

    /// <summary>
    /// 진행 중인 시퀀스 정지
    /// </summary>
    public void StopSequence()
    {
        _isRunning = false;
        _elapsedTime = 0f;
        _nextEventIndex = 0;
        _timelineEvents.Clear();
    }

    private void BuildTimeline()
    {
        _timelineEvents.Clear();

        for (int i = 0; i< _sequenceData.BeatCount; i++)
        {
            ConstellationBeatData beat = _sequenceData.Beats[i];

            _timelineEvents.Add(
                new TimelineEventData(
                    beat.ProjectileLaunchTime,
                    i,
                    TimelineEventType.ProjectileLaunch));

            _timelineEvents.Add(
                new TimelineEventData(
                    beat.StarShowTime,
                    i,
                    TimelineEventType.StarShow));

            _timelineEvents.Add(
                new TimelineEventData(
                    beat.ImpactTime,
                    i,
                    TimelineEventType.Impact));
        }

        _timelineEvents.Sort(
            (left, right) =>
            {
                int timeComparison = left.Time.CompareTo(right.Time);

                if (timeComparison != 0)
                    return timeComparison;

                return left.EventType.CompareTo(right.EventType);
            });
    }

    private void DispatchDueEvents()
    {
        while(_nextEventIndex < _timelineEvents.Count)
        {
            TimelineEventData timelineEvent = _timelineEvents[_nextEventIndex];

            if (_elapsedTime < timelineEvent.Time)
                return;

            DispatchTimelineEvent(timelineEvent);

            _nextEventIndex++;
        }
    }

    /// <summary>
    /// 타임라인 이벤트 종류별 전달
    /// </summary>
    private void DispatchTimelineEvent(
        TimelineEventData timelineEvent)
    {
        ConstellationBeatData beat =
            _sequenceData.Beats[timelineEvent.BeatIndex];

        switch (timelineEvent.EventType)
        {
            case TimelineEventType.ProjectileLaunch:
                OnProjectileLaunchRequested?.Invoke(
                    timelineEvent.BeatIndex,
                    beat);
                break;

            case TimelineEventType.StarShow:
                OnStarShowRequested?.Invoke(
                    timelineEvent.BeatIndex,
                    beat);
                break;

            case TimelineEventType.Impact:
                OnImpactReached?.Invoke(
                    timelineEvent.BeatIndex,
                    beat);
                break;
        }
    }

    /// <summary>
    /// 시퀀스 정상 완료
    /// </summary>
    private void CompleteSequence()
    {
        _elapsedTime = _sequenceData.Duration;
        _isRunning = false;

        OnSequenceCompleted?.Invoke(_sequenceData);
    }
}

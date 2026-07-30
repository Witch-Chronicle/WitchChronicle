using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경로형 별자리 실행 컨트롤러
/// 별 등장, 입력 진행, 타이머와 최종 판정 관리
/// </summary>
public class ConstellationPathController :
    MonoBehaviour
{
    private readonly ConstellationPathProgress
        _progress =
            new ConstellationPathProgress();

    private readonly List<
        ConstellationPathNodeData> _sortedNodes =
            new List<
                ConstellationPathNodeData>();

    private readonly List<string>
        _availableNodeIds =
            new List<string>();

    private Coroutine _runRoutine;

    private ConstellationPathSequenceData
        _sequenceData;

    private ConstellationPathPhase _phase =
        ConstellationPathPhase.Idle;

    private float _remainingTime;

    private float _remainingTimeAtCompletion;

    public ConstellationPathPhase Phase =>
        _phase;

    public ConstellationPathSequenceData
        SequenceData => _sequenceData;

    public float RemainingTime =>
        _remainingTime;

    public float NormalizedRemainingTime
    {
        get
        {
            if (_sequenceData == null ||
                _sequenceData.TimeLimit <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                _remainingTime /
                _sequenceData.TimeLimit);
        }
    }

    public bool IsRunning =>
        _phase != ConstellationPathPhase.Idle &&
        _phase != ConstellationPathPhase.Resolved;

    public bool IsInputEnabled =>
        _phase == ConstellationPathPhase.Playing;

    public ConstellationPathProgress Progress =>
        _progress;

    public event Action<
        ConstellationPathSequenceData>
        OnPathStarted;

    public event Action<
        ConstellationPathPhase>
        OnPhaseChanged;

    public event Action<
        ConstellationPathNodeData>
        OnNodeRevealRequested;

    public event Action
        OnAllNodesRevealed;

    public event Action
        OnInputStarted;

    public event Action<
        float,
        float>
        OnTimerChanged;

    public event Action<
        string,
        ConstellationPathInputResult>
        OnNodeInputResolved;

    public event Action<
        ConstellationPathNodeData>
        OnNodeCompleted;

    public event Action<
        string,
        ConstellationPathNodeState>
        OnNodeStateChanged;

    public event Action<
        IReadOnlyList<string>>
        OnAvailableNodesChanged;

    public event Action<
        ConstellationPathResult>
        OnPathCompleted;

    /// <summary>
    /// 비활성화 시 별자리 실행 정지
    /// </summary>
    private void OnDisable()
    {
        StopPath();
    }

    /// <summary>
    /// 경로형 별자리 실행
    /// </summary>
    /// <param name="sequenceData">실행 시퀀스 데이터</param>
    /// <returns>실행 성공 여부</returns>
    public bool StartPath(
        ConstellationPathSequenceData sequenceData)
    {
        StopPath();

        if (!_progress.Initialize(
                sequenceData,
                out string errorMessage))
        {
            Debug.LogWarning(
                $"[ConstellationPath] 시작 실패: " +
                $"{errorMessage}",
                this);

            return false;
        }

        _sequenceData =
            sequenceData;

        _remainingTime =
            sequenceData.TimeLimit;

        _remainingTimeAtCompletion =
            0f;

        BuildSortedNodeList();

        SetPhase(
            ConstellationPathPhase.Revealing);

        OnPathStarted?.Invoke(
            _sequenceData);

        _runRoutine =
            StartCoroutine(
                RunPathRoutine());

        return true;
    }

    /// <summary>
    /// 경로형 별자리 실행 중단
    /// </summary>
    public void StopPath()
    {
        if (_runRoutine != null)
        {
            StopCoroutine(
                _runRoutine);

            _runRoutine = null;
        }

        _progress.Clear();

        _sequenceData = null;

        _sortedNodes.Clear();
        _availableNodeIds.Clear();

        _remainingTime = 0f;
        _remainingTimeAtCompletion = 0f;

        SetPhase(
            ConstellationPathPhase.Idle);
    }

    /// <summary>
    /// 별 노드 클릭 입력 처리
    /// </summary>
    /// <param name="nodeId">클릭 노드 ID</param>
    /// <returns>입력 판정 결과</returns>
    public ConstellationPathInputResult
        SubmitNodeInput(
            string nodeId)
    {
        if (_phase !=
            ConstellationPathPhase.Playing)
        {
            ConstellationPathInputResult
                disabledResult =
                    ConstellationPathInputResult
                        .InputDisabled;

            OnNodeInputResolved?.Invoke(
                nodeId,
                disabledResult);

            return disabledResult;
        }

        _progress.TryCompleteNode(
            nodeId,
            out ConstellationPathInputResult
                inputResult);

        OnNodeInputResolved?.Invoke(
            nodeId,
            inputResult);

        if (inputResult !=
            ConstellationPathInputResult.Accepted)
        {
            return inputResult;
        }

        if (_sequenceData.TryGetNode(
                nodeId,
                out ConstellationPathNodeData
                    nodeData))
        {
            OnNodeCompleted?.Invoke(
                nodeData);
        }

        NotifyAllNodeStates();
        NotifyAvailableNodes();

        if (_progress.IsCompleted)
        {
            _remainingTimeAtCompletion =
                _remainingTime;

            SetPhase(
                ConstellationPathPhase
                    .SuccessDrain);
        }

        return inputResult;
    }

    /// <summary>
    /// 별자리 전체 실행 진행
    /// </summary>
    private IEnumerator RunPathRoutine()
    {
        yield return RevealNodesRoutine();

        if (_phase !=
            ConstellationPathPhase.Revealing)
        {
            yield break;
        }

        OnAllNodesRevealed?.Invoke();

        SetPhase(
            ConstellationPathPhase.Ready);

        NotifyTimerChanged();

        yield return WaitUnscaled(
            _sequenceData.TimerStartDelay);

        if (_phase !=
            ConstellationPathPhase.Ready)
        {
            yield break;
        }

        SetPhase(
            ConstellationPathPhase.Playing);

        NotifyAllNodeStates();
        NotifyAvailableNodes();

        OnInputStarted?.Invoke();

        while (_phase ==
               ConstellationPathPhase.Playing)
        {
            _remainingTime -=
                Time.unscaledDeltaTime;

            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;

                NotifyTimerChanged();

                ResolvePath(
                    false);

                yield break;
            }

            NotifyTimerChanged();

            yield return null;
        }

        if (_phase ==
            ConstellationPathPhase.SuccessDrain)
        {
            yield return
                DrainSuccessTimerRoutine();

            ResolvePath(
                true);
        }
    }

    /// <summary>
    /// RevealOrder 기준 별 등장 진행
    /// </summary>
    private IEnumerator RevealNodesRoutine()
    {
        int previousRevealOrder =
            int.MinValue;

        for (int i = 0;
             i < _sortedNodes.Count;
             i++)
        {
            ConstellationPathNodeData nodeData =
                _sortedNodes[i];

            bool isNewRevealGroup =
                previousRevealOrder != int.MinValue &&
                previousRevealOrder !=
                nodeData.RevealOrder;

            if (isNewRevealGroup)
            {
                yield return WaitUnscaled(
                    _sequenceData
                        .StarRevealInterval);
            }

            OnNodeRevealRequested?.Invoke(
                nodeData);

            previousRevealOrder =
                nodeData.RevealOrder;
        }
    }

    /// <summary>
    /// 성공 시 남은 타이머 빠른 소진
    /// </summary>
    private IEnumerator
        DrainSuccessTimerRoutine()
    {
        float drainDuration =
            _sequenceData
                .SuccessTimerDrainDuration;

        float startRemainingTime =
            _remainingTime;

        if (drainDuration <= 0f)
        {
            _remainingTime = 0f;

            NotifyTimerChanged();

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < drainDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    drainDuration);

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f);

            _remainingTime =
                Mathf.Lerp(
                    startRemainingTime,
                    0f,
                    easedProgress);

            NotifyTimerChanged();

            yield return null;
        }

        _remainingTime = 0f;

        NotifyTimerChanged();
    }

    /// <summary>
    /// 별자리 최종 판정 처리
    /// </summary>
    /// <param name="isSuccess">성공 여부</param>
    private void ResolvePath(
        bool isSuccess)
    {
        if (_phase ==
            ConstellationPathPhase.Resolved)
        {
            return;
        }

        SetPhase(
            ConstellationPathPhase.Resolved);

        float remainingTimeAtCompletion =
            isSuccess
                ? _remainingTimeAtCompletion
                : 0f;

        float elapsedInputTime =
            _sequenceData != null
                ? _sequenceData.TimeLimit -
                  remainingTimeAtCompletion
                : 0f;

        ConstellationPathResult result =
            new ConstellationPathResult(
                isSuccess,
                _progress.CompletedCount,
                _progress.TotalCount,
                remainingTimeAtCompletion,
                elapsedInputTime);

        _runRoutine = null;

        OnPathCompleted?.Invoke(
            result);
    }

    /// <summary>
    /// RevealOrder 기준 노드 목록 생성
    /// </summary>
    private void BuildSortedNodeList()
    {
        _sortedNodes.Clear();

        for (int i = 0;
             i < _sequenceData.Nodes.Count;
             i++)
        {
            _sortedNodes.Add(
                _sequenceData.Nodes[i]);
        }

        _sortedNodes.Sort(
            CompareNodeRevealOrder);
    }

    /// <summary>
    /// 노드 등장 순서 비교
    /// </summary>
    /// <param name="left">왼쪽 노드</param>
    /// <param name="right">오른쪽 노드</param>
    /// <returns>비교 결과</returns>
    private int CompareNodeRevealOrder(
        ConstellationPathNodeData left,
        ConstellationPathNodeData right)
    {
        int orderComparison =
            left.RevealOrder.CompareTo(
                right.RevealOrder);

        if (orderComparison != 0)
        {
            return orderComparison;
        }

        return string.Compare(
            left.NodeId,
            right.NodeId,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// 전체 노드 상태 이벤트 전달
    /// </summary>
    private void NotifyAllNodeStates()
    {
        if (_sequenceData == null)
        {
            return;
        }

        for (int i = 0;
             i < _sequenceData.Nodes.Count;
             i++)
        {
            ConstellationPathNodeData nodeData =
                _sequenceData.Nodes[i];

            if (!_progress.TryGetNodeState(
                    nodeData.NodeId,
                    out ConstellationPathNodeState
                        nodeState))
            {
                continue;
            }

            OnNodeStateChanged?.Invoke(
                nodeData.NodeId,
                nodeState);
        }
    }

    /// <summary>
    /// 현재 입력 가능 노드 목록 전달
    /// </summary>
    private void NotifyAvailableNodes()
    {
        _progress.GetAvailableNodeIds(
            _availableNodeIds);

        string[] availableNodeSnapshot =
            _availableNodeIds.ToArray();

        OnAvailableNodesChanged?.Invoke(
            availableNodeSnapshot);
    }

    /// <summary>
    /// 타이머 변경 이벤트 전달
    /// </summary>
    private void NotifyTimerChanged()
    {
        OnTimerChanged?.Invoke(
            NormalizedRemainingTime,
            _remainingTime);
    }

    /// <summary>
    /// 진행 단계 변경
    /// </summary>
    /// <param name="nextPhase">다음 단계</param>
    private void SetPhase(
        ConstellationPathPhase nextPhase)
    {
        if (_phase == nextPhase)
        {
            return;
        }

        _phase = nextPhase;

        OnPhaseChanged?.Invoke(
            _phase);
    }

    /// <summary>
    /// 시간 배율과 무관한 대기
    /// </summary>
    /// <param name="duration">대기 시간</param>
    private IEnumerator WaitUnscaled(
        float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }
}
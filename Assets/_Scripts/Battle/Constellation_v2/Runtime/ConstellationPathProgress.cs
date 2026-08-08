using System;
using System.Collections.Generic;

/// <summary>
/// 별자리 경로 진행 상태
/// 완료 노드와 현재 입력 가능 노드 관리
/// </summary>
public class ConstellationPathProgress
{
    private readonly Dictionary<
        string,
        ConstellationPathNodeData> _nodeMap =
            new Dictionary<
                string,
                ConstellationPathNodeData>(
                    StringComparer.Ordinal);

    private readonly HashSet<string>
        _completedNodeIds =
            new HashSet<string>(
                StringComparer.Ordinal);

    private ConstellationPathSequenceData
        _sequenceData;

    private bool _isInitialized;

    public bool IsInitialized =>
        _isInitialized;

    public int CompletedCount =>
        _completedNodeIds.Count;

    public int TotalCount =>
        _sequenceData != null
            ? _sequenceData.NodeCount
            : 0;

    public bool IsCompleted =>
        _isInitialized &&
        TotalCount > 0 &&
        CompletedCount == TotalCount;

    public ConstellationPathSequenceData
        SequenceData => _sequenceData;

    /// <summary>
    /// 별자리 진행 상태 초기화
    /// </summary>
    /// <param name="sequenceData">사용 시퀀스 데이터</param>
    /// <param name="errorMessage">초기화 실패 메시지</param>
    /// <returns>초기화 성공 여부</returns>
    public bool Initialize(
        ConstellationPathSequenceData sequenceData,
        out string errorMessage)
    {
        Clear();

        if (sequenceData == null)
        {
            errorMessage =
                "별자리 시퀀스 데이터가 없음";

            return false;
        }

        if (!sequenceData.TryValidate(
                out errorMessage))
        {
            return false;
        }

        _sequenceData =
            sequenceData;

        for (int i = 0;
             i < sequenceData.Nodes.Count;
             i++)
        {
            ConstellationPathNodeData nodeData =
                sequenceData.Nodes[i];

            _nodeMap.Add(
                nodeData.NodeId,
                nodeData);
        }

        _isInitialized = true;
        errorMessage = string.Empty;

        return true;
    }

    /// <summary>
    /// 별자리 진행 상태 초기화
    /// </summary>
    public void Reset()
    {
        _completedNodeIds.Clear();
    }

    /// <summary>
    /// 별자리 진행 데이터 제거
    /// </summary>
    public void Clear()
    {
        _sequenceData = null;
        _isInitialized = false;

        _nodeMap.Clear();
        _completedNodeIds.Clear();
    }

    /// <summary>
    /// 노드 존재 여부 반환
    /// </summary>
    /// <param name="nodeId">확인 노드 ID</param>
    /// <returns>존재 여부</returns>
    public bool ContainsNode(
        string nodeId)
    {
        if (string.IsNullOrWhiteSpace(
                nodeId))
        {
            return false;
        }

        return _nodeMap.ContainsKey(
            nodeId);
    }

    /// <summary>
    /// 노드 완료 여부 반환
    /// </summary>
    /// <param name="nodeId">확인 노드 ID</param>
    /// <returns>완료 여부</returns>
    public bool IsNodeCompleted(
        string nodeId)
    {
        if (!_isInitialized)
        {
            return false;
        }

        return _completedNodeIds.Contains(
            nodeId);
    }

    /// <summary>
    /// 노드 입력 가능 여부 반환
    /// </summary>
    /// <param name="nodeId">확인 노드 ID</param>
    /// <returns>입력 가능 여부</returns>
    public bool IsNodeAvailable(
        string nodeId)
    {
        if (!_isInitialized)
        {
            return false;
        }

        if (!_nodeMap.TryGetValue(
                nodeId,
                out ConstellationPathNodeData nodeData))
        {
            return false;
        }

        if (_completedNodeIds.Contains(
                nodeId))
        {
            return false;
        }

        return IsPrerequisiteConditionMet(nodeData);
    }

    /// <summary>
    /// 노드 상태 검색
    /// </summary>
    /// <param name="nodeId">검색 노드 ID</param>
    /// <param name="nodeState">검색 결과 상태</param>
    /// <returns>검색 성공 여부</returns>
    public bool TryGetNodeState(
        string nodeId,
        out ConstellationPathNodeState nodeState)
    {
        if (!_isInitialized ||
            !_nodeMap.ContainsKey(nodeId))
        {
            nodeState =
                ConstellationPathNodeState.Locked;

            return false;
        }

        if (_completedNodeIds.Contains(
                nodeId))
        {
            nodeState =
                ConstellationPathNodeState.Completed;

            return true;
        }

        nodeState =
            IsNodeAvailable(nodeId)
                ? ConstellationPathNodeState.Available
                : ConstellationPathNodeState.Locked;

        return true;
    }

    /// <summary>
    /// 노드 완료 입력 처리
    /// </summary>
    /// <param name="nodeId">입력 노드 ID</param>
    /// <param name="inputResult">입력 판정 결과</param>
    /// <returns>입력 성공 여부</returns>
    public bool TryCompleteNode(
        string nodeId,
        out ConstellationPathInputResult inputResult)
    {
        if (!_isInitialized)
        {
            inputResult =
                ConstellationPathInputResult.NotInitialized;

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                nodeId) ||
            !_nodeMap.TryGetValue(
                nodeId,
                out ConstellationPathNodeData nodeData))
        {
            inputResult =
                ConstellationPathInputResult.UnknownNode;

            return false;
        }

        if (_completedNodeIds.Contains(
                nodeId))
        {
            inputResult =
                ConstellationPathInputResult.AlreadyCompleted;

            return false;
        }

        if (!IsPrerequisiteConditionMet(nodeData))
        {
            inputResult = ConstellationPathInputResult.Locked;
            return false;
        }

        _completedNodeIds.Add(
            nodeId);

        inputResult =
            ConstellationPathInputResult.Accepted;

        return true;
    }

    /// <summary>
    /// 현재 입력 가능한 노드 ID 목록 생성
    /// </summary>
    /// <param name="result">결과 저장 목록</param>
    public void GetAvailableNodeIds(
        List<string> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        if (!_isInitialized)
        {
            return;
        }

        for (int i = 0;
             i < _sequenceData.Nodes.Count;
             i++)
        {
            ConstellationPathNodeData nodeData =
                _sequenceData.Nodes[i];

            if (!IsNodeAvailable(
                    nodeData.NodeId))
            {
                continue;
            }

            result.Add(
                nodeData.NodeId);
        }
    }

    /// <summary>
    /// 완료한 노드 ID 목록 생성
    /// </summary>
    /// <param name="result">결과 저장 목록</param>
    public void GetCompletedNodeIds(
        List<string> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        if (!_isInitialized)
        {
            return;
        }

        for (int i = 0;
             i < _sequenceData.Nodes.Count;
             i++)
        {
            string nodeId =
                _sequenceData.Nodes[i].NodeId;

            if (!_completedNodeIds.Contains(
                    nodeId))
            {
                continue;
            }

            result.Add(nodeId);
        }
    }

    /// <summary>
    /// 노드 입력 조건 충족 여부 반환
    /// 선행 노드가 없으면 시작 노드, 여러 개면 하나 이상 완료 시 허용
    /// </summary>
    /// <param name="nodeData">확인 노드 데이터</param>
    /// <returns>입력 조건 충족 여부</returns>
    private bool IsPrerequisiteConditionMet(ConstellationPathNodeData nodeData)
    {
        if (nodeData == null)
        {
            return false;
        }

        if (nodeData.PrerequisiteNodeIds.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < nodeData.PrerequisiteNodeIds.Count; i++)
        {
            string prerequisiteNodeId = nodeData.PrerequisiteNodeIds[i];

            if (_completedNodeIds.Contains(prerequisiteNodeId))
            {
                return true;
            }
        }

        return false;
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경로형 별자리 시퀀스 데이터
/// 별 배치, 분기 구조, 제한 시간 정의
/// </summary>
[CreateAssetMenu(
    fileName = "ConstellationPathSequence",
    menuName =
        "WitchChronicle/Constellation Path/Sequence Data")]
public class ConstellationPathSequenceData :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _sequenceId;

    [Header("Time")]
    [Tooltip("모든 별 등장 후 제공되는 입력 제한 시간")]
    [SerializeField, Min(0.1f)]
    private float _timeLimit = 3f;

    [Tooltip("별 하나가 등장한 뒤 다음 별이 등장하기까지의 간격")]
    [SerializeField, Min(0f)]
    private float _starRevealInterval = 0.08f;

    [Tooltip("모든 별 등장 후 타이머가 줄어들기 전 인식 대기 시간")]
    [SerializeField, Min(0f)]
    private float _timerStartDelay = 0.18f;

    [Tooltip("모든 별 완료 시 남은 타이머가 0까지 빠르게 줄어드는 시간")]
    [SerializeField, Min(0f)]
    private float _successTimerDrainDuration = 0.12f;

    [Header("Nodes")]
    [SerializeField]
    private List<ConstellationPathNodeData> _nodes =
        new List<ConstellationPathNodeData>();

    public string SequenceId =>
        _sequenceId;

    public float TimeLimit =>
        _timeLimit;

    public float StarRevealInterval =>
        _starRevealInterval;

    public float TimerStartDelay =>
        _timerStartDelay;

    public float SuccessTimerDrainDuration =>
        _successTimerDrainDuration;

    public IReadOnlyList<ConstellationPathNodeData>
        Nodes => _nodes;

    public int NodeCount =>
        _nodes.Count;

    /// <summary>
    /// 노드 ID로 데이터 검색
    /// </summary>
    /// <param name="nodeId">검색 노드 ID</param>
    /// <param name="nodeData">검색 결과</param>
    /// <returns>검색 성공 여부</returns>
    public bool TryGetNode(
        string nodeId,
        out ConstellationPathNodeData nodeData)
    {
        for (int i = 0;
             i < _nodes.Count;
             i++)
        {
            ConstellationPathNodeData currentNode =
                _nodes[i];

            if (currentNode == null)
            {
                continue;
            }

            if (currentNode.NodeId != nodeId)
            {
                continue;
            }

            nodeData = currentNode;

            return true;
        }

        nodeData = null;

        return false;
    }

    /// <summary>
    /// 전체 별자리 데이터 유효성 검사
    /// </summary>
    /// <param name="errorMessage">검사 실패 메시지</param>
    /// <returns>유효 여부</returns>
    public bool TryValidate(
        out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(
                _sequenceId))
        {
            errorMessage =
                "SequenceId가 비어 있음";

            return false;
        }

        if (_timeLimit <= 0f)
        {
            errorMessage =
                "제한 시간은 0보다 커야 함";

            return false;
        }

        if (_nodes == null ||
            _nodes.Count == 0)
        {
            errorMessage =
                "별자리 노드가 없음";

            return false;
        }

        Dictionary<
            string,
            ConstellationPathNodeData> nodeMap =
                new Dictionary<
                    string,
                    ConstellationPathNodeData>();

        int rootNodeCount = 0;

        for (int i = 0;
             i < _nodes.Count;
             i++)
        {
            ConstellationPathNodeData nodeData =
                _nodes[i];

            if (nodeData == null)
            {
                errorMessage =
                    $"Nodes[{i}]가 비어 있음";

                return false;
            }

            if (!nodeData.TryValidate(
                    out string nodeErrorMessage))
            {
                errorMessage =
                    nodeErrorMessage;

                return false;
            }

            if (nodeMap.ContainsKey(
                    nodeData.NodeId))
            {
                errorMessage =
                    $"중복 NodeId: {nodeData.NodeId}";

                return false;
            }

            nodeMap.Add(
                nodeData.NodeId,
                nodeData);

            if (nodeData.PrerequisiteNodeIds.Count == 0)
            {
                rootNodeCount++;
            }
        }

        if (rootNodeCount == 0)
        {
            errorMessage =
                "처음 입력 가능한 시작 노드가 없음";

            return false;
        }

        foreach (
            ConstellationPathNodeData nodeData
            in nodeMap.Values)
        {
            for (int i = 0;
                 i < nodeData.PrerequisiteNodeIds.Count;
                 i++)
            {
                string prerequisiteId =
                    nodeData.PrerequisiteNodeIds[i];

                if (nodeMap.ContainsKey(
                        prerequisiteId))
                {
                    continue;
                }

                errorMessage =
                    $"{nodeData.NodeId}: 존재하지 않는 " +
                    $"선행 노드 {prerequisiteId}";

                return false;
            }
        }

        Dictionary<string, int> visitStates =
            new Dictionary<string, int>();

        foreach (string nodeId in nodeMap.Keys)
        {
            visitStates[nodeId] = 0;
        }

        foreach (string nodeId in nodeMap.Keys)
        {
            if (!HasDependencyCycle(
                    nodeId,
                    nodeMap,
                    visitStates))
            {
                continue;
            }

            errorMessage =
                $"순환 의존성 발견: {nodeId}";

            return false;
        }

        errorMessage = string.Empty;

        return true;
    }

    /// <summary>
    /// 노드 의존성 순환 검사
    /// </summary>
    /// <param name="nodeId">검사 노드 ID</param>
    /// <param name="nodeMap">전체 노드 맵</param>
    /// <param name="visitStates">방문 상태</param>
    /// <returns>순환 존재 여부</returns>
    private bool HasDependencyCycle(
        string nodeId,
        IReadOnlyDictionary<
            string,
            ConstellationPathNodeData> nodeMap,
        IDictionary<string, int> visitStates)
    {
        int currentState =
            visitStates[nodeId];

        if (currentState == 1)
        {
            return true;
        }

        if (currentState == 2)
        {
            return false;
        }

        visitStates[nodeId] = 1;

        ConstellationPathNodeData nodeData =
            nodeMap[nodeId];

        for (int i = 0;
             i < nodeData.PrerequisiteNodeIds.Count;
             i++)
        {
            string prerequisiteId =
                nodeData.PrerequisiteNodeIds[i];

            if (HasDependencyCycle(
                    prerequisiteId,
                    nodeMap,
                    visitStates))
            {
                return true;
            }
        }

        visitStates[nodeId] = 2;

        return false;
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 단일 노드 데이터
/// 위치, 등장 순서, 선행 노드 정의
/// </summary>
[Serializable]
public class ConstellationPathNodeData
{
    [Header("Identity")]
    [SerializeField]
    private string _nodeId;

    [Header("Position")]
    [SerializeField]
    private Vector2 _normalizedPosition =
        new Vector2(0.5f, 0.5f);

    [Header("Presentation")]
    [Tooltip("별 등장 순서. 같은 값은 동시에 등장 가능")]
    [SerializeField, Min(0)]
    private int _revealOrder;

    [Header("Progression")]
    [Tooltip("이 별을 누르기 전에 완료되어야 하는 별 ID 목록")]
    [SerializeField]
    private List<string> _prerequisiteNodeIds =
        new List<string>();

    public string NodeId => _nodeId;

    public Vector2 NormalizedPosition =>
        _normalizedPosition;

    public int RevealOrder =>
        _revealOrder;

    public IReadOnlyList<string> PrerequisiteNodeIds =>
        _prerequisiteNodeIds;

    /// <summary>
    /// 노드 데이터 유효성 검사
    /// </summary>
    /// <param name="errorMessage">검사 실패 메시지</param>
    /// <returns>유효 여부</returns>
    public bool TryValidate(
        out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(_nodeId))
        {
            errorMessage =
                "NodeId가 비어 있음";

            return false;
        }

        if (_normalizedPosition.x < 0f ||
            _normalizedPosition.x > 1f ||
            _normalizedPosition.y < 0f ||
            _normalizedPosition.y > 1f)
        {
            errorMessage =
                $"{_nodeId}: 위치가 0~1 범위를 벗어남";

            return false;
        }

        HashSet<string> prerequisiteIds =
            new HashSet<string>();

        for (int i = 0;
             i < _prerequisiteNodeIds.Count;
             i++)
        {
            string prerequisiteId =
                _prerequisiteNodeIds[i];

            if (string.IsNullOrWhiteSpace(
                    prerequisiteId))
            {
                errorMessage =
                    $"{_nodeId}: 빈 선행 노드 ID 존재";

                return false;
            }

            if (prerequisiteId == _nodeId)
            {
                errorMessage =
                    $"{_nodeId}: 자기 자신을 선행 노드로 지정";

                return false;
            }

            if (!prerequisiteIds.Add(
                    prerequisiteId))
            {
                errorMessage =
                    $"{_nodeId}: 중복 선행 노드 " +
                    $"{prerequisiteId}";

                return false;
            }
        }

        errorMessage = string.Empty;

        return true;
    }
}
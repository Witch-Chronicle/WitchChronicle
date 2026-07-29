using UnityEngine;

/// <summary>
/// 경로형 별자리 최종 결과
/// </summary>
public readonly struct ConstellationPathResult
{
    public bool IsSuccess { get; }

    public int CompletedNodeCount { get; }

    public int TotalNodeCount { get; }

    public float RemainingTimeAtCompletion { get; }

    public float ElapsedInputTime { get; }

    /// <summary>
    /// 최종 결과 생성
    /// </summary>
    /// <param name="isSuccess">성공 여부</param>
    /// <param name="completedNodeCount">완료 노드 수</param>
    /// <param name="totalNodeCount">전체 노드 수</param>
    /// <param name="remainingTimeAtCompletion">완료 시 남은 시간</param>
    /// <param name="elapsedInputTime">실제 입력 경과 시간</param>
    public ConstellationPathResult(
        bool isSuccess,
        int completedNodeCount,
        int totalNodeCount,
        float remainingTimeAtCompletion,
        float elapsedInputTime)
    {
        IsSuccess = isSuccess;

        CompletedNodeCount =
            Mathf.Max(
                0,
                completedNodeCount);

        TotalNodeCount =
            Mathf.Max(
                0,
                totalNodeCount);

        RemainingTimeAtCompletion =
            Mathf.Max(
                0f,
                remainingTimeAtCompletion);

        ElapsedInputTime =
            Mathf.Max(
                0f,
                elapsedInputTime);
    }
}
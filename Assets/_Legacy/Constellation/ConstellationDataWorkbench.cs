using UnityEngine;

/// <summary>
/// 별자리 데이터 워크벤치
/// 시퀀스 데이터 검사 및 계산값 출력
/// </summary>
public class ConstellationDataWorkbench : MonoBehaviour
{
    [Header("Sequence")]
    [SerializeField] private ConstellationSequenceData _sequenceData;

    [Header("Test")]
    [SerializeField] private bool _runOnStart = true;

    /// <summary>
    /// 워크벤치 시작 시 데이터 검사
    /// </summary>
    private void Start()
    {
        if (!_runOnStart)
        {
            return;
        }

        RunTest();
    }

    /// <summary>
    /// 별자리 시퀀스 데이터 검사
    /// </summary>
    [ContextMenu("Run Sequence Data Test")]
    public void RunTest()
    {
        if (_sequenceData == null)
        {
            Debug.LogWarning(
                "별자리 시퀀스 데이터가 연결되지 않았습니다.",
                this);

            return;
        }

        if (!_sequenceData.TryValidate(out string errorMessage))
        {
            Debug.LogWarning(
                $"별자리 시퀀스 검사 실패: {errorMessage}",
                _sequenceData);

            return;
        }

        Debug.Log(
            $"별자리 시퀀스 검사 시작: {_sequenceData.SequenceId}",
            _sequenceData);

        for (int i = 0; i < _sequenceData.BeatCount; i++)
        {
            ConstellationBeatData beat = _sequenceData.Beats[i];

            Debug.Log(
                $"Beat {i}" +
                $"\n발사 시각: {beat.ProjectileLaunchTime:F2}초" +
                $"\n별 표시 시각: {beat.StarShowTime:F2}초" +
                $"\n충돌 시각: {beat.ImpactTime:F2}초" +
                $"\n투사체 이동 시간: {beat.ProjectileTravelDuration:F2}초" +
                $"\n별 위치: {beat.NormalizedStarPosition}",
                _sequenceData);
        }

        Debug.Log(
            $"별자리 시퀀스 검사 완료" +
            $"\n박자 수: {_sequenceData.BeatCount}" +
            $"\n전체 길이: {_sequenceData.Duration:F2}초",
            _sequenceData);
    }
}
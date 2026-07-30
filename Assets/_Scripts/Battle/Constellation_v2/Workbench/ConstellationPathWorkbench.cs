using UnityEngine;

/// <summary>
/// 경로형 별자리 독립 테스트 실행기
/// </summary>
public class ConstellationPathWorkbench :
    MonoBehaviour
{
    [SerializeField]
    private ConstellationPathController _pathController;

    [SerializeField]
    private ConstellationPathSequenceData _sequenceData;

    [SerializeField]
    private bool _playOnStart = true;

    /// <summary>
    /// 자동 테스트 실행
    /// </summary>
    private void Start()
    {
        if (!_playOnStart)
        {
            return;
        }

        PlayTest();
    }

    /// <summary>
    /// 테스트 시퀀스 실행
    /// </summary>
    [ContextMenu("Play Test")]
    public void PlayTest()
    {
        if (_pathController == null)
        {
            Debug.LogWarning(
                "[ConstellationPath] Controller 참조 없음",
                this);

            return;
        }

        if (_sequenceData == null)
        {
            Debug.LogWarning(
                "[ConstellationPath] SequenceData 참조 없음",
                this);

            return;
        }

        _pathController.StartPath(
            _sequenceData);
    }
}
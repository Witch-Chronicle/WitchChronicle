using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 별자리 공격 시퀀스 데이터
/// 투사체 연속 공격과 별자리 입력 순서 정의
/// </summary>
[CreateAssetMenu(
    fileName = "ConstellationSequenceData",
    menuName = "Witch Chronicle/Battle/Constellation Sequence")]
public class ConstellationSequenceData : ScriptableObject
{
    [Header("Sequence")]
    [SerializeField] private string _sequenceId;

    [Header("Beats")]
    [SerializeField]
    private List<ConstellationBeatData> _beats =
        new List<ConstellationBeatData>();

    public string SequenceId => _sequenceId;
    public IReadOnlyList<ConstellationBeatData> Beats => _beats;

    public int BeatCount => _beats.Count;

    public float Duration
    {
        get
        {
            if (_beats == null || _beats.Count == 0)
            {
                return 0f;
            }

            ConstellationBeatData lastBeat = _beats[_beats.Count - 1];

            return lastBeat.ImpactTime + lastBeat.GoodWindow;
        }
    }

    /// <summary>
    /// 공격 시퀀스 데이터 유효성 검사
    /// </summary>
    public bool TryValidate(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(_sequenceId))
        {
            errorMessage = "SequenceId가 비어 있습니다.";
            return false;
        }

        if (_beats == null || _beats.Count == 0)
        {
            errorMessage = "Beat 데이터가 존재하지 않습니다.";
            return false;
        }

        float previousImpactTime = -1f;

        for (int i = 0; i < _beats.Count; i++)
        {
            ConstellationBeatData beat = _beats[i];

            if (beat == null)
            {
                errorMessage = $"{i}번 Beat가 비어 있습니다.";
                return false;
            }

            if (!beat.TryValidate(out string beatErrorMessage))
            {
                errorMessage = $"{i}번 Beat 오류: {beatErrorMessage}";
                return false;
            }

            if (beat.ImpactTime <= previousImpactTime)
            {
                errorMessage =
                    $"{i}번 Beat의 ImpactTime이 이전 Beat보다 빠르거나 같습니다.";
                return false;
            }

            previousImpactTime = beat.ImpactTime;
        }

        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// 인스펙터 데이터 검사
    /// </summary>
    [ContextMenu("Validate Sequence")]
    private void ValidateSequence()
    {
        if (TryValidate(out string errorMessage))
        {
            Debug.Log(
                $"별자리 시퀀스 유효성 검사 성공: {_sequenceId}",
                this);
            return;
        }

        Debug.LogWarning(
            $"별자리 시퀀스 유효성 검사 실패: {errorMessage}",
            this);
    }
}
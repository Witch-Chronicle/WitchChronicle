using UnityEngine;

public enum FishGrade { Common, Rare, Legendary }

[CreateAssetMenu(fileName = "NewFish", menuName = "WitchChronicle/Fish Item Data")]
public class FishItemData : MaterialItemData
{
    [Header("낚시 정보")]
    public FishGrade grade;

    [Header("등장 확률")]
    [Tooltip("같은 등급 안에서 상대 확률 (클수록 자주 나옴)")]
    public float spawnWeight = 1f;

    [Header("QTE 난이도")]
    [Tooltip("물고기가 오른쪽으로 당기는 힘 (클수록 어려움)")]
    [Range(0f, 10f)]
    public float tensionRange = 0.3f;

    [Tooltip("잔진동 크기 (클수록 예측 어려움)")]
    [Range(0f, 10f)]
    public float tensionShake = 0.3f;

    [Tooltip("홀드 시 당기는 속도 (작을수록 어려움)")]
    [Range(0.1f, 10f)]
    public float playerPullSpeed = 0.5f;

    [Tooltip("낚시에 걸리는 시간 (초) - 초록 구간 유지해야 하는 총 시간")]
    public float reelDuration = 8f;

    [Header("제한 시간")]
    [Tooltip("이 시간 안에 진행 게이지 못 채우면 실패 (0 이하면 제한 없음)")]
    public float timeLimit = 10f;

    [Header("낚싯대 요구")]
    [Range(1, 3)]
    public int minRodRank = 1;   // 1=초보, 2=강화, 3=마력
}
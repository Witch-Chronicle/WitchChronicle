using UnityEngine;

public abstract class RoomInteraction : MonoBehaviour
{
    /// <summary>
    /// 방 진입 시 실행될 구체적인 상호작용 로직
    /// </summary>
    /// <param name="playerTransform">플레이어 위치 정보</param>
    public abstract void Execute(Vector3 roomCenter);
}
using UnityEngine;

public class ExitRoomInteraction : RoomInteraction
{
    private bool _isCleared;

    private GameObject _exitPortalPrefab;

    private float _yOffset;

    public void Setup(GameObject exitPortalPrefab, float yOffset)
    {
        _exitPortalPrefab = exitPortalPrefab;
        _yOffset = yOffset;
    }

    /// <summary>
    /// 부모 클래스의 Execute를 재정의하여 출구 방 진입 시 포탈 생성
    /// </summary>
    /// <param name="roomCenter">방의 중심 위치</param>
    public override void Execute(Vector3 roomCenter)
    {
        Vector3 spawnPosition = roomCenter;

        spawnPosition.y = _yOffset;
        
        if (_isCleared)
        {
            return;
        }

        Instantiate(_exitPortalPrefab, spawnPosition, Quaternion.identity);

        Debug.Log("Exit Portal Created");
    }
}
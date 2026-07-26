using UnityEngine;

public class ShopRoomInteraction : RoomInteraction
{
    private GameObject _merchantPrefab; // 상인 또는 가판대 프리팹

    private float _yOffset;

    public void Setup(GameObject prefab, float yOffset)
    {
        _merchantPrefab = prefab;

        _yOffset = yOffset;
    }

    /// <summary>
    /// 상점 방 용 상호작용을 위한 재정의 함수
    /// </summary>
    /// <param name="playerTransform">플레이어 위치</param>
    /// <param name="roomCenter">스폰 위치</param>
    public override void Execute(Vector3 roomCenter)
    {
        if (_merchantPrefab == null) return;
        
        Vector3 spawnPosition = roomCenter;

        spawnPosition.y = _yOffset;

        // 방 중앙에 상인 NPC 생성
        GameObject merchant = Instantiate(_merchantPrefab, spawnPosition, Quaternion.identity);
        
        // 상점 전용 초기화 로직(예: 판매 아이템 리스트 세팅)이 필요하다면 여기서 수행함
        Debug.Log("상점방 생성 완료: 상품 라인업 배치.");
    }
}
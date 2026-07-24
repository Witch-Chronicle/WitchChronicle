using Unity.VisualScripting;
using UnityEngine;

public class EventRoomInteraction : RoomInteraction
{
    private EventRoomTableSO _eventTable;
    private float _spawnRadius = 2f;
    private float _yOffset;

   public void Setup(EventRoomTableSO eventTable, float yOffset)
    {
        _eventTable = eventTable;
        _yOffset = yOffset;
    }

    /// <summary>
    /// 이벤트 룸 전용 상호작용 시 호출될 함수
    /// </summary>
    /// <param name="playerTransform">플레이어의 위치</param>
    /// <param name="spawnPosition">스폰될 위치</param>
    public override void Execute(Vector3 roomCenter)
    {
        if (_eventTable == null)
        {
            Debug.LogWarning("[EventRoomInteraction] 이벤트 테이블 누락되었습니다.");
            return;
        }

        // 테이블에서 무작위 이벤트 데이터 추출
        EventDataSO selectedEventData = _eventTable.GetRandomEvent();

        if (selectedEventData == null)
        {
            Debug.LogWarning("[EventRoomInteraction] 유효한 이벤트 데이터가 없습니다.");
            return;
        }

        Vector3 spawnPosition = roomCenter + Random.insideUnitSphere * _spawnRadius;
        spawnPosition.y = _yOffset;

        // 공통 이벤트 오브젝트 생성
        GameObject eventObjInstance = Instantiate(selectedEventData.Prefab, spawnPosition, Quaternion.identity);
        
        EventGameObject eventComponent = eventObjInstance.GetComponentInChildren<EventGameObject>();

        // 이벤트 오브젝트에 데이터 주입
        if (eventComponent != null)
        {
            eventComponent.Setup(selectedEventData);
        }

        Debug.Log($"[EventRoomInteraction] 이벤트 방 생성 완료: {selectedEventData.EventName}");
    }
}
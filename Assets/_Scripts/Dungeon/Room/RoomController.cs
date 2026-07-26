using UnityEngine;

public class RoomController : MonoBehaviour
{
    private RoomNode _roomData;
    private float _tileSize;
    private MinimapMarker _minimapMarker; 
    private RoomInteraction _roomInteraction; 
    private bool _hasVisited = false;
    private Vector3 _roomCenterFloor;

    private RectInt _bounds;

    /// <summary>
    /// 박스 콜라이더를 추가하고 방 크기에 맞게(판정을 위해 y 춛에 0.5) 생성됨, 플레이어가 방에 들어왔는지 판별
    /// </summary>
    /// <param name="roomData">방의 데이터</param>
    /// <param name="tileSize">타일 의 크기</param>
    public void Initialize(RoomNode roomData, float tileSize)
    {
        _roomData = roomData;
        _tileSize = tileSize;

        BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;

        Vector3 size = new Vector3(roomData.Bounds.width * _tileSize, 5f, roomData.Bounds.height * _tileSize);
        trigger.size = size;
        trigger.center = Vector3.zero;

        // 기준 좌표 정의를 이곳에서 단일화
        _roomCenterFloor = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    public bool IsInsideRoom(Vector3 worldPos, float tileSize)
    {
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(worldPos.x / tileSize),
            Mathf.RoundToInt(worldPos.z / tileSize)
        );


        return _bounds.Contains(gridPos);
    }

    /// <summary>
    /// 외부(Spawner)에서 결합이 완료된 전략 컴포넌트의 참조를 주입받음 
    /// </summary>
    public void InjectInteraction(RoomInteraction interaction)
    {
        _roomInteraction = interaction;
    }

    // 참조 받아 재등록하는 함수
    public void RegisterMinimapMarker(MinimapMarker marker)
    {
        _minimapMarker = marker;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasVisited) return;

        if (other.CompareTag("Player"))
        {
            OnPlayerEnterRoom();
        }
    }

    /// <summary>
    /// 방에 플레이어가 들어왔을때, 미니맵의 마커를 드러내는 함수
    /// </summary>
    /// <param name="playerTransform">플레이어 위치</param>
    private void OnPlayerEnterRoom()
    {
        _hasVisited = true;

        if (_minimapMarker != null)
        {
            _minimapMarker.Reveal();
        }
    }

    public void SpawnRoomContent()
    {
        if (_roomInteraction != null )
        {
            _roomInteraction.Execute(_roomCenterFloor);
        }
    }

}
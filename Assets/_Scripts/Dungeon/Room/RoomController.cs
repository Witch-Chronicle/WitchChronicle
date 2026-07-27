using UnityEngine;

/// <summary>
/// 개별 방의 상태와 플레이어 진입 이벤트를 관리한다.
/// </summary>
public class RoomController : MonoBehaviour
{
    private RoomNode _roomData;
    private float _tileSize;
    private RoomInteraction _roomInteraction;
    private bool _hasVisited;
    private Vector3 _roomCenterFloor;

    /// <summary>
    /// 방 데이터와 트리거 영역을 초기화한다.
    /// </summary>
    /// <param name="roomData">방 데이터</param>
    /// <param name="tileSize">타일 크기</param>
    public void Initialize(RoomNode roomData, float tileSize)
    {
        _roomData = roomData;
        _tileSize = tileSize;
        _roomCenterFloor = transform.position;

        CreateTrigger();
    }

    /// <summary>
    /// 방 상호작용 전략을 주입한다.
    /// </summary>
    /// <param name="interaction">방 행동 인터랙션</param>
    public void InjectInteraction(RoomInteraction interaction)
    {
        _roomInteraction = interaction;
    }

    /// <summary>
    /// 방 진입 감지를 위한 BoxCollider 트리거를 생성한다.
    /// </summary>
    private void CreateTrigger()
    {
        BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
        
        trigger.isTrigger = true;
        trigger.size = new Vector3(
            _roomData.Bounds.width * _tileSize,
            5f,
            _roomData.Bounds.height * _tileSize
        );

        Debug.Log($"[RoomController] 방 트리거 생성 완료: {_roomData.Type} (크기: {trigger.size})");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[RoomController] 충돌 감지됨 - 오브젝트: {other.name}, 태그: {other.tag}");

        if (!other.CompareTag("Player"))
        {
            Debug.Log("[RoomController] 충돌한 오브젝트의 태그가 'Player'가 아닙니다.");
            return;
        }

        if (_hasVisited)
        {
            Debug.Log($"[RoomController] 이미 방문했던 방입니다: {_roomData.Type}");
            return;
        }

        DiscoverRoom();
    }

    /// <summary>
    /// 플레이어가 처음 방에 진입했을 때 실행된다.
    /// </summary>
    private void DiscoverRoom()
    {
        _hasVisited = true;
        _roomData.Discover();

        MinimapIconManager minimap = MinimapIconManager.Instance;
        if (minimap != null)
        {
            minimap.RefreshRoom(_roomData);
        }
        else
        {
            Debug.LogWarning("[RoomController] 씬에 MinimapIconManager 인스턴스가 존재하지 않습니다.");
        }

        Debug.Log($"[RoomController] 방 발견 처리 완료 및 미니맵 아이콘 갱신 요청: {_roomData.Type}");
    }

    /// <summary>
    /// 방 내부의 상호작용 콘텐츠를 실행한다.
    /// </summary>
    public void SpawnRoomContent()
    {
        if (_roomInteraction == null)
        {
            return;
        }

        _roomInteraction.Execute(_roomCenterFloor);
    }
}
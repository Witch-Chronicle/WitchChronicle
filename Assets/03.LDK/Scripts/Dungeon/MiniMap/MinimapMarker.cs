using UnityEngine;

public class MinimapMarker : MonoBehaviour
{
    [SerializeField] private GameObject _unknownIcon; // '?' 아이콘 오브젝트
    [SerializeField] private GameObject _startIcon;    // 상점 아이콘
    [SerializeField] private GameObject _battleIcon;  // 칼 모양 등 전투 방 아이콘
    [SerializeField] private GameObject _bossIcon;    // 해골 모양 등 보스 방 아이콘
    [SerializeField] private GameObject _shopIcon;    // 상점 아이콘
    [SerializeField] private GameObject _eventIcon;    // 상점 아이콘
    [SerializeField] private GameObject _treasureIcon;    // 상점 아이콘

    [SerializeField] private GameObject _exitIcon;

    // 생성된 인스턴스들을 추적하기 위한 내부 변수
    private GameObject _instantiatedUnknown;
    private GameObject _instantiatedRoomIcon;

    /// <summary>
    /// 마커가 생성될 때 최초 상태를 설정함. '?'와 실제 방 아이콘을 모두 생성한 뒤 '?'만 켜고 나머지는 모두 숨김.
    /// </summary>
    /// <param name="roomType">사전에 미리 생성해 둘 방의 타입</param>
    public void SetupDefault(RoomType roomType)
    {
        // 1. '?' 아이콘 실시간 생성 및 활성화
        if (_unknownIcon != null)
        {
            _instantiatedUnknown = Instantiate(_unknownIcon, transform.position, transform.rotation, transform);
            _instantiatedUnknown.SetActive(true);
        }

        // 2. 방 타입에 일치하는 실제 아이콘 프리팹 선택
        GameObject prefabToSpawn = roomType switch
        {
            RoomType.Start => _startIcon,
            RoomType.Battle => _battleIcon,
            RoomType.Boss => _bossIcon,
            RoomType.Shop => _shopIcon,
            RoomType.Treasure => _treasureIcon,
            RoomType.Event => _eventIcon,
            RoomType.Exit => _exitIcon,
            _ => null
        };

        // 3. 실제 방 아이콘 실시간 생성 및 비활성화 (숨김 처리)
        if (prefabToSpawn != null)
        {
            _instantiatedRoomIcon = Instantiate(prefabToSpawn, transform.position, transform.rotation, transform);
            _instantiatedRoomIcon.SetActive(false); 
        }
    }

    /// <summary>
    /// 방 진입 시 호출되어 '?'를 끄고, 방 타입에 맞는 실제 아이콘을 활성화함.
    /// </summary>
    public void Reveal()
    {
        // 이미 생성되어 있는 '?' 아이콘을 비활성화
        if (_instantiatedUnknown != null)
        {
            _instantiatedUnknown.SetActive(false);
        }

        // 이미 생성되어 숨겨져 있던 실제 방 아이콘을 활성화
        if (_instantiatedRoomIcon != null)
        {
            _instantiatedRoomIcon.SetActive(true);
        }
    }
}
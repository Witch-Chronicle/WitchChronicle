using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵에 표시되는 하나의 방 아이콘을 관리한다.
/// </summary>
public class MinimapRoomIcon : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage _image;

    [Header("Icons")]
    [SerializeField] private Sprite _unknownIcon;
    [SerializeField] private Sprite _startIcon;
    [SerializeField] private Sprite _battleIcon;
    [SerializeField] private Sprite _treasureIcon;
    [SerializeField] private Sprite _shopIcon;
    [SerializeField] private Sprite _eventIcon;
    [SerializeField] private Sprite _bossIcon;
    [SerializeField] private Sprite _exitIcon;

    [Header("Settings")]
    [SerializeField] private Vector2 _iconSize = new Vector2(24f, 24f);

    private RoomNode _room;

    public RoomNode Room => _room;

    /// <summary>
    /// 방 데이터를 연결하고 초기 크기 및 상태를 설정한다.
    /// </summary>
    public void Setup(RoomNode room)
    {
        _room = room;
        ApplyIconSize();
        Refresh();
    }

    /// <summary>
    /// 방 방문 상태에 따라 아이콘 텍스처를 갱신한다.
    /// </summary>
    public void Refresh()
    {
        if (_room == null)
        {
            return;
        }

        MinimapIconType iconType = MinimapIconUtility.GetIconType(_room);
        Sprite targetSprite = GetSprite(iconType);

        if (_image != null && targetSprite != null && targetSprite.texture != null)
        {
            _image.color = Color.white;
            _image.texture = targetSprite.texture;
        }
    }

    /// <summary>
    /// 아이콘 타입에 맞는 스프라이트를 반환한다.
    /// </summary>
    private Sprite GetSprite(MinimapIconType iconType)
    {
        return iconType switch
        {
            MinimapIconType.Start => _startIcon,
            MinimapIconType.Battle => _battleIcon,
            MinimapIconType.Treasure => _treasureIcon,
            MinimapIconType.Shop => _shopIcon,
            MinimapIconType.Event => _eventIcon,
            MinimapIconType.Boss => _bossIcon,
            MinimapIconType.Exit => _exitIcon,
            _ => _unknownIcon
        };
    }

    /// <summary>
    /// 방의 Bounds를 이용해 정중앙 좌표를 구하고, uvRect와 부모 크기에 맞춰 뷰 위치와 스케일을 갱신한다.
    /// </summary>
    public void UpdateView(Rect uvRect, int minX, int minY, int maxX, int maxY, RectTransform parentRect)
    {
        if (transform is not RectTransform rect || _room == null || parentRect == null)
        {
            return;
        }

        int totalWidth = maxX - minX + 1;
        int totalHeight = maxY - minY + 1;

        if (totalWidth <= 0 || totalHeight <= 0)
        {
            return;
        }

        // 방의 실제 기하학적 정중앙 산출
        float roomCenterX = (_room.Bounds.min.x + _room.Bounds.max.x) * 0.5f;
        float roomCenterY = (_room.Bounds.min.y + _room.Bounds.max.y) * 0.5f;

        // 전체 맵 기준 정규화 좌표 (0 ~ 1)
        float normX = (roomCenterX - minX) / totalWidth;
        float normY = (roomCenterY - minY) / totalHeight;

        // 줌인/줌아웃(uvRect) 영역 안에서의 상대 좌표 변환 (여기가 핵심)
        float viewNormX = (normX - uvRect.x) / uvRect.width;
        float viewNormY = (normY - uvRect.y) / uvRect.height;

        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        // UI 앵커 포지션 적용 (중앙 기준 앵커일 때 완벽히 일치함)
        float localX = (viewNormX - 0.5f) * parentWidth;
        float localY = (viewNormY - 0.5f) * parentHeight;

        rect.anchoredPosition = new Vector2(localX, localY);
    }

    /// <summary>
    /// 아이콘의 고정 크기를 설정한다.
    /// </summary>
    private void ApplyIconSize()
    {
        if (transform is not RectTransform rect)
        {
            return;
        }

        rect.sizeDelta = _iconSize;
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미니맵 아이콘 이미지(Sprite)를 인스펙터에서 직접 관리하고 갱신을 조율하는 매니저 클래스.
/// </summary>
public class MinimapIconManager : MonoBehaviour
{
    [Header("Icon Settings")]
    [SerializeField] private Sprite _unvisitedIconSprite;
    [SerializeField] private List<RoomIconMapping> _roomIconMappings = new List<RoomIconMapping>();
    
    [Header("References")]
    [SerializeField] private MinimapRenderer _minimapRenderer;
    [SerializeField] private MinimapPlayerIcon _playerIcon; // 플레이어 아이콘 참조 연결

    [Serializable]
    public struct RoomIconMapping
    {
        [SerializeField] private RoomType _roomType;
        [SerializeField] private Sprite _iconSprite;

        public RoomType RoomType => _roomType;
        public Sprite IconSprite => _iconSprite;
    }

    public static MinimapIconManager Instance { get; private set; }

    private Dictionary<RoomType, Texture2D> _iconMap = new Dictionary<RoomType, Texture2D>();
    private Texture2D _unvisitedTexture;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildIconMap();
    }

    /// <summary>
    /// 플레이어 아이콘을 초기화한다.
    /// </summary>
    public void InitializePlayerIcon(Transform playerTransform, float tileSize)
    {
        if (_playerIcon != null)
        {
            _playerIcon.Initialize(playerTransform, tileSize);
            Debug.Log("[MinimapIconManager] 플레이어 아이콘 초기화 전달 완료");
        }
    }

    /// <summary>
    /// 인스펙터의 Sprite들을 Texture2D로 변환하여 딕셔너리를 구축한다.
    /// </summary>
    private void BuildIconMap()
    {
        _iconMap.Clear();

        foreach (RoomIconMapping mapping in _roomIconMappings)
        {
            if (mapping.IconSprite == null)
            {
                continue;
            }

            Texture2D extractedTexture = GetTextureFromSprite(mapping.IconSprite);
            if (extractedTexture != null && !_iconMap.ContainsKey(mapping.RoomType))
            {
                _iconMap.Add(mapping.RoomType, extractedTexture);
            }
        }

        if (_unvisitedIconSprite != null)
        {
            _unvisitedTexture = GetTextureFromSprite(_unvisitedIconSprite);
        }
    }

    /// <summary>
    /// Sprite로부터 Texture2D를 안전하게 추출한다.
    /// </summary>
    private Texture2D GetTextureFromSprite(Sprite sprite)
    {
        Texture2D sourceTex = sprite.texture;

        try
        {
            Color testColor = sourceTex.GetPixel(0, 0);
        }
        catch (System.Exception)
        {
            Debug.LogError($"[MinimapIconManager] 텍스처 '{sourceTex.name}'의 픽셀에 접근할 수 없습니다! 인스펙터 설정에서 'Read/Write Enabled'를 체크해주세요.");
            return null;
        }

        if (sprite.rect.width == sourceTex.width && sprite.rect.height == sourceTex.height)
        {
            return sourceTex;
        }

        try
        {
            Rect rect = sprite.rect;
            Texture2D newTex = new Texture2D(Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height), TextureFormat.RGBA32, false);
            newTex.filterMode = FilterMode.Point;

            Color[] pixels = sourceTex.GetPixels(
                Mathf.RoundToInt(rect.x),
                Mathf.RoundToInt(rect.y),
                Mathf.RoundToInt(rect.width),
                Mathf.RoundToInt(rect.height)
            );

            newTex.SetPixels(pixels);
            newTex.Apply();

            return newTex;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MinimapIconManager] Sprite '{sprite.name}' 텍스처 추출 중 오류 발생: {e.Message}");
            return sourceTex;
        }
    }

    /// <summary>
    /// 등록된 방 타입별 아이콘 텍스처 맵을 반환한다.
    /// </summary>
    public IReadOnlyDictionary<RoomType, Texture2D> GetIconMap()
    {
        return _iconMap;
    }

    /// <summary>
    /// 미방문 시 표시할 물음표 아이콘 텍스처를 반환한다.
    /// </summary>
    public Texture2D GetUnvisitedTexture()
    {
        return _unvisitedTexture;
    }

    /// <summary>
    /// 특정 방의 상태 변화가 생겼을 때 미니맵을 다시 그린다.
    /// </summary>
    public void RefreshRoom(RoomNode roomNode)
    {
        if (_minimapRenderer != null)
        {
            _minimapRenderer.Refresh();
        }

        Debug.Log($"[MinimapIconManager] 방 아이콘 갱신 완료: {roomNode.Type}");
    }

    /// <summary>
    /// 줌/팬 변경 시 플레이어 아이콘의 뷰 위치와 회전을 갱신하도록 전달한다.
    /// </summary>
    public void UpdateAllIconViews(
        Rect uvRect,
        int minX,
        int minY,
        int maxX,
        int maxY,
        RectTransform rawImageRectTransform)
    {
        if (_playerIcon != null)
        {
            _playerIcon.UpdateView(uvRect, minX, minY, maxX, maxY, rawImageRectTransform);
        }
    }
}
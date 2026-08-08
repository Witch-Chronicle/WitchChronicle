using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미니맵 데이터를 Texture2D로 변환하는 순수 로직 클래스.
/// - BuildBase(): 바닥/벽 타일만 그린 베이스 텍스처 생성. 던전 구조가 바뀌지 않는 한 한 번만 호출하면 됨.
/// - DrawIconsOnto(): 이미 만들어진 텍스처 위에 방 아이콘만 다시 그림 (방 발견 시마다 호출).
/// * 개별 SetPixel/GetPixel 호출 대신 배열(GetPixels/SetPixels)로 일괄 처리해서 성능을 크게 개선함.
///   아이콘 텍스처의 픽셀 배열도 캐싱해서, 같은 아이콘을 쓰는 방이 여러 개여도 GetPixels()는 한 번만 호출됨.
/// </summary>
public class MinimapTextureBuilder
{
    private const int PixelsPerTile = 15;
    private const float IconScaleFactor = 0.34f; // 방 크기 대비 아이콘이 차지할 비율

    private static readonly Color ClearColor = new Color(0f, 0f, 0f, 0f);
    private static readonly Color FloorColor = new Color32(140, 140, 140, 255); // 중간 밝기의 그레이 (바닥)
    private static readonly Color WallColor = new Color32(70, 70, 70, 255);     // 어두운 그레이 (벽)

    // 아이콘 텍스처별 픽셀 배열 캐시 - GetPixel 반복 호출 대신 한 번만 GetPixels()로 뽑아서 재사용
    private readonly Dictionary<Texture2D, Color[]> _iconPixelCache = new Dictionary<Texture2D, Color[]>();

    /// <summary>
    /// 바닥/벽 타일만 그려진 베이스 텍스처 생성. 던전 구조가 바뀌지 않는 한 딱 한 번만 호출하면 됨.
    /// </summary>
    public Texture2D BuildBase(MinimapData data)
    {
        if (data == null || data.Rooms == null || data.Rooms.Count == 0)
        {
            Debug.LogWarning("[MinimapTextureBuilder] 미니맵 데이터 또는 방 정보가 유효하지 않습니다.");
            return new Texture2D(2, 2);
        }

        int width = (data.MaxX - data.MinX + 1) * PixelsPerTile;
        int height = (data.MaxY - data.MinY + 1) * PixelsPerTile;

        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false);

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = ClearColor;
        }

        if (data.FloorTiles != null)
        {
            foreach (Vector2Int tile in data.FloorTiles)
            {
                FillTileInBuffer(pixels, width, height, tile, data.MinX, data.MinY, FloorColor);
            }
        }

        if (data.WallTiles != null)
        {
            foreach (Vector2Int tile in data.WallTiles)
            {
                FillTileInBuffer(pixels, width, height, tile, data.MinX, data.MinY, WallColor);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Debug.Log("[MinimapTextureBuilder] 베이스 Texture 생성 완료");

        return texture;
    }

    /// <summary>
    /// 이미 캐싱된 베이스 픽셀 배열(basePixels)을 복제한 뒤 그 위에 방 아이콘들을 그려서 texture에 반영.
    /// basePixels를 넘겨받으므로 texture.GetPixels()를 다시 호출하지 않아 할당이 줄어듦.
    /// </summary>
    public void DrawIconsOntoPixels(
        Texture2D texture,
        Color[] basePixels,
        IReadOnlyList<RoomNode> rooms,
        int minX,
        int minY,
        IReadOnlyDictionary<RoomType, Texture2D> iconMap,
        Texture2D unvisitedIcon)
    {
        Color[] pixels = (Color[])basePixels.Clone();

        if (rooms != null)
        {
            foreach (RoomNode room in rooms)
            {
                Texture2D iconTexture = null;

                if (room.IsDiscovered)
                {
                    if (iconMap != null && iconMap.TryGetValue(room.Type, out Texture2D foundIcon))
                    {
                        iconTexture = foundIcon;
                    }
                }
                else
                {
                    iconTexture = unvisitedIcon;
                }

                DrawIconInBuffer(pixels, texture.width, texture.height, room, minX, minY, iconTexture);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Debug.Log("[MinimapTextureBuilder] 아이콘 Texture 갱신 완료");
    }

    /// <summary>
    /// 아이콘 텍스처의 픽셀 배열을 캐싱해서 반환 (동일 텍스처는 GetPixels()를 한 번만 호출).
    /// </summary>
    private Color[] GetCachedIconPixels(Texture2D iconTexture)
    {
        if (_iconPixelCache.TryGetValue(iconTexture, out Color[] cached))
        {
            return cached;
        }

        Color[] pixels = iconTexture.GetPixels();
        _iconPixelCache[iconTexture] = pixels;
        return pixels;
    }

    /// <summary>
    /// 방의 실제 크기에 비례하여 아이콘 텍스처를 샘플링하고, 대상 픽셀 배열의 중심에 복사한다.
    /// </summary>
    private void DrawIconInBuffer(
        Color[] pixels,
        int textureWidth,
        int textureHeight,
        RoomNode room,
        int minX,
        int minY,
        Texture2D iconTexture)
    {
        int centerX = (room.Center.x - minX) * PixelsPerTile;
        int centerY = (room.Center.y - minY) * PixelsPerTile;

        int iconSize = 100; // 아이콘 크기 (원하시는 크기에 따라 20~28 사이로 조절 가능)
        int iconWidth = iconSize;
        int iconHeight = iconSize;

        if (iconTexture != null)
        {
            Color[] iconPixels = GetCachedIconPixels(iconTexture);
            int iconTexWidth = iconTexture.width;
            int iconTexHeight = iconTexture.height;

            int startX = centerX - (iconWidth / 2);
            int startY = centerY - (iconHeight / 2);

            for (int x = 0; x < iconWidth; x++)
            {
                int pixelX = startX + x;

                if (pixelX < 0 || pixelX >= textureWidth)
                {
                    continue;
                }

                float u = (float)x / iconWidth;
                int sourceX = Mathf.Clamp(Mathf.FloorToInt(u * iconTexWidth), 0, iconTexWidth - 1);

                for (int y = 0; y < iconHeight; y++)
                {
                    int pixelY = startY + y;

                    if (pixelY < 0 || pixelY >= textureHeight)
                    {
                        continue;
                    }

                    float v = (float)y / iconHeight;
                    int sourceY = Mathf.Clamp(Mathf.FloorToInt(v * iconTexHeight), 0, iconTexHeight - 1);

                    Color pixelColor = iconPixels[sourceY * iconTexWidth + sourceX];

                    if (pixelColor.a > 0f)
                    {
                        pixels[pixelY * textureWidth + pixelX] = pixelColor;
                    }
                }
            }
        }
        else
        {
            Color fallbackColor = Color.gray;
            int fallbackSize = Mathf.Max(iconWidth / 3, 4);

            for (int x = -fallbackSize; x <= fallbackSize; x++)
            {
                int pixelX = centerX + x;

                if (pixelX < 0 || pixelX >= textureWidth)
                {
                    continue;
                }

                for (int y = -fallbackSize; y <= fallbackSize; y++)
                {
                    int pixelY = centerY + y;

                    if (pixelY < 0 || pixelY >= textureHeight)
                    {
                        continue;
                    }

                    pixels[pixelY * textureWidth + pixelX] = fallbackColor;
                }
            }
        }
    }

    /// <summary>
    /// 픽셀 배열 버퍼에 타일 하나를 채움 (SetPixel 대신 배열 직접 접근이라 훨씬 빠름).
    /// </summary>
    private void FillTileInBuffer(
        Color[] pixels,
        int textureWidth,
        int textureHeight,
        Vector2Int tile,
        int minX,
        int minY,
        Color color)
    {
        int startX = (tile.x - minX) * PixelsPerTile;
        int startY = (tile.y - minY) * PixelsPerTile;

        for (int x = 0; x < PixelsPerTile; x++)
        {
            int px = startX + x;

            if (px < 0 || px >= textureWidth)
            {
                continue;
            }

            for (int y = 0; y < PixelsPerTile; y++)
            {
                int py = startY + y;

                if (py < 0 || py >= textureHeight)
                {
                    continue;
                }

                pixels[py * textureWidth + px] = color;
            }
        }
    }
}
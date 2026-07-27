using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미니맵 데이터를 Texture2D로 변환하는 순수 로직 클래스.
/// </summary>
public class MinimapTextureBuilder
{
    private const int PixelsPerTile = 15;
    private const float IconScaleFactor = 0.34f; // 방 크기 대비 아이콘이 차지할 비율 (예: 80%)

    private static readonly Color ClearColor = new Color(0f, 0f, 0f, 0f);
    private static readonly Color FloorColor = new Color32(140, 140, 140, 255); // 중간 밝기의 그레이 (바닥)
    private static readonly Color WallColor = new Color32(70, 70, 70, 255);     // 어두운 그레이 (벽)

    /// <summary>
    /// 미니맵 Texture를 생성한다.
    /// </summary>
    public Texture2D Build(
        MinimapData data, 
        IReadOnlyDictionary<RoomType, Texture2D> iconMap, 
        Texture2D unvisitedIcon)
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

        Clear(texture);

        if (data.FloorTiles != null)
        {
            foreach (Vector2Int tile in data.FloorTiles)
            {
                DrawTile(
                    texture,
                    tile,
                    data.MinX,
                    data.MinY,
                    FloorColor);
            }
        }

        if (data.WallTiles != null)
        {
            foreach (Vector2Int tile in data.WallTiles)
            {
                DrawTile(
                    texture,
                    tile,
                    data.MinX,
                    data.MinY,
                    WallColor);
            }
        }

        DrawRoomIcons(
            texture,
            data.Rooms,
            data.MinX,
            data.MinY,
            iconMap,
            unvisitedIcon);

        texture.Apply();

        Debug.Log("[MinimapTextureBuilder] 미니맵 Texture 생성 완료");

        return texture;
    }

    /// <summary>
    /// 방 아이콘들을 Texture 위에 표시한다.
    /// </summary>
    public void DrawRoomIcons(
        Texture2D texture,
        IReadOnlyList<RoomNode> rooms,
        int minX,
        int minY,
        IReadOnlyDictionary<RoomType, Texture2D> iconMap,
        Texture2D unvisitedIcon)
    {
        if (rooms == null)
        {
            return;
        }

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

            DrawIcon(
                texture,
                room,
                minX,
                minY,
                iconTexture);
        }
    }

    /// <summary>
    /// 방의 실제 크기에 비례하여 아이콘 텍스처를 샘플링하고 미니맵 텍스처의 중심에 복사한다.
    /// </summary>
    private void DrawIcon(
        Texture2D texture,
        RoomNode room,
        int minX,
        int minY,
        Texture2D iconTexture)
    {
        int centerX = (room.Center.x - minX) * PixelsPerTile;
        int centerY = (room.Center.y - minY) * PixelsPerTile;

        // 방의 실제 너비와 높이에 비례하여 아이콘 크기 결정 (최소 15픽셀 보장)
        int iconWidth = Mathf.Max(Mathf.RoundToInt(room.Bounds.width * PixelsPerTile * IconScaleFactor), 15);
        int iconHeight = Mathf.Max(Mathf.RoundToInt(room.Bounds.height * PixelsPerTile * IconScaleFactor), 15);

        if (iconTexture != null)
        {
            int startX = centerX - (iconWidth / 2);
            int startY = centerY - (iconHeight / 2);

            for (int x = 0; x < iconWidth; x++)
            {
                for (int y = 0; y < iconHeight; y++)
                {
                    int pixelX = startX + x;
                    int pixelY = startY + y;

                    if (pixelX < 0 ||
                        pixelY < 0 ||
                        pixelX >= texture.width ||
                        pixelY >= texture.height)
                    {
                        continue;
                    }

                    // 원본 아이콘 텍스처에서 비율에 맞게 픽셀을 샘플링
                    float u = (float)x / iconWidth;
                    float v = (float)y / iconHeight;
                    int sourceX = Mathf.Clamp(Mathf.FloorToInt(u * iconTexture.width), 0, iconTexture.width - 1);
                    int sourceY = Mathf.Clamp(Mathf.FloorToInt(v * iconTexture.height), 0, iconTexture.height - 1);

                    Color pixelColor = iconTexture.GetPixel(sourceX, sourceY);

                    if (pixelColor.a > 0f)
                    {
                        texture.SetPixel(pixelX, pixelY, pixelColor);
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
                for (int y = -fallbackSize; y <= fallbackSize; y++)
                {
                    int pixelX = centerX + x;
                    int pixelY = centerY + y;

                    if (pixelX < 0 ||
                        pixelY < 0 ||
                        pixelX >= texture.width ||
                        pixelY >= texture.height)
                    {
                        continue;
                    }

                    texture.SetPixel(pixelX, pixelY, fallbackColor);
                }
            }
        }
    }

    private void DrawTile(
        Texture2D texture,
        Vector2Int tile,
        int minX,
        int minY,
        Color color)
    {
        int startX = (tile.x - minX) * PixelsPerTile;
        int startY = (tile.y - minY) * PixelsPerTile;

        for (int x = 0; x < PixelsPerTile; x++)
        {
            for (int y = 0; y < PixelsPerTile; y++)
            {
                texture.SetPixel(
                    startX + x,
                    startY + y,
                    color);
            }
        }
    }

    private void Clear(Texture2D texture)
    {
        Color[] colors = new Color[texture.width * texture.height];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = ClearColor;
        }

        texture.SetPixels(colors);
    }
}
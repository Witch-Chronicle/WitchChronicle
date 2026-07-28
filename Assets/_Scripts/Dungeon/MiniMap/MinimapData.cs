using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미니맵 생성 및 표시에 필요한 전체 데이터를 관리한다.
/// </summary>
public class MinimapData
{
    public IReadOnlyCollection<Vector2Int> FloorTiles { get; }
    public IReadOnlyCollection<Vector2Int> WallTiles { get; }
    public IReadOnlyList<RoomNode> Rooms { get; }
    public Vector2Int PlayerPosition { get; set; }

    public int MinX { get; private set; }
    public int MinY { get; private set; }
    public int MaxX { get; private set; }
    public int MaxY { get; private set; }

    private const int MapPadding = 8;

    public MinimapData(
        IReadOnlyCollection<Vector2Int> floorTiles,
        IReadOnlyCollection<Vector2Int> wallTiles,
        IReadOnlyList<RoomNode> rooms)
    {
        FloorTiles = floorTiles;
        WallTiles = wallTiles;
        Rooms = rooms;

        CalculateBounds();
    }

    /// <summary>
    /// 던전 전체의 바운드를 계산하고 여백(Padding)을 부여한다.
    /// </summary>
    private void CalculateBounds()
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        if (FloorTiles != null)
        {
            foreach (Vector2Int tile in FloorTiles)
            {
                if (tile.x < minX) { minX = tile.x; }
                if (tile.y < minY) { minY = tile.y; }
                if (tile.x > maxX) { maxX = tile.x; }
                if (tile.y > maxY) { maxY = tile.y; }
            }
        }

        if (WallTiles != null)
        {
            foreach (Vector2Int tile in WallTiles)
            {
                if (tile.x < minX) { minX = tile.x; }
                if (tile.y < minY) { minY = tile.y; }
                if (tile.x > maxX) { maxX = tile.x; }
                if (tile.y > maxY) { maxY = tile.y; }
            }
        }

        if (Rooms != null)
        {
            foreach (RoomNode room in Rooms)
            {
                if (room.Bounds.xMin < minX) { minX = room.Bounds.xMin; }
                if (room.Bounds.yMin < minY) { minY = room.Bounds.yMin; }
                if (room.Bounds.xMax > maxX) { maxX = room.Bounds.xMax; }
                if (room.Bounds.yMax > maxY) { maxY = room.Bounds.yMax; }
            }
        }

        MinX = minX - MapPadding;
        MinY = minY - MapPadding;
        MaxX = maxX + MapPadding;
        MaxY = maxY + MapPadding;

        Debug.Log($"[MinimapData] 패딩이 포함된 던전 바운드 계산 완료 - X: {MinX}~{MaxX}, Y: {MinY}~{MaxY}");
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;


public enum RoomType
{
    Start,
    Battle,
    Treasure,
    Shop,
    Event,
    Boss,
    Exit
}


public class RoomNode
{
    public RectInt Bounds;

    public RoomType Type;

    public List<RoomNode> ConnectedRooms;


    public RoomController RoomControllerInstance
    {
        get;
        set;
    }


    public int Depth;


    public bool IsDiscovered
    {
        get;
        private set;
    }


    public event Action<RoomNode> OnDiscovered;



    /// <summary>
    /// RoomNode 생성자
    /// </summary>
    public RoomNode(RectInt bounds)
    {
        Bounds = bounds;

        Type = RoomType.Battle;

        ConnectedRooms = new List<RoomNode>();

        Depth = 0;

        IsDiscovered = false;
    }



    public Vector2Int Center
    {
        get
        {
            return new Vector2Int(
                Bounds.x + Bounds.width / 2,
                Bounds.y + Bounds.height / 2);
        }
    }



    /// <summary>
    /// 플레이어가 방을 발견 처리한다.
    /// </summary>
    public void Discover()
    {
        if(IsDiscovered)
        {
            return;
        }


        IsDiscovered = true;


        Debug.Log($"[RoomNode] 방 발견 : {Type}");


        OnDiscovered?.Invoke(this);
    }
}
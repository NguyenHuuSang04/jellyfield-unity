using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BlockCell
{
    public Vector2Int LocalSlot; // Tọa độ con nội bộ (0..1, 0..1) trong cụm
    public BlockColor Color;     // Màu của khối nhỏ đó
}

[System.Serializable]
public class ClusterData
{
    // Một cụm có thể gồm nhiều khối nhỏ cấu thành (1 khối đơn, 2 khối, hình chữ L...)
    public List<BlockCell> Blocks = new List<BlockCell>();
}
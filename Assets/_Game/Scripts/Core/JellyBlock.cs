using System.Collections.Generic;
using UnityEngine;

public class JellyBlock
{
    public int Id;
    public BlockColor Color;
    
    // Đảm bảo có dòng public HashSet này và viết đúng chính xác từng chữ:
    public HashSet<Vector2Int> LocalSlots = new HashSet<Vector2Int>();

    public JellyBlock(int id, BlockColor color, IEnumerable<Vector2Int> slots)
    {
        this.Id = id;
        this.Color = color;
        this.LocalSlots = new HashSet<Vector2Int>(slots);
    }
}
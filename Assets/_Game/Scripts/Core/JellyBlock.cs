using System.Collections.Generic;
using UnityEngine;

public class JellyBlock
{
    public int Id;
    public BlockColor Color;
    public HashSet<Vector2Int> LocalSlots = new HashSet<Vector2Int>();

    public List<GameObject> VisualObjs = new List<GameObject>();

    public JellyBlock(int id, BlockColor color, IEnumerable<Vector2Int> slots)
    {
        this.Id = id;
        this.Color = color;
        this.LocalSlots = new HashSet<Vector2Int>(slots);
        
        // Khởi tạo danh sách rỗng khi tạo khối mới
        this.VisualObjs = new List<GameObject>();
    }
}
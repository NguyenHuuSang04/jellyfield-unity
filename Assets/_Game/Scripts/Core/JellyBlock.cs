using System.Collections.Generic;
using UnityEngine;

namespace JellyField.Core
{
    public class JellyBlock
    {
        public int Id { get; set; }
        public BlockColor Color { get; set; }
        public HashSet<Vector2Int> LocalSlots { get; set; } = new HashSet<Vector2Int>();

        public JellyBlock(int id, BlockColor color, IEnumerable<Vector2Int> slots)
        {
            this.Id = id;
            this.Color = color;
            this.LocalSlots = new HashSet<Vector2Int>(slots);
        }
    }
}
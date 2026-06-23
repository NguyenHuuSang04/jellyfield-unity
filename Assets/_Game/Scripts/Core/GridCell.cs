using System.Collections.Generic;
using UnityEngine;

namespace JellyField.Core
{
    public class GridCell
    {
        public Vector2Int Coord { get; set; } // Tọa độ của ô lưới lớn trên bàn chơi (ví dụ: X=0, Y=2)
        public List<JellyBlock> Blocks { get; set; } = new List<JellyBlock>(); // Chứa từ 1 đến 4 khối thạch
        public bool IsActive { get; set; } = true;

        public GridCell(Vector2Int coord)
        {
            this.Coord = coord;
        }

        // Kiểm tra xem một vị trí sub-slot cụ thể (0..1, 0..1) có còn trống không
        public bool IsSlotFree(Vector2Int localPos)
        {
            foreach (var block in Blocks)
            {
                if (block.LocalSlots.Contains(localPos))
                    return false; // Đã bị một khối khác chiếm chỗ
            }
            return true;
        }

        // Kiểm tra xem ô lớn này đã bị lấp đầy hoàn toàn cả 4 slot con chưa
        public bool IsFull()
        {
            int occupiedCount = 0;
            foreach (var block in Blocks)
            {
                occupiedCount += block.LocalSlots.Count;
            }
            return occupiedCount >= 4;
        }
    }
}
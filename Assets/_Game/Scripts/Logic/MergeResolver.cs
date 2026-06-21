using System.Collections.Generic;
using UnityEngine;

public static class MergeResolver
{
    // Hướng quét 4 phía kề cận (Ngang, Dọc - Không quét chéo) nội bộ ô 2x2
    private static readonly Vector2Int[] Directions = {
        new Vector2Int(1, 0),  // Phải
        new Vector2Int(-1, 0), // Trái
        new Vector2Int(0, 1),  // Trên
        new Vector2Int(0, -1)  // Dưới
    };

    public static bool ResolveIntraCellMerge(GridCell cell)
    {
        if (cell.Blocks.Count <= 1) return false; // Không đủ khối để gộp

        // Nhóm các khối thạch đang có trong ô lớn theo màu sắc
        Dictionary<BlockColor, List<JellyBlock>> colorGroups = new Dictionary<BlockColor, List<JellyBlock>>();
        foreach (var block in cell.Blocks)
        {
            if (block.Color == BlockColor.None) continue;
            if (!colorGroups.ContainsKey(block.Color))
                colorGroups[block.Color] = new List<JellyBlock>();
            colorGroups[block.Color].Add(block);
        }

        bool anyMergeHappened = false;
        List<JellyBlock> newBlocksList = new List<JellyBlock>();

        // Duyệt qua từng nhóm màu để xử lý gộp
        foreach (var kvp in colorGroups)
        {
            BlockColor color = kvp.Key;
            List<JellyBlock> blocksOfColor = kvp.Value;

            if (blocksOfColor.Count >= 2)
            {
                // Thuật toán gộp: Gom toàn bộ các Sub-slot đơn lẻ của cùng một màu lại với nhau
                HashSet<Vector2Int> combinedSlots = new HashSet<Vector2Int>();
                List<GameObject> combinedVisuals = new List<GameObject>(); // Lưu visual gom được
                int minId = int.MaxValue;

                foreach (var b in blocksOfColor)
                {
                    combinedSlots.UnionWith(b.LocalSlots);
                    if (b.VisualObjs != null) combinedVisuals.AddRange(b.VisualObjs); // Gom hình ảnh
                    if (b.Id < minId) minId = b.Id;
                }

                // Kiểm tra xem các ô con gộp lại có liền kề nhau hợp lệ không
                if (ValidateAdjacency(combinedSlots))
                {
                    // Tạo một khối thạch mới to hơn đã được hợp nhất diện tích
                    JellyBlock mergedBlock = new JellyBlock(minId, color, combinedSlots);
                    mergedBlock.VisualObjs = combinedVisuals; // Kế thừa trọn vẹn danh sách hình ảnh!

                    newBlocksList.Add(mergedBlock);
                    anyMergeHappened = true;
                }
                else
                {
                    // Nếu không kề nhau (ví dụ nằm chéo góc nhau), giữ nguyên không gộp
                    newBlocksList.AddRange(blocksOfColor);
                }
            }
            else
            {
                // Chỉ có 1 khối màu này, giữ nguyên
                newBlocksList.AddRange(blocksOfColor);
            }
        }

        // Nếu có gộp xảy ra, cập nhật lại danh sách khối thực tế của ô lưới lớn
        if (anyMergeHappened)
        {
            cell.Blocks = newBlocksList;
        }

        return anyMergeHappened;
    }

    // Thuật toán kiểm tra tính liền kề bằng kỹ thuật BFS / Loang đơn giản
    private static bool ValidateAdjacency(HashSet<Vector2Int> slots)
    {
        if (slots.Count <= 1) return true;

        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Bốc đại một ô con làm điểm xuất phát
        var enumerator = slots.GetEnumerator();
        enumerator.MoveNext();
        Vector2Int start = enumerator.Current;

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (var dir in Directions)
            {
                Vector2Int neighbor = current + dir;
                if (slots.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Nếu số ô đi qua bằng đúng tổng số ô ban đầu nghĩa là cụm khối dính liền nhau hợp lệ
        return visited.Count == slots.Count;
    }
}
using System.Collections.Generic;
using UnityEngine;

public static class MatchResolver
{
    public static List<HashSet<JellyBlock>> ResolveInterCellMatch(GridManager gridManager)
    {
        // Danh sách lưu các nhóm khối thạch thỏa mãn điều kiện Match-2 trở lên để chuẩn bị xóa
        List<HashSet<JellyBlock>> matchGroups = new List<HashSet<JellyBlock>>();
        
        // Dùng một HashSet để đánh dấu các khối đã được xếp vào nhóm nào đó rồi, tránh quét trùng
        HashSet<JellyBlock> visitedBlocks = new HashSet<JellyBlock>();

        // Quét qua tất cả các ô lưới đang hoạt động trên bàn chơi
        foreach (var cellA in gridManager.ActiveCells)
        {
            foreach (var blockA in cellA.Blocks)
            {
                if (visitedBlocks.Contains(blockA)) continue;

                // Tạo một nhóm match tiềm năng bắt đầu từ khối blockA này
                HashSet<JellyBlock> currentGroup = new HashSet<JellyBlock>();
                Queue<KeyValuePair<GridCell, JellyBlock>> queue = new Queue<KeyValuePair<GridCell, JellyBlock>>();

                queue.Enqueue(new KeyValuePair<GridCell, JellyBlock>(cellA, blockA));
                currentGroup.Add(blockA);

                while (queue.Count > 0)
                {
                    var currentPair = queue.Dequeue();
                    GridCell currentCell = currentPair.Key;
                    JellyBlock currentBlock = currentPair.Value;

                    // Kiểm tra 4 ô lưới lớn láng giềng xung quanh (Phải, Trái, Trên, Dưới)
                    Vector2Int[] neighborDirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
                    
                    foreach (var dir in neighborDirs)
                    {
                        Vector2Int neighborCoord = currentCell.Coord + dir;
                        if (gridManager.TryGetCell(neighborCoord, out GridCell cellB))
                        {
                            foreach (var blockB in cellB.Blocks)
                            {
                                // Nếu khối hàng xóm cùng màu và chưa bị duyệt qua
                                if (blockB.Color == currentBlock.Color && !currentGroup.Contains(blockB) && !visitedBlocks.Contains(blockB))
                                {
                                    // Kiểm tra xem 2 khối này có thực sự áp sát và chạm nhau qua biên ô lớn không
                                    if (CheckBorderContact(currentCell.Coord, currentBlock, cellB.Coord, blockB))
                                    {
                                        currentGroup.Add(blockB);
                                        queue.Enqueue(new KeyValuePair<GridCell, JellyBlock>(cellB, blockB));
                                    }
                                }
                            }
                        }
                    }
                }

                // Nếu nhóm tìm được từ 2 khối trở lên kề nhau qua biên -> Xác nhận cụm Match hợp lệ!
                if (currentGroup.Count >= 2)
                {
                    matchGroups.Add(currentGroup);
                    visitedBlocks.UnionWith(currentGroup);
                }
            }
        }

        return matchGroups;
    }

    // Hàm kiểm tra xem hai khối ở 2 ô cạnh nhau có phần sub-slot nào chạm biên sát vào nhau không
    private static bool CheckBorderContact(Vector2Int coordA, JellyBlock blockA, Vector2Int coordB, JellyBlock blockB)
    {
        Vector2Int diff = coordB - coordA;

        foreach (var slotA in blockA.LocalSlots)
        {
            foreach (var slotB in blockB.LocalSlots)
            {
                // Nếu ô B nằm bên PHẢI ô A
                if (diff.x == 1 && diff.y == 0)
                {
                    if (slotA.x == 1 && slotB.x == 0 && slotA.y == slotB.y) return true;
                }
                // Nếu ô B nằm bên TRÁI ô A
                else if (diff.x == -1 && diff.y == 0)
                {
                    if (slotA.x == 0 && slotB.x == 1 && slotA.y == slotB.y) return true;
                }
                // Nếu ô B nằm bên TRÊN ô A
                else if (diff.x == 0 && diff.y == 1)
                {
                    if (slotA.y == 1 && slotB.y == 0 && slotA.x == slotB.x) return true;
                }
                // Nếu ô B nằm bên DƯỚI ô A
                else if (diff.x == 0 && diff.y == -1)
                {
                    if (slotA.y == 0 && slotB.y == 1 && slotA.x == slotB.x) return true;
                }
            }
        }
        return false;
    }
}
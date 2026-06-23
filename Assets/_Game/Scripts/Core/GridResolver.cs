using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridManager))] // Đảm bảo luôn đi kèm với GridManager
public class GridResolver : MonoBehaviour
{
    private GridManager gridManager;

    void Awake()
    {
        gridManager = GetComponent<GridManager>();
    }

    public void RunResolutionLoop()
    {
        StartCoroutine(ResolutionRoutine());
    }

    private IEnumerator ResolutionRoutine()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.PopSound);

        yield return new WaitForSeconds(gridManager.DropDelayBeforePop);

        int comboCount = 0;
        bool hasChanges = true;

        while (hasChanges && comboCount < 10)
        {
            hasChanges = false;

            // ===================================================================
            // 🔋 BƯỚC A: XỬ LÝ GỘP MÀU NỘI BỘ TRONG TỪNG Ô TRƯỚC (INTRA-CELL)
            // ===================================================================
            foreach (var cell in gridManager.ActiveCells)
            {
                if (MergeResolver.ResolveIntraCellMerge(cell))
                {
                    hasChanges = true;
                    gridManager.NormalizeCellLayout(cell, true);
                    yield return new WaitForSeconds(0.3f); // Đợi cú gộp nẩy nhẹ ổn định phom dáng
                }
            }

            // ===================================================================
            // 💥 BƯỚC B: XỬ LÝ QUÉT CỤM NỔ MATCH GIỮA CÁC Ô (INTER-CELL)
            // ===================================================================
            var matchGroups = MatchResolver.ResolveInterCellMatch(gridManager);
            if (matchGroups.Count > 0)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.MatchSound);

                comboCount++;
                matchGroups.Sort((a, b) => CalculateCascadePotential(b).CompareTo(CalculateCascadePotential(a)));
                var singleGroup = matchGroups[0];

                // 📦 Ghi nhận thông số màu nổ trước, tạm giữ lại chưa báo GameManager vội để chống Win sớm
                BlockColor groupColor = BlockColor.None;
                int dynamicProgress = 0;
                if (singleGroup.Count >= 2)
                {
                    foreach (var b in singleGroup) { groupColor = b.Color; break; }
                    dynamicProgress = singleGroup.Count - 1;
                }

                // 🌟 PHA 1: CHỈ CHO CÁC KHỐI TRONG CỤM MATCH PHÓNG NỔ BẮN HẠT ĐỒNG MÀU
                foreach (var explodedBlock in singleGroup)
                {
                    if (explodedBlock.VisualObjs != null)
                    {
                        foreach (var vObj in explodedBlock.VisualObjs)
                        {
                            if (vObj != null)
                            {
                                JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                                if (jiggle != null)
                                {
                                    vObj.transform.SetParent(null); // Tách khỏi cha để không bị kéo lệch tâm khi nổ
                                    jiggle.PlayMatchExplosionFX(() => {
                                        Destroy(vObj);
                                    });
                                }
                                else
                                {
                                    Destroy(vObj);
                                }
                            }
                        }
                    }
                }

                // ⏳ ĐỢI CỤM MATCH CO NHỎ NỔ BẮN HẠT XONG XUÔI (Mất 0.27 giây)
                yield return new WaitForSeconds(0.27f);

                // 🔥 THỜI ĐIỂM VÀNG: Viên thạch Goal cuối cùng biến mất sạch sẽ, giờ mới bung bảng WIN!
                if (GameManager.Instance != null && groupColor != BlockColor.None)
                {
                    GameManager.Instance.TrackPoppedBlocks(groupColor, dynamicProgress);
                }

                // 🌟 PHA 2: SAU KHI NỔ XONG, MỚI UPDATE DATA LOGIC VÀ RA LỆNH CHO KHỐI CÒN LẠI DẠT HÀNG
                HashSet<GridCell> affectedCells = new HashSet<GridCell>();

                foreach (var explodedBlock in singleGroup)
                {
                    foreach (var cell in gridManager.ActiveCells)
                    {
                        if (cell.Blocks.Contains(explodedBlock))
                        {
                            cell.Blocks.Remove(explodedBlock);
                            affectedCells.Add(cell);
                            break;
                        }
                    }
                }

                // Các khối màu còn lại đồng loạt Elastic nảy tràn ra lấp ô trống
                foreach (var cell in affectedCells)
                {
                    gridManager.NormalizeCellLayout(cell, true);
                }

                hasChanges = true;
                yield return new WaitForSeconds(0.45f); // Đợi ổn định chỗ ngồi mới rồi vòng lặp quay lại check Combo tiếp theo
            }
        }

        if (gridManager.Cells == null || gridManager.Cells.Count == 0) yield break;

        bool isGridFull = true;
        foreach (var cell in gridManager.ActiveCells)
        {
            if (cell.Blocks.Count == 0)
            {
                isGridFull = false;
                break;
            }
        }

        if (isGridFull)
        {
            Debug.Log("<color=red>[GridResolver]: LƯỚI ĐÃ BỊ LẤP ĐẦY! BUNG LOSE PANEL TRỰC TIẾP.</color>");
            GameUIManager uiManager = Object.FindFirstObjectByType<GameUIManager>();
            if (uiManager != null) uiManager.TriggerLoseState();
        }
    }

    private int CalculateCascadePotential(HashSet<JellyBlock> group)
    {
        int totalScore = 0;
        HashSet<GridCell> affectedCells = new HashSet<GridCell>();
        
        foreach (var block in group)
        {
            foreach (var cell in gridManager.ActiveCells)
            {
                if (cell.Blocks.Contains(block)) { affectedCells.Add(cell); break; }
            }
        }

        foreach (var cell in affectedCells)
        {
            List<JellyBlock> remainingBlocks = new List<JellyBlock>();
            foreach (var b in cell.Blocks) if (!group.Contains(b)) remainingBlocks.Add(b);

            if (remainingBlocks.Count == 1)
            {
                JellyBlock survivor = remainingBlocks[0];
                Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

                foreach (var dir in dirs)
                {
                    Vector2Int neighborCoord = cell.Coord + dir;
                    if (gridManager.TryGetCell(neighborCoord, out GridCell neighborCell))
                    {
                        foreach (var neighborBlock in neighborCell.Blocks)
                        {
                            if (neighborBlock.Color == survivor.Color && !group.Contains(neighborBlock))
                            {
                                if (WillTouchAfterExpansion(dir, neighborBlock))
                                {
                                    if (!CheckBorderContactInternal(cell.Coord, survivor, neighborCell.Coord, neighborBlock)) totalScore += 100;
                                }
                            }
                        }
                    }
                }
            }
        }
        return totalScore;
    }

    private bool WillTouchAfterExpansion(Vector2Int dir, JellyBlock neighborBlock)
    {
        foreach (var slot in neighborBlock.LocalSlots)
        {
            if (dir.x == 1 && dir.y == 0) { if (slot.x == 0) return true; }
            else if (dir.x == -1 && dir.y == 0) { if (slot.x == 1) return true; }
            else if (dir.x == 0 && dir.y == 1) { if (slot.y == 0) return true; }
            else if (dir.x == 0 && dir.y == -1) { if (slot.y == 1) return true; }
        }
        return false;
    }

    private bool CheckBorderContactInternal(Vector2Int coordA, JellyBlock blockA, Vector2Int coordB, JellyBlock blockB)
    {
        Vector2Int diff = coordB - coordA;
        foreach (var slotA in blockA.LocalSlots)
        {
            foreach (var slotB in blockB.LocalSlots)
            {
                if (diff.x == 1 && diff.y == 0) { if (slotA.x == 1 && slotB.x == 0 && slotA.y == slotB.y) return true; }
                else if (diff.x == -1 && diff.y == 0) { if (slotA.x == 0 && slotB.x == 1 && slotA.y == slotB.y) return true; }
                else if (diff.x == 0 && diff.y == 1) { if (slotA.y == 1 && slotB.y == 0 && slotA.x == slotB.x) return true; }
                else if (diff.x == 0 && diff.y == -1) { if (slotA.y == 0 && slotB.y == 1 && slotA.x == slotB.x) return true; }
            }
        }
        return false;
    }
}
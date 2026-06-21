using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    private Dictionary<Vector2Int, GridCell> _cells = new Dictionary<Vector2Int, GridCell>();

    [Header("Grid Config")]
    public float cellSize = 1.4f;
    public GameObject gridCellPrefab;
    public GameObject jellyBlockPrefab;

    [Tooltip("Thời gian chờ khối thạch nảy ổn định sau khi đặt xuống lưới rồi mới nổ")]
    public float dropDelayBeforePop = 0.4f;

    [Header("Tùy Chỉnh Khe Hở Trên Lưới")]
    [Range(0f, 0.2f)]
    public float cellGapPercent = 0.0f;

    [Header("Level Asset")]
    public LevelData currentLevelData;

    public IEnumerable<GridCell> ActiveCells => _cells.Values;

    void OnEnable() { GameManager.OnStateChanged += HandleStateChanged; }
    void OnDisable() { GameManager.OnStateChanged -= HandleStateChanged; }

    void Start()
    {
        InitializeLevel();
        if (GameManager.Instance != null) GameManager.Instance.StartGame();
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.Playing) InitializeLevel();
    }

    private void InitializeLevel()
    {
        if (currentLevelData != null) SetupLevelGrid(currentLevelData);
        else SetupDefaultRectangleGrid(5, 4);

        // ĐÃ CẬP NHẬT: Tìm kiếm bao gồm cả Object đang ẩn (FindObjectsInactive.Include) để bật lại khay Dock
        DockManager dock = Object.FindFirstObjectByType<DockManager>(FindObjectsInactive.Include);
        if (dock != null)
        {
            dock.gameObject.SetActive(true); // Bật lại DockManager khi bắt đầu chơi màn mới!

            IClusterGenerator levelGen = currentLevelData.GenMode == DockGenMode.BagGridAware
                ? new BagGridAwareGenerator(currentLevelData.PredefinedClusters)
                : new FixedQueueGenerator(currentLevelData.PredefinedClusters);

            dock.InitDock(this, levelGen, currentLevelData.DockSlotCount);
        }
    }

    public void RunResolutionLoop()
    {
        StartCoroutine(ResolutionRoutine());
    }

    private IEnumerator ResolutionRoutine()
    {
        yield return new WaitForSeconds(dropDelayBeforePop);

        int comboCount = 0;
        bool hasChanges = true;

        while (hasChanges && comboCount < 10)
        {
            hasChanges = false;

            foreach (var cell in ActiveCells)
            {
                if (MergeResolver.ResolveIntraCellMerge(cell))
                {
                    hasChanges = true;
                    NormalizeCellLayout(cell, true);
                }
            }

            var matchGroups = MatchResolver.ResolveInterCellMatch(this);
            if (matchGroups.Count > 0)
            {
                // ===================================================================
                // 🔥 ĐÃ SỬA: Đổi từ popSound sang matchSound để khi nổ khối màu mới kêu MATCH!
                // ===================================================================
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.matchSound);
                // ===================================================================

                comboCount++;

                matchGroups.Sort((a, b) => CalculateCascadePotential(b).CompareTo(CalculateCascadePotential(a)));

                var singleGroup = matchGroups[0];

                if (GameManager.Instance != null && singleGroup.Count >= 2)
                {
                    BlockColor groupColor = BlockColor.None;
                    foreach (var b in singleGroup) { groupColor = b.Color; break; }

                    if (groupColor != BlockColor.None)
                    {
                        int dynamicProgress = singleGroup.Count - 1;
                        GameManager.Instance.TrackPoppedBlocks(groupColor, dynamicProgress);
                    }
                }

                foreach (var explodedBlock in singleGroup)
                {
                    foreach (var cell in ActiveCells)
                    {
                        if (cell.Blocks.Contains(explodedBlock))
                        {
                            if (explodedBlock.VisualObjs != null)
                            {
                                foreach (var vObj in explodedBlock.VisualObjs)
                                {
                                    if (vObj != null) Destroy(vObj);
                                }
                            }
                            cell.Blocks.Remove(explodedBlock);

                            NormalizeCellLayout(cell, true);
                            break;
                        }
                    }
                }
                hasChanges = true;

                yield return new WaitForSeconds(0.55f);
            }
        }
    }

    private int CalculateCascadePotential(HashSet<JellyBlock> group)
    {
        int totalScore = 0;

        HashSet<GridCell> affectedCells = new HashSet<GridCell>();
        foreach (var block in group)
        {
            foreach (var cell in ActiveCells)
            {
                if (cell.Blocks.Contains(block))
                {
                    affectedCells.Add(cell);
                    break;
                }
            }
        }

        foreach (var cell in affectedCells)
        {
            List<JellyBlock> remainingBlocks = new List<JellyBlock>();
            foreach (var b in cell.Blocks)
            {
                if (!group.Contains(b)) remainingBlocks.Add(b);
            }

            if (remainingBlocks.Count == 1)
            {
                JellyBlock survivor = remainingBlocks[0];
                Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

                foreach (var dir in dirs)
                {
                    Vector2Int neighborCoord = cell.Coord + dir;
                    if (TryGetCell(neighborCoord, out GridCell neighborCell))
                    {
                        foreach (var neighborBlock in neighborCell.Blocks)
                        {
                            if (neighborBlock.Color == survivor.Color && !group.Contains(neighborBlock))
                            {
                                if (WillTouchAfterExpansion(dir, neighborBlock))
                                {
                                    if (!CheckBorderContactInternal(cell.Coord, survivor, neighborCell.Coord, neighborBlock))
                                    {
                                        totalScore += 100;
                                    }
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

    private IEnumerator AnimateResize(Transform targetTransform, Vector3 targetLocalPos, Vector3 targetLocalScale, float duration)
    {
        if (targetTransform == null) yield break;

        Vector3 startPos = targetTransform.localPosition;
        Vector3 startScale = targetTransform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
            float bounceEffect = Mathf.Sin(t * Mathf.PI * 2.5f) * 0.12f * (1f - t);
            float finalT = smoothT + bounceEffect;

            if (targetTransform == null) yield break;

            targetTransform.localPosition = Vector3.LerpUnclamped(startPos, targetLocalPos, finalT);
            targetTransform.localScale = Vector3.LerpUnclamped(startScale, targetLocalScale, finalT);

            yield return null;
        }

        if (targetTransform != null)
        {
            targetTransform.localPosition = targetLocalPos;
            targetTransform.localScale = targetLocalScale;
        }
    }

    public void NormalizeCellLayout(GridCell cell, bool animate = false)
    {
        if (cell == null || cell.Blocks == null || cell.Blocks.Count == 0) return;

        float size = cellSize;
        float maxSubBlockSize = size / 2f;
        float gap = size * cellGapPercent;

        int count = cell.Blocks.Count;

        if (count == 1)
        {
            var b = cell.Blocks[0];
            b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) };

            Vector3 targetPos = new Vector3(0f, 0f, 0f);
            Vector3 targetScale = new Vector3(size - gap, 1f, size - gap);

            foreach (var vObj in b.VisualObjs)
            {
                if (vObj == null) continue;
                if (animate) StartCoroutine(AnimateResize(vObj.transform, targetPos, targetScale, 0.35f));
                else
                {
                    vObj.transform.localPosition = targetPos;
                    vObj.transform.localScale = targetScale;
                }
            }
        }
        else if (count == 2)
        {
            var b0 = cell.Blocks[0];
            var b1 = cell.Blocks[1];

            JellyBlockView v0 = b0.VisualObjs[0].GetComponent<JellyBlockView>();
            JellyBlockView v1 = b1.VisualObjs[0].GetComponent<JellyBlockView>();

            bool isVerticalStack = (v0.localSlot.x == v1.localSlot.x);

            if (isVerticalStack)
            {
                for (int i = 0; i < 2; i++)
                {
                    var b = cell.Blocks[i];
                    int originY = b.VisualObjs[0].GetComponent<JellyBlockView>().localSlot.y;
                    float offsetZ = (originY == 0) ? -size / 4f : size / 4f;
                    int ySlot = (originY == 0) ? 0 : 1;

                    b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(0, ySlot), new Vector2Int(1, ySlot) };

                    Vector3 targetPos = new Vector3(0f, 0f, offsetZ);
                    Vector3 targetScale = new Vector3(size - gap, 1f, maxSubBlockSize - gap);

                    foreach (var vObj in b.VisualObjs)
                    {
                        if (vObj == null) continue;
                        if (animate) StartCoroutine(AnimateResize(vObj.transform, targetPos, targetScale, 0.35f));
                        else
                        {
                            vObj.transform.localPosition = targetPos;
                            vObj.transform.localScale = targetScale;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    var b = cell.Blocks[i];
                    int originX = b.VisualObjs[0].GetComponent<JellyBlockView>().localSlot.x;
                    float offsetX = (originX == 0) ? -size / 4f : size / 4f;
                    int xSlot = (originX == 0) ? 0 : 1;

                    b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(xSlot, 0), new Vector2Int(xSlot, 1) };

                    Vector3 targetPos = new Vector3(offsetX, 0f, 0f);
                    Vector3 targetScale = new Vector3(maxSubBlockSize - gap, 1f, size - gap);

                    foreach (var vObj in b.VisualObjs)
                    {
                        if (vObj == null) continue;
                        if (animate) StartCoroutine(AnimateResize(vObj.transform, targetPos, targetScale, 0.35f));
                        else
                        {
                            vObj.transform.localPosition = targetPos;
                            vObj.transform.localScale = targetScale;
                        }
                    }
                }
            }
        }
        else if (count == 3)
        {
            bool[,] occupied = new bool[2, 2];
            foreach (var b in cell.Blocks)
            {
                JellyBlockView v = b.VisualObjs[0].GetComponent<JellyBlockView>();
                occupied[Mathf.Clamp(v.localSlot.x, 0, 1), Mathf.Clamp(v.localSlot.y, 0, 1)] = true;
            }

            int emptyX = 0, emptyY = 0;
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    if (!occupied[x, y]) { emptyX = x; emptyY = y; }

            bool chooseHorizontalExpand = (emptyX == emptyY);

            foreach (var b in cell.Blocks)
            {
                JellyBlockView v = b.VisualObjs[0].GetComponent<JellyBlockView>();
                int slotX = v.localSlot.x;
                int slotY = v.localSlot.y;

                float offsetX = (slotX == 0) ? -size / 4f : size / 4f;
                float offsetZ = (slotY == 0) ? -size / 4f : size / 4f;
                float scaleX = maxSubBlockSize - gap;
                float scaleZ = maxSubBlockSize - gap;

                b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(slotX, slotY) };

                if (chooseHorizontalExpand)
                {
                    if (slotX == 1 - emptyX && slotY == emptyY)
                    {
                        offsetX = 0f;
                        scaleX = size - gap;
                        b.LocalSlots.Add(new Vector2Int(emptyX, emptyY));
                    }
                }
                else
                {
                    if (slotX == emptyX && slotY == 1 - emptyY)
                    {
                        offsetZ = 0f;
                        scaleZ = size - gap;
                        b.LocalSlots.Add(new Vector2Int(emptyX, emptyY));
                    }
                }

                Vector3 targetPos = new Vector3(offsetX, 0f, offsetZ);
                Vector3 targetScale = new Vector3(scaleX, 1f, scaleZ);

                foreach (var vObj in b.VisualObjs)
                {
                    if (vObj == null) continue;
                    if (animate) StartCoroutine(AnimateResize(vObj.transform, targetPos, targetScale, 0.35f));
                    else
                    {
                        vObj.transform.localPosition = targetPos;
                        vObj.transform.localScale = targetScale;
                    }
                }
            }
        }
        else if (count == 4)
        {
            foreach (var b in cell.Blocks)
            {
                JellyBlockView v = b.VisualObjs[0].GetComponent<JellyBlockView>();
                int slotX = Mathf.Clamp(v.localSlot.x, 0, 1);
                int slotY = Mathf.Clamp(v.localSlot.y, 0, 1);

                float offsetX = (slotX == 0) ? -size / 4f : size / 4f;
                float offsetZ = (slotY == 0) ? -size / 4f : size / 4f;

                b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(slotX, slotY) };

                Vector3 targetPos = new Vector3(offsetX, 0f, offsetZ);
                Vector3 targetScale = new Vector3(maxSubBlockSize - gap, 1f, maxSubBlockSize - gap);

                foreach (var vObj in b.VisualObjs)
                {
                    if (vObj == null) continue;
                    if (animate) StartCoroutine(AnimateResize(vObj.transform, targetPos, targetScale, 0.35f));
                    else
                    {
                        vObj.transform.localPosition = targetPos;
                        vObj.transform.localScale = targetScale;
                    }
                }
            }
        }
    }

    public void SetupLevelGrid(LevelData levelData)
    {
        _cells.Clear();
        foreach (Transform child in this.transform) Destroy(child.gameObject);

        foreach (Vector3 worldPos in levelData.ActiveCells)
        {
            int xKey = Mathf.RoundToInt(worldPos.x / cellSize);
            int yKey = Mathf.RoundToInt(worldPos.z / cellSize);
            Vector2Int coord = new Vector2Int(xKey, yKey);

            GridCell cell = new GridCell(coord);
            _cells[coord] = cell;

            if (gridCellPrefab != null)
            {
                Vector3 visualPos = new Vector3(worldPos.x, 0f, worldPos.z);
                GameObject cellObj = Instantiate(gridCellPrefab, visualPos, UnityEngine.Quaternion.Euler(90f, 0f, 0f), this.transform);
                cellObj.name = $"GridCell_{coord.x}_{coord.y}";
                cellObj.transform.localScale = new Vector3(cellSize * 0.96f, cellSize * 0.96f, 1f);
            }
        }

        if (levelData.PrePlacedClusters != null && jellyBlockPrefab != null)
        {
            foreach (var clusterEntry in levelData.PrePlacedClusters)
            {
                int computedX = Mathf.RoundToInt(clusterEntry.GridWorldPos.x / cellSize);
                int computedY = Mathf.RoundToInt(clusterEntry.GridWorldPos.z / cellSize);
                Vector2Int computedCoord = new Vector2Int(computedX, computedY);

                if (TryGetCell(computedCoord, out GridCell targetCell))
                {
                    GameObject clusterContainer = new GameObject($"PrePlaced_Cluster_Holder_{computedCoord.x}_{computedCoord.y}");
                    clusterContainer.transform.SetParent(this.transform);
                    clusterContainer.transform.position = new Vector3(clusterEntry.GridWorldPos.x, 0.5f, clusterEntry.GridWorldPos.z);
                    clusterContainer.transform.rotation = UnityEngine.Quaternion.identity;

                    foreach (var subBlock in clusterEntry.Blocks)
                    {
                        if (subBlock.Color == BlockColor.None) continue;

                        Vector3 spawnPos = new Vector3(clusterContainer.transform.position.x, 0.5f, clusterContainer.transform.position.z);
                        GameObject blockObj = Instantiate(jellyBlockPrefab, spawnPos, UnityEngine.Quaternion.identity, clusterContainer.transform);
                        blockObj.name = $"PrePlaced_Block_{subBlock.Color}_{subBlock.LocalSlot.x}_{subBlock.LocalSlot.y}";

                        JellyBlockView viewScript = blockObj.GetComponent<JellyBlockView>();
                        if (viewScript != null)
                        {
                            viewScript.localSlot = subBlock.LocalSlot;
                            viewScript.SetColorView(subBlock.Color);
                        }

                        JellyBlock newLogicBlock = new JellyBlock(Random.Range(10000, 99999), subBlock.Color, new List<Vector2Int> { subBlock.LocalSlot });
                        newLogicBlock.VisualObjs.Add(blockObj);

                        targetCell.Blocks.Add(newLogicBlock);
                    }
                }
            }
        }

        foreach (var cell in ActiveCells)
        {
            MergeResolver.ResolveIntraCellMerge(cell);
            NormalizeCellLayout(cell, false);
        }

        Debug.Log($"[GridManager]: Nạp thành công cấu trúc màn chơi LEVEL {levelData.LevelIndex}.");
    }

    public void SetupDefaultRectangleGrid(int width, int height)
    {
        _cells.Clear();
        Vector3 originOffset = new Vector3((width - 1) * cellSize / 2f, 0, (height - 1) * cellSize / 2f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                GridCell cell = new GridCell(coord);
                _cells[coord] = cell;

                Vector3 worldPos = new Vector3(x * cellSize, 0, y * cellSize) - originOffset;

                if (gridCellPrefab != null)
                {
                    GameObject cellObj = Instantiate(gridCellPrefab, worldPos, Quaternion.Euler(90f, 0f, 0f), this.transform);
                    cellObj.name = $"GridCell_{x}_{y}";
                    cellObj.transform.localScale = new Vector3(cellSize * 0.96f, cellSize * 0.96f, 1f);
                }
            }
        }
    }


    public void ClearActiveBoard()
    {
        _cells.Clear();

        JellyBlockView[] allViews = Object.FindObjectsByType<JellyBlockView>(FindObjectsSortMode.None);
        foreach (var view in allViews)
        {
            if (view != null) Destroy(view.gameObject);
        }

        foreach (Transform child in this.transform)
        {
            if (child != null) Destroy(child.gameObject);
        }

        // ĐÃ CẬP NHẬT THÔNG MINH: Ẩn hẳn cụm DockManager để giấu hoàn toàn 2 ô xám nền static 3D đi!
        DockManager dock = Object.FindFirstObjectByType<DockManager>();
        if (dock != null)
        {
            dock.gameObject.SetActive(false);
        }

        Debug.Log("[GridManager]: Đã ẩn khay dock và dọn sạch bong kin kít toàn bộ trận địa.");
    }

    public bool TryGetCell(Vector2Int coord, out GridCell cell)
    {
        return _cells.TryGetValue(coord, out cell);
    }
}

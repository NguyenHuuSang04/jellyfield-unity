using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(GridResolver))] // Tự động kéo GridResolver vào cùng
public class GridManager : MonoBehaviour
{
    public Dictionary<Vector2Int, GridCell> Cells { get; private set; } = new Dictionary<Vector2Int, GridCell>();

    [Header("Grid Config")]
    [SerializeField] private float cellSize = 1.4f;
    [SerializeField] private GameObject gridCellPrefab;
    [SerializeField] private GameObject jellyBlockPrefab;

    [SerializeField] private float dropDelayBeforePop = 0.4f;

    [Range(0f, 0.2f)]
    [SerializeField] private float cellGapPercent = 0.0f;

    [Header("Level Asset")]
    [SerializeField] private LevelData currentLevelData;

    [Header("🎯 Toàn Bộ Các Màn Chơi Hệ Thống")]
    [SerializeField] private List<LevelData> allLevels = new List<LevelData>();

    public float CellSize => cellSize;
    public GameObject GridCellPrefab => gridCellPrefab;
    public GameObject JellyBlockPrefab => jellyBlockPrefab;
    public float DropDelayBeforePop => dropDelayBeforePop;
    public float CellGapPercent => cellGapPercent;
    public LevelData CurrentLevelData
    {
        get => currentLevelData;
        set => currentLevelData = value;
    }
    public List<LevelData> AllLevels => allLevels;

    public IEnumerable<GridCell> ActiveCells => Cells.Values;
    private GridResolver gridResolver;

    void Awake()
    {
        gridResolver = GetComponent<GridResolver>();
    }

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
        if (MainMenuManager.ChosenLevelData != null)
        {
            currentLevelData = MainMenuManager.ChosenLevelData;
        }

        if (currentLevelData != null) SetupLevelGrid(currentLevelData);
        else SetupDefaultRectangleGrid(5, 4);

        // Đánh thức các ô Static_Slot_BG đang lẩn khuất (dành cho các ô được thiết kế tĩnh)
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go != null && go.name.StartsWith("Static_Slot_BG"))
            {
                go.SetActive(true);
            }
        }

        DockManager dock = Object.FindFirstObjectByType<DockManager>(FindObjectsInactive.Include);
        if (dock != null)
        {
            dock.gameObject.SetActive(true);

            //  Ép tất cả các màn chơi chạy bộ sinh tuần tự cố định từ LevelData
            IClusterGenerator levelGen = new FixedQueueGenerator(currentLevelData.PredefinedClusters);

            dock.InitDock(this, levelGen, currentLevelData.DockSlotCount);
        }
    }

    public void RunResolutionLoop()
    {
        if (gridResolver != null) gridResolver.RunResolutionLoop();
    }

    private void AnimateResize(Transform targetTransform, Vector3 targetLocalPos, Vector3 targetLocalScale, float duration)
    {
        if (targetTransform == null) return;

        // Dọn dẹp luồng tween cũ tránh xung đột kích thước
        targetTransform.DOKill();

        // 1. Di chuyển tâm vị trí mượt mà vào vùng ô lưới mới
        targetTransform.DOLocalMove(targetLocalPos, duration).SetEase(Ease.OutQuad);

        // 2. Ép khối thạch bùng nổ phình to tràn ra lòng ô trống như chất lỏng bằng Ease.OutElastic
        targetTransform.DOScale(targetLocalScale, duration * 1.5f).SetEase(Ease.OutElastic);
    }

    public void NormalizeCellLayout(GridCell cell, bool animate = false)
    {
        if (cell == null || cell.Blocks == null || cell.Blocks.Count == 0) return;

        if (animate)
        {
            foreach (var b in cell.Blocks)
            {
                foreach (var vObj in b.VisualObjs)
                {
                    if (vObj != null)
                    {
                        JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                        if (jiggle != null) jiggle.PlayMergeJiggle();
                    }
                }
            }
        }

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

                JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
            }
        }
        else if (count == 2)
        {
            var b0 = cell.Blocks[0]; var b1 = cell.Blocks[1];
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

                        JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                        if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                        if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                        else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
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

                        JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                        if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                        if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                        else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
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
                int slotX = v.localSlot.x; int slotY = v.localSlot.y;
                float offsetX = (slotX == 0) ? -size / 4f : size / 4f;
                float offsetZ = (slotY == 0) ? -size / 4f : size / 4f;
                float scaleX = maxSubBlockSize - gap; float scaleZ = maxSubBlockSize - gap;
                b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(slotX, slotY) };

                if (chooseHorizontalExpand)
                {
                    if (slotX == 1 - emptyX && slotY == emptyY)
                    {
                        offsetX = 0f; scaleX = size - gap; b.LocalSlots.Add(new Vector2Int(emptyX, emptyY));
                    }
                }
                else
                {
                    if (slotX == emptyX && slotY == 1 - emptyY)
                    {
                        offsetZ = 0f; scaleZ = size - gap; b.LocalSlots.Add(new Vector2Int(emptyX, emptyY));
                    }
                }

                Vector3 targetPos = new Vector3(offsetX, 0f, offsetZ);
                Vector3 targetScale = new Vector3(scaleX, 1f, scaleZ);

                foreach (var vObj in b.VisualObjs)
                {
                    if (vObj == null) continue;

                    JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                    if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                    if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                    else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
                }
            }
        }
        else if (count == 4)
        {
            foreach (var b in cell.Blocks)
            {
                JellyBlockView v = b.VisualObjs[0].GetComponent<JellyBlockView>();
                int slotX = Mathf.Clamp(v.localSlot.x, 0, 1); int slotY = Mathf.Clamp(v.localSlot.y, 0, 1);
                float offsetX = (slotX == 0) ? -size / 4f : size / 4f;
                float offsetZ = (slotY == 0) ? -size / 4f : size / 4f;
                b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(slotX, slotY) };
                Vector3 targetPos = new Vector3(offsetX, 0f, offsetZ);
                Vector3 targetScale = new Vector3(maxSubBlockSize - gap, 1f, maxSubBlockSize - gap);

                foreach (var vObj in b.VisualObjs)
                {
                    if (vObj == null) continue;

                    JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                    if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                    if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                    else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
                }
            }
        }
    }
    
    public void SetupLevelGrid(LevelData levelData)
    {
        Cells.Clear();
        foreach (Transform child in this.transform) Destroy(child.gameObject);

        foreach (Vector3 worldPos in levelData.ActiveCells)
        {
            int xKey = Mathf.RoundToInt(worldPos.x / cellSize);
            int yKey = Mathf.RoundToInt(worldPos.z / cellSize);
            Vector2Int coord = new Vector2Int(xKey, yKey);

            GridCell cell = new GridCell(coord);
            Cells[coord] = cell;

            if (gridCellPrefab != null)
            {
                Vector3 visualPos = new Vector3(worldPos.x, 0f, worldPos.z);
                GameObject cellObj = Instantiate(gridCellPrefab, visualPos, UnityEngine.Quaternion.Euler(90f, 0f, 0f), this.transform);
                cellObj.name = $"GridCell_{coord.x}_{coord.y}";
                cellObj.transform.localScale = new Vector3(cellSize * 0.96f, cellSize * 0.96f, 1f);

                GridCellTag cellTag = cellObj.GetComponent<GridCellTag>();
                if (cellTag == null) cellTag = cellObj.AddComponent<GridCellTag>();
                cellTag.Coord = coord;
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
        Cells.Clear();
        Vector3 originOffset = new Vector3((width - 1) * cellSize / 2f, 0, (height - 1) * cellSize / 2f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                GridCell cell = new GridCell(coord);
                Cells[coord] = cell;

                Vector3 worldPos = new Vector3(x * cellSize, 0, y * cellSize) - originOffset;

                if (gridCellPrefab != null)
                {
                    GameObject cellObj = Instantiate(gridCellPrefab, worldPos, Quaternion.Euler(90f, 0f, 0f), this.transform);
                    cellObj.name = $"GridCell_{x}_{y}";
                    cellObj.transform.localScale = new Vector3(cellSize * 0.96f, cellSize * 0.96f, 1f);

                    GridCellTag cellTag = cellObj.GetComponent<GridCellTag>();
                    if (cellTag == null) cellTag = cellObj.AddComponent<GridCellTag>();
                    cellTag.Coord = coord;
                }
            }
        }
    }

    public void ClearActiveBoard()
    {
        Cells.Clear();

        JellyBlockView[] allViews = Object.FindObjectsByType<JellyBlockView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var view in allViews)
        {
            if (view != null) Destroy(view.gameObject);
        }

        foreach (Transform child in this.transform)
        {
            if (child != null) Destroy(child.gameObject);
        }

        DockManager dock = Object.FindFirstObjectByType<DockManager>(FindObjectsInactive.Include);
        if (dock != null)
        {
            dock.gameObject.SetActive(false);
        }

        GameObject[] allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGameObjects)
        {
            if (go != null)
            {
                if (go.name.StartsWith("Static_Slot_BG"))
                {
                    go.SetActive(false);
                }
                else if (go.name.StartsWith("Dock_Cluster") || go.name.Contains("DockSlotBG"))
                {
                    Destroy(go);
                }
            }
        }

        Debug.Log("[GridManager]: Trận địa cũ đã được dọn dẹp sạch sẽ và giấu khay dock an toàn.");
    }

    public bool TryGetCell(Vector2Int coord, out GridCell cell)
    {
        return Cells.TryGetValue(coord, out cell);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using JellyField.Managers;
using JellyField.View;
using JellyField.Level;
using JellyField.Logic;

namespace JellyField.Core
{
    [RequireComponent(typeof(GridResolver))] // Tự động kéo GridResolver vào cùng
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

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
        private CellLayoutView cellLayout;

        // Decoupled Visual System mapping JellyBlock.Id -> List<GameObject> of visuals
        private Dictionary<int, List<GameObject>> blockVisuals = new Dictionary<int, List<GameObject>>();

        public void RegisterVisuals(int blockId, List<GameObject> visuals)
        {
            blockVisuals[blockId] = visuals;
        }

        public List<GameObject> GetVisuals(int blockId)
        {
            if (blockVisuals.TryGetValue(blockId, out var list))
                return list;
            return null;
        }

        public void UnregisterVisuals(int blockId)
        {
            blockVisuals.Remove(blockId);
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            gridResolver = GetComponent<GridResolver>();
            
            cellLayout = GetComponent<CellLayoutView>();
            if (cellLayout == null) cellLayout = gameObject.AddComponent<CellLayoutView>();
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

        public void NormalizeCellLayout(GridCell cell, bool animate = false)
        {
            if (cellLayout != null)
            {
                cellLayout.NormalizeCellLayout(cell, animate);
            }
        }
        
        public void SetupLevelGrid(LevelData levelData)
        {
            Cells.Clear();
            blockVisuals.Clear();
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
                                viewScript.LocalSlot = subBlock.LocalSlot;
                                viewScript.SetColorView(subBlock.Color);
                            }

                            JellyBlock newLogicBlock = new JellyBlock(Random.Range(10000, 99999), subBlock.Color, new List<Vector2Int> { subBlock.LocalSlot });
                            RegisterVisuals(newLogicBlock.Id, new List<GameObject> { blockObj });

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
            blockVisuals.Clear();
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
            blockVisuals.Clear();

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
}
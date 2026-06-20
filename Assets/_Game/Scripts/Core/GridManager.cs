using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    private Dictionary<Vector2Int, GridCell> _cells = new Dictionary<Vector2Int, GridCell>();

    [Header("Grid Config")]
    public float cellSize = 1.4f;
    public GameObject gridCellPrefab; // Kéo GridCell_Placeholder vào đây
    public GameObject jellyBlockPrefab; // KÉO JELLYBLOCK_PREFAB VÀO ĐÂY

    [Header("Level Asset")]
    public LevelData currentLevelData;

    public IEnumerable<GridCell> ActiveCells => _cells.Values;

    void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    void Start()
    {
        // MẶC ĐỊNH: Khi nhấn Play, tự động kích hoạt dựng màn chơi luôn không cần qua Menu
        InitializeLevel();

        // Ép GameManager kích hoạt trạng thái Playing để HUD UI (GameUIManager) cũng tự chạy theo
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void HandleStateChanged(GameState newState)
    {
        // Vẫn giữ hàm này để khi bạn bấm nút "Retry" (Nạp lại màn), bàn chơi biết đường tự reset
        if (newState == GameState.Playing)
        {
            InitializeLevel();
        }
    }

    private void InitializeLevel()
    {
        if (currentLevelData != null)
        {
            SetupLevelGrid(currentLevelData);
        }
        else
        {
            SetupDefaultRectangleGrid(5, 4);
        }

        // Khởi tạo nạp khay Dock dưới đáy màn hình
        DockManager dock = Object.FindFirstObjectByType<DockManager>();
        if (dock != null && currentLevelData != null)
        {
            IClusterGenerator levelGen;
            if (currentLevelData.GenMode == DockGenMode.BagGridAware)
            {
                levelGen = new BagGridAwareGenerator(currentLevelData.PredefinedClusters);
            }
            else
            {
                levelGen = new FixedQueueGenerator(currentLevelData.PredefinedClusters);
            }

            dock.InitDock(this, levelGen, currentLevelData.DockSlotCount);
        }
    }

    public void RunResolutionLoop()
    {
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
                }
            }

            var matchGroups = MatchResolver.ResolveInterCellMatch(this);
            if (matchGroups.Count > 0)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.popSound);
                comboCount++;
                Debug.Log($"💥 PHÁT HIỆN COMBO LẦN {comboCount}: Có {matchGroups.Count} cụm Match đủ điều kiện nổ.");

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddGoalProgress(1);
                }

                foreach (var group in matchGroups)
                {
                    foreach (var explodedBlock in group)
                    {
                        foreach (var cell in ActiveCells)
                        {
                            if (cell.Blocks.Contains(explodedBlock))
                            {
                                cell.Blocks.Remove(explodedBlock);
                                Debug.Log($"-> Đã nổ và xóa khối Jelly ID {explodedBlock.Id} màu {explodedBlock.Color} tại ô ({cell.Coord.x}, {cell.Coord.y})");
                                break;
                            }
                        }
                    }
                }
                hasChanges = true;
            }
        }

        Debug.Log($"[Kết thúc Vòng lặp giải quyết]: Tổng số chuỗi Combo nổ = {comboCount}");
    }

    public void SetupLevelGrid(LevelData levelData)
    {
        _cells.Clear();

        // Dọn dẹp sạch sẽ các ô cũ trước khi dựng bàn chơi thực tế
        foreach (Transform child in this.transform)
        {
            Destroy(child.gameObject);
        }

        // ĐỔI SANG VECTOR3: Duyệt trực tiếp qua mảng tọa độ thực tế từ file Asset
        foreach (Vector3 worldPos in levelData.ActiveCells)
        {
            // Tự động quy đổi vị trí float thành Index nguyên gần nhất để hệ thống tính Combo/Nổ không bị lỗi
            int xKey = Mathf.RoundToInt(worldPos.x / cellSize);

            // Kiểm tra linh hoạt: Nếu Sang di chuyển trên trục Y (2D), lấy Y. Nếu di chuyển trên trục Z (3D), lấy Z.
            int yKey = (Mathf.Abs(worldPos.y) > Mathf.Abs(worldPos.z))
                ? Mathf.RoundToInt(worldPos.y / cellSize)
                : Mathf.RoundToInt(worldPos.z / cellSize);

            Vector2Int logicalCoord = new Vector2Int(xKey, yKey);

            GridCell cell = new GridCell(logicalCoord);
            _cells[logicalCoord] = cell;

            if (gridCellPrefab != null)
            {
                // SINH RA CHUẨN ĐÉT: Đặt đúng tọa độ float (worldPos) mà Sang đã tinh chỉnh ngoài Editor
                GameObject cellObj = Instantiate(gridCellPrefab, worldPos, Quaternion.Euler(90f, 0f, 0f), this.transform);
                cellObj.name = $"GridCell_{logicalCoord.x}_{logicalCoord.y}";
            }
        }

        Debug.Log($"[GridManager]: Khởi tạo thành công lưới Level {levelData.LevelIndex} với {levelData.ActiveCells.Count} ô hoạt động tự do.");
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

                    // DÒNG MỚI: Xoay 90 độ trục X để đặt nằm bẹt xuống không gian nằm ngang XZ
                    GameObject cellObj = Instantiate(gridCellPrefab, worldPos, Quaternion.Euler(90f, 0f, 0f), this.transform);
                    cellObj.name = $"GridCell_{x}_{y}";
                }
            }
        }
    }

    public bool TryGetCell(Vector2Int coord, out GridCell cell)
    {
        return _cells.TryGetValue(coord, out cell);
    }
}
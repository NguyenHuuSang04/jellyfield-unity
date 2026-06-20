using System.Collections.Generic;
using UnityEngine;

public class DockManager : MonoBehaviour
{
    private GridManager gridManager;
    private IClusterGenerator generator;
    
    // ĐÃ ĐỔI THÀNH PRIVATE: Ẩn hoàn toàn khỏi Inspector để tránh nhầm lẫn
    private int slotCount = 2; 

    [Header("Dock Config")]
    public GameObject clusterVisualPrefab; // Prefab hiển thị cụm khối dưới khay

    // Danh sách lưu các cụm khối 3D thực tế đang nằm dưới khay Dock
    private List<GameObject> activeSpawnedClusters = new List<GameObject>();

    public void InitDock(GridManager grid, IClusterGenerator customGenerator, int slots)
    {
        this.gridManager = grid;
        this.generator = customGenerator;
        this.slotCount = slots;

        RefreshDockSlots();
    }

    public void RefreshDockSlots()
    {
        foreach (var obj in activeSpawnedClusters)
        {
            if (obj != null) Destroy(obj);
        }
        activeSpawnedClusters.Clear();

        if (generator == null || gridManager == null) return;

        for (int i = 0; i < slotCount; i++)
        {
            if (generator.TryGetNext(gridManager, out ClusterData nextCluster))
            {
                // TÍNH TOÁN VỊ TRÍ PHÙ HỢP VỚI GÓC CAMERA:
                float posX = gridManager.transform.position.x + (i - (slotCount - 1) / 2f) * 2.5f;

                // CHUẨN KHÍT TRỤC X_Z: Đặt Z = -1.5f để đẩy khay chứa lên sát vùng nhìn thấy của Camera Orthographic
                Vector3 spawnPos = new Vector3(posX, 0.5f, -1.5f);

                if (clusterVisualPrefab != null)
                {
                    GameObject clusterObj = Instantiate(clusterVisualPrefab, spawnPos, Quaternion.identity, this.transform);
                    clusterObj.name = $"Dock_Cluster_{i}";

                    // ================== ĐOẠN BỔ SUNG KẾT NỐI: ==================
                    // Lấy script điều khiển ClusterVisual để kích hoạt sinh thạch con theo đúng dữ liệu màu sắc
                    ClusterVisual visualScript = clusterObj.GetComponent<ClusterVisual>();
                    if (visualScript != null)
                    {
                        visualScript.BuildCluster(nextCluster);
                    }
                    // ===========================================================

                    Debug.Log($"[Dock Slot {i}]: Đã nạp thành công cụm gồm {nextCluster.Blocks.Count} khối thạch logic.");
                    activeSpawnedClusters.Add(clusterObj);
                }
            }
        }
    }

    public void SetupDock(int targetSlots)
    {
        this.slotCount = targetSlots;
        RefreshDockSlots();
    }
}
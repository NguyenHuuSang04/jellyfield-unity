using System.Collections.Generic;
using UnityEngine;

public class DockManager : MonoBehaviour
{
    private GridManager gridManager;
    private IClusterGenerator generator;
    private int slotCount = 2;

    [Header("Dock Prefabs Config")]
    [SerializeField] private GameObject clusterVisualPrefab;
    [SerializeField] private GameObject dockSlotBackgroundPrefab;

    [Header("📐 Bộ Căn Chỉnh Vị Trí Dock (Inspector)")]
    [SerializeField] private float dockZPosition = -1.5f;
    [SerializeField] private float dockYHeight = 0.5f;
    [SerializeField] private float slotSpacing = 2.5f;

    [Header("🎨 Tùy Chỉnh Ô Nền Tĩnh (37.jpg)")]
    [Tooltip("Độ lệch tầng Y của ô nền so với thạch. Nên để tầm -0.52f để đẩy ô nền nằm HẲN XUỐNG DƯỚI đáy khối thạch 3D.")]
    [SerializeField] private float slotBGYOffset = -0.52f;

    [Tooltip("Tỉ lệ phóng theo chiều ngang (Trái - Phải) để vừa khít phom thạch")]
    [SerializeField] private float slotBGWidthMultiplier = 1.15f;

    [Tooltip("Tỉ lệ phóng theo chiều dọc (Trên - Dưới) để vừa khít phom thạch")]
    [SerializeField] private float slotBGLengthMultiplier = 1.15f;

    private List<GameObject> activeSpawnedClusters = new List<GameObject>();
    private List<GameObject> spawnedSlotBackgrounds = new List<GameObject>();

    public void InitDock(GridManager grid, IClusterGenerator customGenerator, int slots)
    {
        this.gridManager = grid;
        this.generator = customGenerator;
        this.slotCount = slots;

        foreach (var obj in activeSpawnedClusters)
        {
            if (obj != null) Destroy(obj);
        }
        activeSpawnedClusters.Clear();
        for (int i = 0; i < slotCount; i++) activeSpawnedClusters.Add(null);

        GenerateStaticBackgroundSlots();
        RefreshDockSlots();
    }

    private void GenerateStaticBackgroundSlots()
    {
        if (gridManager == null || dockSlotBackgroundPrefab == null) return;

        foreach (var bg in spawnedSlotBackgrounds)
        {
            if (bg != null) Destroy(bg);
        }
        spawnedSlotBackgrounds.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            float posX = gridManager.transform.position.x + (i - (slotCount - 1) / 2f) * slotSpacing;

            // ĐÃ NÂNG CẤP: Ép độ cao Y đi theo slotBGYOffset tùy biến ngoài Inspector để chìm xuống đáy hoàn toàn
            Vector3 bgPos = new Vector3(posX, dockYHeight + slotBGYOffset, dockZPosition);

            GameObject bgObj = Instantiate(dockSlotBackgroundPrefab, bgPos, UnityEngine.Quaternion.Euler(90f, 0f, 0f), this.transform);
            bgObj.name = $"Static_Slot_BG_{i}";

            SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteRealWidth = sr.sprite.bounds.size.x;
                if (spriteRealWidth > 0)
                {
                    float baseScale = gridManager.CellSize / spriteRealWidth;

                    // ĐÃ NÂNG CẤP: Tách biệt hoàn toàn tỉ lệ phóng theo 2 chiều Ngang - Dọc theo ý Sang
                    float finalScaleX = baseScale * slotBGWidthMultiplier;
                    float finalScaleY = baseScale * slotBGLengthMultiplier;

                    bgObj.transform.localScale = new Vector3(finalScaleX, finalScaleY, 1f);
                }
            }

            spawnedSlotBackgrounds.Add(bgObj);
        }
    }

    public void RefreshDockSlots()
    {
        if (generator == null || gridManager == null) return;

        for (int i = 0; i < slotCount; i++)
        {
            if (activeSpawnedClusters[i] == null)
            {
                SpawnClusterInSlot(i);
            }
        }
    }

    private void SpawnClusterInSlot(int slotIndex)
    {
        if (generator.TryGetNext(gridManager, out ClusterData nextCluster))
        {
            float posX = gridManager.transform.position.x + (slotIndex - (slotCount - 1) / 2f) * slotSpacing;
            Vector3 spawnPos = new Vector3(posX, dockYHeight, dockZPosition);

            if (clusterVisualPrefab != null)
            {
                GameObject clusterObj = Instantiate(clusterVisualPrefab, spawnPos, UnityEngine.Quaternion.identity, this.transform);
                clusterObj.name = $"Dock_Cluster_{slotIndex}";

                ClusterVisual visualScript = clusterObj.GetComponent<ClusterVisual>();
                if (visualScript != null)
                {
                    visualScript.BuildCluster(nextCluster);
                }

                DraggableGroup dragScript = clusterObj.GetComponent<DraggableGroup>();
                if (dragScript != null)
                {
                    dragScript.SetSlotInfo(slotIndex, this);
                }

                activeSpawnedClusters[slotIndex] = clusterObj;
            }
        }
    }

    public void NotifyDragStarted(int slotIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(AudioManager.Instance.PopSound);
        }
        if (slotIndex >= 0 && slotIndex < activeSpawnedClusters.Count)
        {
            activeSpawnedClusters[slotIndex] = null;
        }
    }

    public void NotifyDragSuccess(int slotIndex)
    {
        SpawnClusterInSlot(slotIndex);
    }

    public void NotifyDragFailed(int slotIndex, GameObject clusterObj)
    {
        if (slotIndex >= 0 && slotIndex < activeSpawnedClusters.Count)
        {
            activeSpawnedClusters[slotIndex] = clusterObj;
        }
    }

    public void SetupDock(int targetSlots)
    {
        this.slotCount = targetSlots;
        InitDock(gridManager, generator, targetSlots);
    }
}
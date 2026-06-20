using System.Collections.Generic;
using UnityEngine;

public enum DockGenMode
{
    FixedQueue,      // Hàng đợi cố định (Dùng cho L1, L2)
    BagGridAware     // Túi bài thông minh (Dùng cho L3)
}

[System.Serializable]
public struct GoalEntry
{
    public BlockColor Color;
    public int Count;
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "JellyField/LevelData")]
public class LevelData : ScriptableObject
{
    public int LevelIndex;
    
    [Header("Grid Shape")]
    // Danh sách các tọa độ (X, Y) logic thực sự tồn tại trên lưới của level này
    public List<Vector3> ActiveCells; // Đổi sang Vector3 để lưu đúng vị trí thực tế X, Y, Z ngoài Scene

    [Header("Goals")]
    public List<GoalEntry> Goals = new List<GoalEntry>();

    [Header("Dock Settings")]
    public int DockSlotCount = 2;
    public DockGenMode GenMode = DockGenMode.FixedQueue;
    
    // Khay chứa các cụm khối xếp sẵn cho L1, L2 hoặc túi mẫu cho L3
    public List<ClusterData> PredefinedClusters = new List<ClusterData>();
}
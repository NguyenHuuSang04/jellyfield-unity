// using System.Collections.Generic;
// using UnityEngine;

// public enum DockGenMode
// {
//     FixedQueue,      // Hàng đợi cố định (Dùng cho L1, L2)
//     BagGridAware     // Túi bài thông minh (Dùng cho L3)
// }

// [System.Serializable]
// public struct GoalEntry
// {
//     public BlockColor Color;
//     public int Count;
// }

// [System.Serializable]
// public struct PrePlacedSubBlock
// {
//     [Tooltip("Vị trí ô góc vuông con bên trong ô lưới lớn (X: 0..1, Y: 0..1)")]
//     public Vector2Int LocalSlot;   
//     [Tooltip("Màu sắc của khối thạch con này")]
//     public BlockColor Color;       
// }

// // ===================================================================
// // ĐÃ NÂNG CẤP: Cho phép copy paste thẳng số Float từ Active Cells
// // ===================================================================
// [System.Serializable]
// public struct PrePlacedClusterEntry
// {
//     [Header("Copy số thực từ mục Active Cells dán vào đây")]
//     [Tooltip("Ví dụ: Nhập X = -2.86, Y = 0, Z = 1.52 cực kỳ tiện lợi.")]
//     public Vector3 GridWorldPos;   

//     [Tooltip("Danh sách các khối con bên trong ô này (Có thể tạo từ 1 đến 4 khối)")]
//     public List<PrePlacedSubBlock> Blocks;       
// }

// [CreateAssetMenu(fileName = "NewLevelData", menuName = "JellyField/LevelData")]
// public class LevelData : ScriptableObject
// {
//     public int LevelIndex;
    
//     [Header("Grid Shape")]
//     public List<Vector3> ActiveCells; 

//     [Header("Goals")]
//     public List<GoalEntry> Goals = new List<GoalEntry>();

//     [Header("Pre-Placed Clusters On Grid")]
//     public List<PrePlacedClusterEntry> PrePlacedClusters = new List<PrePlacedClusterEntry>();

//     [Header("Dock Settings")]
//     public int DockSlotCount = 2;
//     public DockGenMode GenMode = DockGenMode.FixedQueue;
    
//     public List<ClusterData> PredefinedClusters = new List<ClusterData>();
// }

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct GoalEntry
{
    public BlockColor Color;
    public int Count;
}

[System.Serializable]
public struct PrePlacedSubBlock
{
    [Tooltip("Vị trí ô góc vuông con bên trong ô lưới lớn (X: 0..1, Y: 0..1)")]
    public Vector2Int LocalSlot;   
    [Tooltip("Màu sắc của khối thạch con này")]
    public BlockColor Color;       
}

[System.Serializable]
public struct PrePlacedClusterEntry
{
    [Header("Copy số thực từ mục Active Cells dán vào đây")]
    [Tooltip("Ví dụ: Nhập X = -2.86, Y = 0, Z = 1.52 cực kỳ tiện lợi.")]
    public Vector3 GridWorldPos;   

    [Tooltip("Danh sách các khối con bên trong ô này (Có thể tạo từ 1 đến 4 khối)")]
    public List<PrePlacedSubBlock> Blocks;       
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "JellyField/LevelData")]
public class LevelData : ScriptableObject
{
    public int LevelIndex;
    
    [Header("Grid Shape")]
    public List<Vector3> ActiveCells; 

    [Header("Goals")]
    public List<GoalEntry> Goals = new List<GoalEntry>();

    [Header("Pre-Placed Clusters On Grid")]
    public List<PrePlacedClusterEntry> PrePlacedClusters = new List<PrePlacedClusterEntry>();

    [Header("Dock Settings")]
    public int DockSlotCount = 2;
    
    [Header("Danh sách khối màu xuất hiện tuần tự")]
    public List<ClusterData> PredefinedClusters = new List<ClusterData>(); // Cứ xếp khối gì trong này, game sẽ sinh ra y chang từ trái qua phải
}
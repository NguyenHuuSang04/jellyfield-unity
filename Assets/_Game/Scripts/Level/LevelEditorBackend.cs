#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JellyField.Core;

namespace JellyField.Level
{
    public class LevelEditorBackend : MonoBehaviour
    {
        [Header("Design Target")]
        [SerializeField] private LevelData targetLevelData; 

        [Header("Design Matrix Size")]
        [SerializeField] private int previewWidth = 5;
        [SerializeField] private int previewHeight = 5;

        // ĐỔI SANG VECTOR3: Để lưu trữ chính xác vị trí float thực tế ngoài Scene
        [HideInInspector] [SerializeField] private List<Vector3> editorActiveCells = new List<Vector3>();

        public void SpawnEditorWorkspace()
        {
            foreach (Transform child in transform)
            {
                if (child != null) DestroyImmediate(child.gameObject);
            }

            GridManager manager = GetComponent<GridManager>();
            float size = manager != null ? manager.CellSize : 1.4f;
            GameObject cellPrefab = manager != null ? manager.GridCellPrefab : null;

            if (cellPrefab == null) return;

            for (int x = 0; x < previewWidth; x++)
            {
                for (int y = 0; y < previewHeight; y++)
                {
                    Vector3 worldPos = new Vector3(x * size, 0, y * size);
                    GameObject cellObj = Instantiate(cellPrefab, worldPos, Quaternion.Euler(90f, 0f, 0f), this.transform);
                    cellObj.name = $"EditorCell_{x}_{y}";
                }
            }
        }

        // Quét sạch vị trí thực tế của các ô còn sống ngoài Scene (Nhận cả số float tự do)
        public void SaveCurrentLayoutToAsset()
        {
            if (targetLevelData == null)
            {
                Debug.LogError("⚠️ [Level Editor]: Hãy kéo file LevelData Asset vào ô trống trước khi Save!");
                return;
            }

            editorActiveCells.Clear();

            foreach (Transform child in transform)
            {
                if (child != null && child != transform)
                {
                    // LẤY ĐÚNG TỌA ĐỘ THỰC TẾ: Sang di chuyển ô đi đâu (X, Y, Z float), code bốc trọn vị trí đó
                    editorActiveCells.Add(child.position);
                }
            }

            // Nạp thẳng mảng Vector3 vào file dữ liệu Level
            targetLevelData.ActiveCells = new List<Vector3>(editorActiveCells);
            
            EditorUtility.SetDirty(targetLevelData);
            AssetDatabase.SaveAssets();

            Debug.Log($" [Level Editor]: Đã lưu thành công {editorActiveCells.Count} ô lưới với tọa độ thực tế vào file {targetLevelData.name}!");
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelEditorBackend))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ giao diện mặc định của Inspector
        DrawDefaultInspector();

        LevelEditorBackend script = (LevelEditorBackend)target;

        GUILayout.Space(15);
        GUILayout.Label("🛠️ KHU VỰC THIẾT KẾ MÀN CHƠI NHANH", EditorStyles.boldLabel);

        // Nút 1: Sinh vùng làm việc rỗng để thiết kế
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("1. Sinh Vùng Thiết Kế Rỗng (Workspace)", GUILayout.Height(35)))
        {
            script.SpawnEditorWorkspace();
        }

        GUILayout.Space(5);

        // Nút 2: Quét Scene và đóng gói lưu vào ScriptableObject
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("2. Đóng Gói & Lưu Lại (Save to Asset) 💾", GUILayout.Height(40)))
        {
            script.SaveCurrentLayoutToAsset();
        }
    }
}
#endif
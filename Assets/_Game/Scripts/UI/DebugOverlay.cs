using UnityEngine;
using JellyField.Managers;

namespace JellyField.UI
{
    public class DebugOverlay : MonoBehaviour
    {
        void OnGUI()
        {
            // Thiết lập một ô vùng chữ nhật góc trên cùng bên trái màn hình
            GUILayout.BeginArea(new Rect(20, 20, 300, 250));
            
            GUILayout.Label("=== JELLY FIELD DEBUG OVERLAY ===", GUILayout.Height(25));

            if (GameManager.Instance != null)
            {
                // Hiển thị trạng thái hiện tại của Game State
                GUILayout.Label($"Trạng thái game: {GameManager.Instance.CurrentState.ToString()}", GUILayout.Height(20));

                if (GameManager.Instance.CurrentState == GameState.MainMenu)
                {
                    if (GUILayout.Button("BẮT ĐẦU CHƠI (Play)", GUILayout.Height(35)))
                    {
                        GameManager.Instance.StartGame();
                    }
                }
                else if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    // Giả lập các nút bấm thay thế UI thật để kiểm thử tính năng
                    if (GUILayout.Button("GIẢ LẬP THẮNG (Cộng 1 điểm Goal)", GUILayout.Height(35)))
                    {
                        GameManager.Instance.AddGoalProgress(1);
                    }

                    if (GUILayout.Button("GIẢ LẬP THUA (Kẹt lưới)", GUILayout.Height(35)))
                    {
                        GameManager.Instance.ChangeState(GameState.LevelLost);
                    }
                }
                else if (GameManager.Instance.CurrentState == GameState.LevelWon || GameManager.Instance.CurrentState == GameState.LevelLost)
                {
                    if (GUILayout.Button("CHƠI LẠI (Retry)", GUILayout.Height(35)))
                    {
                        GameManager.Instance.RestartLevel();
                    }

                    if (GUILayout.Button("QUAY VỀ MENU", GUILayout.Height(35)))
                    {
                        GameManager.Instance.BackToMenu();
                    }
                }
            }
            else
            {
                GUILayout.Label("Không tìm thấy GameManager trong Scene!");
            }

            GUILayout.EndArea();
        }
    }
}
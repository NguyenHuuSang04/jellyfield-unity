using System.Collections.Generic;
using UnityEngine;
using JellyField.Core;
using JellyField.Level;

namespace JellyField.Managers
{
    public enum GameState
    {
        MainMenu,
        Playing,
        LevelWon,
        LevelLost
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static System.Action<GameState> OnStateChanged;
        public static System.Action OnGoalUpdated;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        private Dictionary<BlockColor, int> runtimeGoals = new Dictionary<BlockColor, int>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartGame()
        {
            ChangeState(GameState.Playing);
            SetupRuntimeGoals();
        }

        public void RestartLevel()
        {
            ChangeState(GameState.Playing);
            SetupRuntimeGoals();
        }

        public void BackToMenu()
        {
            ChangeState(GameState.MainMenu);
        }

        private void SetupRuntimeGoals()
        {
            runtimeGoals.Clear();
            GridManager grid = Object.FindFirstObjectByType<GridManager>();
            
            if (grid != null && grid.CurrentLevelData != null)
            {
                foreach (var goal in grid.CurrentLevelData.Goals)
                {
                    if (goal.Color != BlockColor.None)
                    {
                        runtimeGoals[goal.Color] = goal.Count;
                    }
                }
            }
            
            OnGoalUpdated?.Invoke();
        }

        // HÀM SỬ TRỪ MỤC TIÊU THEO MÀU THỰC TẾ KHI NỔ
        public void TrackPoppedBlocks(BlockColor color, int count)
        {
            if (CurrentState != GameState.Playing) return;

            if (runtimeGoals.ContainsKey(color))
            {
                runtimeGoals[color] = Mathf.Max(0, runtimeGoals[color] - count);
                Debug.Log($"[GameManager]: Thạch màu {color} vừa nổ! Còn lại cần nổ: {runtimeGoals[color]}");

                OnGoalUpdated?.Invoke();
                CheckWinCondition();
            }
        }

        // ===================================================================
        // BỔ SUNG TƯƠNG THÍCH NGƯỢC: Giúp DebugOverlay.cs không bị lỗi biên dịch
        // ===================================================================
        public void AddGoalProgress(int progress)
        {
            // Hàm phụ trợ để các script cũ gọi không bị crash compiler
        }

        private void CheckWinCondition()
        {
            bool isAllGoalsCleared = true;

            foreach (var kvp in runtimeGoals)
            {
                if (kvp.Value > 0)
                {
                    isAllGoalsCleared = false;
                    break;
                }
            }

            if (isAllGoalsCleared)
            {
                Debug.LogWarning("🎉🎉🎉 CHIẾN THẮNG! Bạn đã hoàn thành xuất sắc tất cả mục tiêu của màn chơi! 🎉🎉🎉");
                ChangeState(GameState.LevelWon);
            }
        }

        public int GetGoalRemainingCount(BlockColor color)
        {
            if (runtimeGoals.TryGetValue(color, out int count))
            {
                return count;
            }
            return 0;
        }

        // ĐÃ SỬA THÀNH PUBLIC: Để DebugOverlay.cs có quyền gọi thoải mái
        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"[GameManager]: Trạng thái game chuyển sang -> {newState}");
        }
    }
}
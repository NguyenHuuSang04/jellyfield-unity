using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using JellyField.Core;
using JellyField.Level;
using JellyField.Managers;
using JellyField.View;

namespace JellyField.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("🏢 Bộ Khung Panels Hệ Thống")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayHUDPanel;
        [SerializeField] private GameObject resultPopupPanel;

        [Header("🎯 Bảng Thông Báo Win/Lose Mới")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        [Header("📦 Khai Báo Khay Chứa Goal")]
        [SerializeField] private Transform goalContainerParent;
        [SerializeField] private Transform winGoalContainer;
        [SerializeField] private Transform loseGoalContainer;

        [Header("🎨 Prefabs Hiển Thị")]
        [SerializeField] private GameObject goalItemPrefab;
        [SerializeField] private GameObject goalUiPrefab;

        [Header("🔢 Dynamic HUD Text Elements")]
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI resultStatusText;

        [Header("🎛️ Bộ Nút Bấm Điều Hướng Popups")]
        [SerializeField] private Button btnHomeWin;
        [SerializeField] private Button btnNextLevel;
        [SerializeField] private Button btnHomeLose;
        [SerializeField] private Button btnRetry;

        [Header("🔄 Nút Chơi Lại Đặc Biệt Trên WinPanel (Màn Cuối)")]
        [SerializeField] private Button btnRetryWin;

        private List<GameObject> spawnedGoalItems = new List<GameObject>();
        private Dictionary<BlockColor, GoalItemUI> activeGoalUIs = new Dictionary<BlockColor, GoalItemUI>();
        private GridManager gridManager;

        void Awake()
        {
            gridManager = Object.FindFirstObjectByType<GridManager>();
        }

        void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
            GameManager.OnGoalUpdated += UpdateGoalHUD;
        }

        void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
            GameManager.OnGoalUpdated -= UpdateGoalHUD;
        }

        void Start()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);

            if (btnHomeWin != null) btnHomeWin.onClick.AddListener(GoToHome);
            if (btnHomeLose != null) btnHomeLose.onClick.AddListener(GoToHome);
            if (btnRetry != null) btnRetry.onClick.AddListener(RestartLevel);
            if (btnNextLevel != null) btnNextLevel.onClick.AddListener(LoadNextLevel);

            if (btnRetryWin != null) btnRetryWin.onClick.AddListener(RestartLevel);
        }

        private void HandleStateChanged(GameState newState)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false);
            if (resultPopupPanel != null) resultPopupPanel.SetActive(false);

            if (winPanel != null) { winPanel.transform.DOKill(); winPanel.SetActive(false); }
            if (losePanel != null) { losePanel.transform.DOKill(); losePanel.SetActive(false); }

            switch (newState)
            {
                case GameState.MainMenu:
                    if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                    break;

                case GameState.Playing:
                    if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true);
                    GenerateLevelUI();
                    break;

                case GameState.LevelWon:
                    TriggerWinState();
                    break;

                case GameState.LevelLost:
                    TriggerLoseState();
                    break;
            }
        }

        public void TriggerWinState()
        {
            if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false);
            if (resultPopupPanel != null) resultPopupPanel.SetActive(true);
            if (resultStatusText != null) resultStatusText.text = "YOU WIN! 🎉";

            if (winPanel != null)
            {
                winPanel.SetActive(true);
                winPanel.transform.localScale = Vector3.zero;
                winPanel.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
            }
            PopulatePanelGoals(winGoalContainer, true);

            bool isLastLevel = false;
            if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();

            if (gridManager != null && gridManager.CurrentLevelData != null)
            {
                if (gridManager.CurrentLevelData.LevelIndex >= 3)
                {
                    isLastLevel = true;
                }
            }

            if (isLastLevel)
            {
                if (btnNextLevel != null) btnNextLevel.gameObject.SetActive(false);
                if (btnRetryWin != null) btnRetryWin.gameObject.SetActive(true);
            }
            else
            {
                if (btnNextLevel != null) btnNextLevel.gameObject.SetActive(true);
                if (btnRetryWin != null) btnRetryWin.gameObject.SetActive(false);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.WinSound);
            if (gridManager != null) gridManager.ClearActiveBoard();
        }

        public void TriggerLoseState()
        {
            if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false);
            if (resultPopupPanel != null) resultPopupPanel.SetActive(true);
            if (resultStatusText != null) resultStatusText.text = "GAME OVER 😢";

            if (losePanel != null)
            {
                losePanel.SetActive(true);
                losePanel.transform.localScale = Vector3.zero;
                losePanel.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
            }
            PopulatePanelGoals(loseGoalContainer, false);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.LoseSound);
            if (gridManager != null) gridManager.ClearActiveBoard();
        }

        private void GenerateLevelUI()
        {
            if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();
            if (gridManager == null || gridManager.CurrentLevelData == null) return;

            LevelData currentLevel = gridManager.CurrentLevelData;

            if (levelTitleText != null) levelTitleText.text = $"LEVEL {currentLevel.LevelIndex}";

            foreach (var item in spawnedGoalItems)
            {
                if (item != null) Destroy(item);
            }
            spawnedGoalItems.Clear();
            activeGoalUIs.Clear();

            if (goalItemPrefab != null && goalContainerParent != null)
            {
                foreach (var goal in currentLevel.Goals)
                {
                    GameObject newGoalObj = Instantiate(goalItemPrefab, goalContainerParent);
                    spawnedGoalItems.Add(newGoalObj);

                    GoalItemUI goalUI = newGoalObj.GetComponent<GoalItemUI>();
                    if (goalUI != null)
                    {
                        int currentCount = GameManager.Instance.GetGoalRemainingCount(goal.Color);
                        goalUI.SetupGoal(goal.Color, currentCount);
                        activeGoalUIs[goal.Color] = goalUI;
                    }
                }
            }
        }

        private void UpdateGoalHUD()
        {
            if (GameManager.Instance == null) return;

            foreach (var kvp in activeGoalUIs)
            {
                BlockColor color = kvp.Key;
                GoalItemUI goalUI = kvp.Value;

                if (goalUI != null)
                {
                    int remaining = GameManager.Instance.GetGoalRemainingCount(color);
                    goalUI.SetupGoal(color, remaining);
                }
            }
        }

        private void PopulatePanelGoals(Transform container, bool isWin)
        {
            if (container == null) return;

            foreach (Transform child in container) Destroy(child.gameObject);

            if (gridManager == null || gridManager.CurrentLevelData == null) return;

            GameObject prefabToUse = goalUiPrefab != null ? goalUiPrefab : goalItemPrefab;
            if (prefabToUse == null) return;

            foreach (var goal in gridManager.CurrentLevelData.Goals)
            {
                GameObject goalItem = Instantiate(prefabToUse, container);

                GoalItemUI goalUI = goalItem.GetComponent<GoalItemUI>();
                if (goalUI != null)
                {
                    int countToShow = isWin ? goal.Count : GameManager.Instance.GetGoalRemainingCount(goal.Color);
                    goalUI.SetupGoal(goal.Color, countToShow);
                }
            }
        }

        private void LoadNextLevel()
        {
            if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();
            if (gridManager == null || gridManager.AllLevels == null || gridManager.AllLevels.Count == 0) return;

            int currentIndex = gridManager.AllLevels.IndexOf(gridManager.CurrentLevelData);

            if (currentIndex != -1 && currentIndex < gridManager.AllLevels.Count - 1)
            {
                Debug.Log($"[GameUIManager]: Bấm NEXT LEVEL -> Tiến tới màn chơi tiếp theo: LEVEL {currentIndex + 2}");

                MainMenuManager.ChosenLevelData = gridManager.AllLevels[currentIndex + 1];

                UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
            }
        }

        // ===================================================================
        // 🔄 ĐÃ SỬA CHUẨN: Giữ lại duy nhất bộ hàm chuyển Scene xịn sò của Sang
        // ===================================================================
        private void GoToHome()
        {
            Debug.Log("[GameUIManager]: Bấm HOME -> Chuyển từ GameplayScene quay về MenuScene.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
        }

        private void RestartLevel()
        {
            Debug.Log("[GameUIManager]: Bấm RETRY -> Tải lại màn chơi hiện tại.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene");
        }

        // ===================================================================
        // 🔥 ĐÃ KHÔI PHỤC: Các hàm kết nối Inspector cho nút bấm trên màn hình HUD
        // ===================================================================
        public void OnPlayButtonPressed() => GameManager.Instance.StartGame();
        public void OnRetryButtonPressed() => RestartLevel();
        public void OnMenuButtonPressed() => GoToHome();
    }
}
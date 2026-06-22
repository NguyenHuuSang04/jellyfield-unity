using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("🏢 Bộ Khung Panels Hệ Thống")]
    public GameObject mainMenuPanel;
    public GameObject gameplayHUDPanel;
    public GameObject resultPopupPanel;

    [Header("🎯 Bảng Thông Báo Win/Lose Mới")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("📦 Khai Báo Khay Chứa Goal")]
    public Transform goalContainerParent;
    public Transform winGoalContainer;
    public Transform loseGoalContainer;

    [Header("🎨 Prefabs Hiển Thị")]
    public GameObject goalItemPrefab;
    public GameObject goalUiPrefab;

    [Header("🔢 Dynamic HUD Text Elements")]
    public TextMeshProUGUI levelTitleText;
    public TextMeshProUGUI resultStatusText;

    [Header("🎛️ Bộ Nút Bấm Điều Hướng Popups")]
    public Button btnHomeWin;
    public Button btnNextLevel;
    public Button btnHomeLose;
    public Button btnRetry;

    [Header("🔄 Nút Chơi Lại Đặc Biệt Trên WinPanel (Màn Cuối)")]
    public Button btnRetryWin;

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

        if (gridManager != null && gridManager.currentLevelData != null)
        {
            if (gridManager.currentLevelData.LevelIndex >= 3)
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

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.winSound);
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

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.loseSound);
        if (gridManager != null) gridManager.ClearActiveBoard();
    }

    private void GenerateLevelUI()
    {
        if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();
        if (gridManager == null || gridManager.currentLevelData == null) return;

        LevelData currentLevel = gridManager.currentLevelData;

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

        if (gridManager == null || gridManager.currentLevelData == null) return;

        GameObject prefabToUse = goalUiPrefab != null ? goalUiPrefab : goalItemPrefab;
        if (prefabToUse == null) return;

        foreach (var goal in gridManager.currentLevelData.Goals)
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
        if (gridManager == null || gridManager.allLevels == null || gridManager.allLevels.Count == 0) return;

        int currentIndex = gridManager.allLevels.IndexOf(gridManager.currentLevelData);

        if (currentIndex != -1 && currentIndex < gridManager.allLevels.Count - 1)
        {
            Debug.Log($"[GameUIManager]: Bấm NEXT LEVEL -> Tiến tới màn chơi tiếp theo: LEVEL {currentIndex + 2}");

            MainMenuManager.ChosenLevelData = gridManager.allLevels[currentIndex + 1];

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
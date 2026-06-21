using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Kích hoạt bộ thư viện hoạt họa DOTween xịn sò của dự án

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
    public Transform goalContainerParent; // Khay chứa trên HUD chính trận
    public Transform winGoalContainer;     // Khay chứa trên popup Chiến Thắng
    public Transform loseGoalContainer;    // Khay chứa trên popup Thua Cuộc

    [Header("🎨 Prefabs Hiển Thị")]
    public GameObject goalItemPrefab;     // Prefab ô mục tiêu trên HUD
    public GameObject goalUiPrefab;       // Prefab ô mục tiêu trên popup

    [Header("🔢 Dynamic HUD Text Elements")]
    public TextMeshProUGUI levelTitleText; 
    public TextMeshProUGUI resultStatusText; 

    [Header("🎛️ Bộ Nút Bấm Điều Hướng Popups")]
    public Button btnHomeWin;
    public Button btnNextLevel;
    public Button btnHomeLose;
    public Button btnRetry;

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

                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.winSound);

                if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();
                if (gridManager != null) gridManager.ClearActiveBoard();
                break;
                
            case GameState.LevelLost:
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

                if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();
                if (gridManager != null) gridManager.ClearActiveBoard();
                break;
        }
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

    private void GoToHome()
    {
        if (GameManager.Instance != null) GameManager.Instance.BackToMenu();
    }

    private void RestartLevel()
    {
        if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
    }

    private void LoadNextLevel()
    {
        // Chèn logic tăng chỉ số màn chơi tiếp theo của bạn ở đây nhé
    }

    public void OnPlayButtonPressed() => GameManager.Instance.StartGame();
    public void OnRetryButtonPressed() => GameManager.Instance.RestartLevel();
    public void OnMenuButtonPressed() => GameManager.Instance.BackToMenu();
}
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameplayHUDPanel;
    public GameObject resultPopupPanel;

    [Header("Dynamic HUD Elements (Data-Driven)")]
    public TextMeshProUGUI levelTitleText; // Gắn Txt_LevelTitle vào đây để tự đổi "LEVEL 1", "LEVEL 2"...
    public Transform goalContainerParent; // Gắn Goal_Container hoặc Layout Group con của nó vào đây
    public GameObject goalItemPrefab;     // Kéo file Prefab "GoalItem_Placeholder" vào đây
    public TextMeshProUGUI resultStatusText; 

    private List<GameObject> spawnedGoalItems = new List<GameObject>();
    private GridManager gridManager;

    void Start()
    {
        // Tìm GridManager trong Scene để lấy dữ liệu LevelData đang chạy
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

    private void HandleStateChanged(GameState newState)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false);
        if (resultPopupPanel != null) resultPopupPanel.SetActive(false);

        switch (newState)
        {
            case GameState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
                
            case GameState.Playing:
                if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true);
                GenerateLevelUI(); // TỰ ĐỘNG KHỞI TẠO ICON THEO LEVEL CẤU HÌNH
                break;
                
            case GameState.LevelWon:
                if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true);
                if (resultPopupPanel != null) resultPopupPanel.SetActive(true);
                if (resultStatusText != null) resultStatusText.text = "YOU WIN! 🎉";
                break;
                
            case GameState.LevelLost:
                if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true);
                if (resultPopupPanel != null) resultPopupPanel.SetActive(true);
                if (resultStatusText != null) resultStatusText.text = "GAME OVER 😢";
                break;
        }
    }

    // Hàm quét file dữ liệu màn chơi để tự động sinh số lượng mục tiêu tương ứng
    private void GenerateLevelUI()
    {
        if (gridManager == null || gridManager.currentLevelData == null) return;
        LevelData currentLevel = gridManager.currentLevelData;

        // 1. Tự động cập nhật tên Level hiển thị trên cùng
        if (levelTitleText != null) levelTitleText.text = $"LEVEL {currentLevel.LevelIndex}";

        // 2. Dọn dẹp sạch sẽ các Icon mục tiêu cũ của màn chơi trước đó (nếu có)
        foreach (var item in spawnedGoalItems)
        {
            if (item != null) Destroy(item);
        }
        spawnedGoalItems.Clear();

        // 3. Duyệt qua danh sách Goals được cấu hình riêng trong ScriptableObject của màn chơi đó
        if (goalItemPrefab != null && goalContainerParent != null)
        {
            foreach (var goal in currentLevel.Goals)
            {
                // Sinh ra một cụm Icon + Chữ số lượng từ Prefab mẫu
                GameObject newGoalObj = Instantiate(goalItemPrefab, goalContainerParent);
                spawnedGoalItems.Add(newGoalObj);

                // Gọi component điều khiển hiển thị để gán đúng màu thạch và số lượng mục tiêu
                GoalItemUI goalUI = newGoalObj.GetComponent<GoalItemUI>();
                if (goalUI != null)
                {
                    goalUI.SetupGoal(goal.Color, goal.Count);
                }
            }
        }
    }

    private void UpdateGoalHUD()
    {
        // Đồng bộ cập nhật điểm số khi thạch nổ ăn điểm trong quá trình chơi
    }

    public void OnPlayButtonPressed() => GameManager.Instance.StartGame();
    public void OnRetryButtonPressed() => GameManager.Instance.RestartLevel();
    public void OnMenuButtonPressed() => GameManager.Instance.BackToMenu();
}
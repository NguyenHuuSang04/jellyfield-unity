using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Current State")]
    private GameState _currentState;
    public GameState CurrentState => _currentState;

    // Các Sự Kiện (Events) hướng dữ liệu để UI Canvas đăng ký lắng nghe sau này
    public static event Action<GameState> OnStateChanged;
    public static event Action OnGoalUpdated;

    [Header("Gameplay Data (Mockup)")]
    public int targetScore = 4;
    private int currentScore = 0;

    void Awake()
    {
        // Khởi tạo mô hình Singleton an toàn
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

    void Start()
    {
        // Bắt đầu game từ màn hình chờ MainMenu
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        _currentState = newState;
        Debug.Log($"[GameManager]: Trạng thái game chuyển sang -> {_currentState}");

        // Phát sự kiện ra toàn hệ thống
        OnStateChanged?.Invoke(_currentState);

        // Xử lý logic đặc thù cho từng trạng thái
        switch (newState)
        {
            case GameState.MainMenu:
                // Tạm thời log để test, Ngày 4 UI sẽ tự bật panel tương ứng
                break;
            case GameState.Playing:
                currentScore = 0;
                break;
            case GameState.LevelWon:
                Debug.Log("🎉 CHIẾN THẮNG MÀN CHƠI! Đủ chỉ tiêu Goals.");
                break;
            case GameState.LevelLost:
                Debug.Log("😢 THUA CUỘC! Bàn chơi kẹt cứng, không còn ô trống.");
                break;
        }
    }

    // Hàm API công khai để tăng điểm mục tiêu (Sẽ gọi khi Match thành công ở Ngày 2)
    public void AddGoalProgress(int amount)
    {
        if (_currentState != GameState.Playing) return;

        currentScore += amount;
        Debug.Log($"[Goal Tracker]: Tiến độ mục tiêu hiện tại = {currentScore}/{targetScore}");
        
        OnGoalUpdated?.Invoke();

        if (currentScore >= targetScore)
        {
            ChangeState(GameState.LevelWon);
        }
    }

    // Các hàm Helper cho nút bấm UI ngày mai kết nối nhanh
    public void StartGame() => ChangeState(GameState.Playing);
    public void RestartLevel() => ChangeState(GameState.Playing);
    public void BackToMenu() => ChangeState(GameState.MainMenu);
}
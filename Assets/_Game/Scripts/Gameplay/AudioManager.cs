using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource efxSource; // Gắn Component AudioSource dùng để phát tiếng động ngắn

    [Header("Audio Clips")]
    public AudioClip dropSound;    // Tiếng bộp/vút khi đặt khối thạch xuống lưới
    public AudioClip popSound;     // Tiếng pop giòn giã khi thạch match nổ combo
    public AudioClip winSound;     // Tiếng chuông reo vang khi qua màn thắng cuộc
    public AudioClip loseSound;    // Tiếng âm trầm dứt khoát khi kẹt lưới thua cuộc

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

    void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện từ GameManager để tự động phát âm thanh Win/Lose
        GameManager.OnStateChanged += HandleGameStateAudio;
    }

    void OnDisable()
    {
        GameManager.OnStateChanged -= HandleGameStateAudio;
    }

    private void HandleGameStateAudio(GameState state)
    {
        switch (state)
        {
            case GameState.LevelWon:
                PlaySound(winSound);
                break;
            case GameState.LevelLost:
                PlaySound(loseSound);
                break;
        }
    }

    // Hàm API công khai dùng để phát âm thanh hiệu ứng một cách nhanh chóng
    public void PlaySound(AudioClip clip)
    {
        if (efxSource != null && clip != null)
        {
            efxSource.PlayOneShot(clip);
        }
    }
}
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("🔊 Audio Sources (Tự động gán)")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("🎵 Nhạc Nền")]
    public AudioClip backgroundMusic;

    [Header("✨ Hiệu Ứng Âm Thanh (Audio Clips)")]
    [Tooltip("Tiếng 'pop' dùng cho cả 2 sự kiện: Click bốc khối ở Dock và Thả khối xuống lưới")]
    public AudioClip popSound;

    [Tooltip("Tiếng 'match' dùng riêng khi các khối màu chạm biên gộp nổ dây chuyền")]
    public AudioClip matchSound;

    public AudioClip winSound;
    public AudioClip loseSound;

    // ===================================================================
    // 🚀 ĐƯỜNG TRUYỀN LIÊN THÔNG ĐÃ SỬA CHUẨN ĐÉT:
    // Hễ bên DraggableGroup gọi chữ 'dropSound' cũ khi đặt thạch, 
    // hệ thống sẽ tự động bốc tiếng 'popSound' ra phát mượt mà!
    // ===================================================================
    public AudioClip dropSound => popSound;
    // ===================================================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        // TÌM TẤT CẢ LOA: Lấy danh sách toàn bộ cấu phần AudioSource đang nằm trên GameManager
        AudioSource[] sources = GetComponents<AudioSource>();

        // Phân bổ loa số 1 chuyên trị SFX ngắn
        if (sources.Length >= 1) sfxSource = sources[0];
        else sfxSource = gameObject.AddComponent<AudioSource>();

        // Phân bổ loa số 2 chuyên trị Nhạc nền BGM
        if (sources.Length >= 2) bgmSource = sources[1];
        else bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;

        PlayBGM();
    }

    public void PlayBGM()
    {
        if (backgroundMusic != null && bgmSource != null)
        {
            bgmSource.clip = backgroundMusic;
            bgmSource.Play();
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("🔊 Audio Sources Component")]
    [SerializeField] private AudioSource bgmSource; // AudioSource số 1 (Dành riêng cho BGM)
    [SerializeField] private AudioSource sfxSource; // AudioSource số 2 (Dành riêng cho SFX)

    [Header("🎵 Background Music Clips")]
    [SerializeField] private AudioClip menuBGM;

    [Header("💥 Gameplay SFX Clips")]
    [SerializeField] private AudioClip popSound;
    [SerializeField] private AudioClip matchSound;
    [SerializeField] private AudioClip dropSound;
    
    // 🔥 FIX LỖI ĐỎ: Trả lại đúng 2 tên biến cũ cho GameUIManager.cs gọi bắn tiếng
    [SerializeField] private AudioClip winSound;  
    [SerializeField] private AudioClip loseSound; 

    public AudioSource BgmSource => bgmSource;
    public AudioSource SfxSource => sfxSource;
    public AudioClip MenuBGM => menuBGM;
    public AudioClip PopSound => popSound;
    public AudioClip MatchSound => matchSound;
    public AudioClip DropSound => dropSound;
    public AudioClip WinSound => winSound;
    public AudioClip LoseSound => loseSound;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ AudioManager sống xuyên suốt game
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        PlayBackgroundMusic();
    }

    public void PlayBackgroundMusic()
    {
        if (bgmSource != null && menuBGM != null && !bgmSource.isPlaying)
        {
            bgmSource.clip = menuBGM;
            bgmSource.loop = true; 
            bgmSource.Play();
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
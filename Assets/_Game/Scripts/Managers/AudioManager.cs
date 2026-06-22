using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("🔊 Audio Sources Component")]
    public AudioSource bgmSource; // AudioSource số 1 (Dành riêng cho BGM)
    public AudioSource sfxSource; // AudioSource số 2 (Dành riêng cho SFX)

    [Header("🎵 Background Music Clips")]
    public AudioClip menuBGM;

    [Header("💥 Gameplay SFX Clips")]
    public AudioClip popSound;
    public AudioClip matchSound;
    public AudioClip dropSound;
    
    // 🔥 FIX LỖI ĐỎ: Trả lại đúng 2 tên biến cũ cho GameUIManager.cs gọi bắn tiếng
    public AudioClip winSound;  
    public AudioClip loseSound; 

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
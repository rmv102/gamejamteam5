using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    [Header("Background Music Settings")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loopMusic = true;
    
    [Header("Audio Source Settings")]
    [SerializeField] private bool createAudioSource = true;
    
    private AudioSource audioSource;
    
    void Start()
    {
        SetupAudioSource();
        
        if (playOnStart && backgroundMusic != null)
        {
            PlayBackgroundMusic();
        }
    }
    
    void SetupAudioSource()
    {
        // Find existing AudioSource or create one
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null && createAudioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (audioSource != null)
        {
            // Configure AudioSource for background music
            audioSource.clip = backgroundMusic;
            audioSource.volume = musicVolume;
            audioSource.loop = loopMusic;
            audioSource.playOnAwake = false;
            audioSource.priority = 0; // Lower priority for background music
            audioSource.spatialBlend = 0f; // 2D sound (not 3D positional)
        }
        else
        {
            Debug.LogError("BackgroundMusicManager: No AudioSource component found and createAudioSource is false!");
        }
    }
    
    public void PlayBackgroundMusic()
    {
        if (audioSource != null && backgroundMusic != null)
        {
            audioSource.Play();
            Debug.Log("BackgroundMusicManager: Started playing background music");
        }
        else
        {
            if (audioSource == null)
                Debug.LogError("BackgroundMusicManager: No AudioSource component!");
            if (backgroundMusic == null)
                Debug.LogError("BackgroundMusicManager: No background music clip assigned!");
        }
    }
    
    public void StopBackgroundMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("BackgroundMusicManager: Stopped background music");
        }
    }
    
    public void PauseBackgroundMusic()
    {
        if (audioSource != null)
        {
            audioSource.Pause();
            Debug.Log("BackgroundMusicManager: Paused background music");
        }
    }
    
    public void ResumeBackgroundMusic()
    {
        if (audioSource != null)
        {
            audioSource.UnPause();
            Debug.Log("BackgroundMusicManager: Resumed background music");
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = musicVolume;
        }
    }
    
    public void SetBackgroundMusic(AudioClip newMusic)
    {
        backgroundMusic = newMusic;
        if (audioSource != null)
        {
            audioSource.clip = backgroundMusic;
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Play Music")]
    public void PlayMusicDebug()
    {
        PlayBackgroundMusic();
    }
    
    [ContextMenu("Stop Music")]
    public void StopMusicDebug()
    {
        StopBackgroundMusic();
    }
    
    [ContextMenu("Pause Music")]
    public void PauseMusicDebug()
    {
        PauseBackgroundMusic();
    }
    
    [ContextMenu("Resume Music")]
    public void ResumeMusicDebug()
    {
        ResumeBackgroundMusic();
    }
    
    // Public getters
    public bool IsPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }
    
    public float GetCurrentVolume()
    {
        return audioSource != null ? audioSource.volume : 0f;
    }
}

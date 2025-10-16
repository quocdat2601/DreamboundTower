using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource engineSource; // Dedicated source for smooth engine transitions

    [Header("Music")]
    public AudioClip background;
    
    [Header("Player SFX")]
    public AudioClip jumpLanding;  // Sound when player lands after jumping
    public AudioClip engineIdle;
    public AudioClip engineRev;
    public AudioClip boost;
    
    [Header("Collectible SFX")]
    public AudioClip pointCollect;    // For Apple, Diamond (score items)
    public AudioClip buffCollect;     // For Health, Shield, SpeedBoost (power-ups)
    
    [Header("Collision SFX")]
    public AudioClip carHit;
    public AudioClip coneHit;
    public AudioClip meteoriteHit;
    public AudioClip death;
    
    [Header("Game State SFX")]
    public AudioClip gameOver;
    
    [Header("UI SFX")]
    public AudioClip buttonClick;
    public AudioClip buttonHover;
    public AudioClip menuOpen;
    public AudioClip menuClose;

    [Header("Mixer (optional)")]
    public AudioMixer audioMixer; // route your sources to Mixer Groups in Inspector
    public string musicVolumeParameter = "MusicVolume";
    public string sfxVolumeParameter = "SFXVolume";

    void Awake()
    {
        // Singleton persistent across scenes
        if (Instance != null && Instance != this)
        {
            Debug.Log($"AudioManager: Destroying duplicate instance in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Destroy(gameObject);
            return;
        }
        
        // Only create instance if none exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"AudioManager: Created singleton instance in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        }
        else
        {
            Debug.Log($"AudioManager: Instance already exists, destroying duplicate in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (musicSource != null && background != null)
        {
            if (musicSource.clip != background)
            {
                musicSource.clip = background;
            }
            if (!musicSource.isPlaying) musicSource.Play();
        }

        // Load saved volumes
        float music = PlayerPrefs.GetFloat("MusicVolume01", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume01", 1f);
        SetMusicVolume01(music);
        SetSFXVolume01(sfx);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
    
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
    
    // Player SFX Methods
    public void PlayJumpLandingSound()
    {
        // Play landing sound with higher volume for better impact
        PlaySFX(jumpLanding, 1.2f); // 20% louder than normal SFX
    }
    
    // Smooth engine sound transitions
    private Coroutine engineTransitionCoroutine;
    
    public void PlayEngineIdle()
    {
        if (engineIdle != null && engineSource != null)
        {
            StartEngineTransition(engineIdle, 1.0f);
        }
    }
    
    public void PlayEngineRev()
    {
        if (engineRev != null && engineSource != null)
        {
            StartEngineTransition(engineRev, 1.2f);
        }
    }
    
    public void StopEngineSound()
    {
        if (engineSource != null && engineSource.isPlaying)
        {
            if (engineTransitionCoroutine != null)
            {
                StopCoroutine(engineTransitionCoroutine);
            }
            StartCoroutine(FadeOutEngineSound());
        }
    }
    
    public void ResetEngineSoundState()
    {
        // Stop any playing engine sound
        if (engineSource != null && engineSource.isPlaying)
        {
            if (engineTransitionCoroutine != null)
            {
                StopCoroutine(engineTransitionCoroutine);
            }
            engineSource.Stop();
        }
        
        // Reset engine transition coroutine
        engineTransitionCoroutine = null;
    }
    
    public void ResetAllAudio()
    {
        // Stop all audio sources
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        
        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
        
        // Use existing ResetEngineSoundState method to avoid code duplication
        ResetEngineSoundState();
        
        // Reset engine source properties
        if (engineSource != null)
        {
            engineSource.clip = null;
            engineSource.pitch = 1.0f;
            engineSource.loop = false;
        }
    }
    
    public void OnPlayerDeath()
    {
        // Don't stop engine sounds when player takes damage
        // Only play death sound for impact feedback
        PlayDeathSound();
    }
    
    public void OnGameOver()
    {
        // Reset all audio for clean state
        ResetAllAudio();
        
        // Play game over sound
        PlayGameOverSound();
    }
    
    public void OnSceneTransition()
    {
        // Don't reset engine sounds when transitioning to Game scene
        // Only reset when going to MainMenu or other scenes
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Only reset engine sound when leaving Game scene (going to MainMenu)
        if (currentScene == "Game")
        {
            return;
        }
        
        // Don't reset engine sound when going from MainMenu to Game
        if (currentScene == "MainMenu")
        {
            return;
        }
        
        ResetEngineSoundState();
    }
    
    private void StartEngineTransition(AudioClip newClip, float targetPitch, float volumeMultiplier = 1.0f)
    {
        if (engineTransitionCoroutine != null)
        {
            StopCoroutine(engineTransitionCoroutine);
        }
        engineTransitionCoroutine = StartCoroutine(TransitionEngineSound(newClip, targetPitch, volumeMultiplier));
    }
    
    private System.Collections.IEnumerator TransitionEngineSound(AudioClip newClip, float targetPitch, float volumeMultiplier = 1.0f)
    {
        if (engineSource.clip != newClip)
        {
            // Instant transition to match player movement speed
            float startVolume = engineSource.volume;
            float targetVolume = startVolume * volumeMultiplier;
            
            // Switch to new clip immediately
            engineSource.clip = newClip;
            engineSource.pitch = targetPitch;
            engineSource.loop = true;
            engineSource.Play();
            engineSource.volume = targetVolume;
        }
        else
        {
            // Same clip, instant pitch change
            engineSource.pitch = targetPitch;
            engineSource.volume = engineSource.volume * volumeMultiplier;
        }
        
        engineTransitionCoroutine = null;
        yield return null; // Single frame delay to prevent issues
    }
    
    private System.Collections.IEnumerator FadeOutEngineSound()
    {
        float startVolume = engineSource.volume;
        float fadeTime = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            engineSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }
        
        engineSource.Stop();
        engineSource.volume = startVolume; // Reset volume for next time
    }
    
    public void PlayBoostSound()
    {
        PlaySFX(boost);
    }
    
    //// Collectible SFX Methods
    //public void PlayCollectibleSound(CollectibleType collectibleType)
    //{
    //    switch (collectibleType)
    //    {
    //        case CollectibleType.Apple:
    //        case CollectibleType.Diamond:
    //            // Point collectibles (score items) - quieter volume
    //            PlaySFX(pointCollect, 0.6f);
    //            break;
    //        case CollectibleType.Health:
    //        case CollectibleType.Shield:
    //        case CollectibleType.SpeedBoost:
    //            // Buff collectibles (power-ups) - quieter volume
    //            PlaySFX(buffCollect, 0.6f);
    //            break;
    //        default:
    //            PlaySFX(pointCollect, 0.6f); // Default to point collect sound - quieter volume
    //            break;
    //    }
    //}
    
    // Collision SFX Methods
    public void PlayCollisionSound(string obstacleType)
    {
        switch (obstacleType.ToLower())
        {
            case "car":
                PlaySFX(carHit, 0.6f); // Quieter collision sound
                break;
            case "cone":
            case "trafficcone":
                PlaySFX(coneHit, 0.6f); // Quieter collision sound
                break;
            case "meteorite":
                PlaySFX(meteoriteHit, 0.6f); // Quieter collision sound
                break;
            default:
                PlaySFX(carHit, 0.6f); // Default collision sound - quieter volume
                break;
        }
    }
    
    public void PlayDeathSound()
    {
        PlaySFX(death);
    }
    
    // Game State SFX Methods
    public void PlayGameOverSound()
    {
        PlaySFX(gameOver);
    }
    
    // UI SFX Methods
    public void PlayButtonClickSound()
    {
        PlaySFX(buttonClick);
    }
    
    public void PlayButtonHoverSound()
    {
        PlaySFX(buttonHover);
    }
    
    public void PlayMenuOpenSound()
    {
        PlaySFX(menuOpen);
    }
    
    public void PlayMenuCloseSound()
    {
        PlaySFX(menuClose);
    }

    public void PauseAudio()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Pause();
        if (engineSource != null && engineSource.isPlaying) engineSource.Pause();
    }

    public void ResumeAudio()
    {
        if (musicSource != null) musicSource.UnPause();
        if (sfxSource != null) sfxSource.UnPause();
        if (engineSource != null) engineSource.UnPause();
    }

    // volume in 0..1
    public void SetMusicVolume01(float value)
    {
        value = Mathf.Clamp01(value);
        if (audioMixer != null && !string.IsNullOrEmpty(musicVolumeParameter))
        {
            audioMixer.SetFloat(musicVolumeParameter, ToDecibels(value));
        }
        if (musicSource != null) musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume01", value);
    }

    public void SetSFXVolume01(float value)
    {
        value = Mathf.Clamp01(value);
        if (audioMixer != null && !string.IsNullOrEmpty(sfxVolumeParameter))
        {
            audioMixer.SetFloat(sfxVolumeParameter, ToDecibels(value));
        }
        if (sfxSource != null) sfxSource.volume = value;
        if (engineSource != null) engineSource.volume = value; // Engine sounds also respond to SFX volume
        PlayerPrefs.SetFloat("SFXVolume01", value);
    }

    static float ToDecibels(float value01)
    {
        // Map 0..1 to -80dB..0dB (avoid -Infinity at 0)
        return value01 > 0.0001f ? Mathf.Log10(value01) * 20f : -80f;
    }
}

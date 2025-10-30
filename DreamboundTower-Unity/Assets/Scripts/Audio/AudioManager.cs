using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Background Music")]
    public List<AudioClip> combatMusicClips;
    public List<AudioClip> mapMusicClips;

    [Header("Combat SFX")]
    public AudioClip attackSFX;
    public AudioClip criticalHitSFX;
    public AudioClip missSFX;
    public AudioClip skillAttackSFX;
    public AudioClip skillBuffSFX;

    [Header("UI SFX")]
    public AudioClip buttonClick;
    public AudioClip buttonHover;
    public AudioClip menuOpen;
    public AudioClip menuClose;

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
            return;
        }

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        LoadVolumeSettings();
    }

    // --- CÁC HÀM PHÁT ÂM THANH  ---
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayRandomCombatMusic()
    {
        if (combatMusicClips != null && combatMusicClips.Count > 0)
        {
            AudioClip clipToPlay = combatMusicClips[Random.Range(0, combatMusicClips.Count)];
            PlayMusic(clipToPlay);
        }
        else { Debug.LogWarning("AudioManager: No Combat Music Clips assigned."); }
    }

    public void PlayRandomMapMusic()
    {
        if (mapMusicClips != null && mapMusicClips.Count > 0)
        {
            AudioClip clipToPlay = mapMusicClips[Random.Range(0, mapMusicClips.Count)];
            PlayMusic(clipToPlay);
        }
        else { Debug.LogWarning("AudioManager: No Map Music Clips assigned."); }
    }

    // --- HÀM HELPER SFX  ---
    public void PlayAttackSFX() { if (attackSFX != null) PlaySFX(attackSFX); }
    public void PlayCriticalHitSFX() { if (criticalHitSFX != null) PlaySFX(criticalHitSFX); }
    public void PlayMissSFX() { if (missSFX != null) PlaySFX(missSFX); }
    public void PlaySkillAttackSFX() { if (skillAttackSFX != null) PlaySFX(skillAttackSFX); }
    public void PlaySkillBuffSFX() { if (skillBuffSFX != null) PlaySFX(skillBuffSFX); }
    public void PlayButtonClickSFX() { if (buttonClick != null) PlaySFX(buttonClick); }
    public void PlayButtonHoverSFX() { if (buttonHover != null) PlaySFX(buttonHover); }

    public void PauseAudio()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Pause();
    }

    public void ResumeAudio()
    {
        if (musicSource != null) musicSource.UnPause();
        if (sfxSource != null) sfxSource.UnPause();
    }

    // volume in 0..1
    public void SetMusicVolume01(float value)
    {
        value = Mathf.Clamp01(value);
        if (musicSource != null) musicSource.volume = value; // Chỉ set volume trực tiếp
        PlayerPrefs.SetFloat("MusicVolume01", value);
    }

    public void SetSFXVolume01(float value)
    {
        value = Mathf.Clamp01(value);
        if (sfxSource != null) sfxSource.volume = value; // Chỉ set volume trực tiếp
        PlayerPrefs.SetFloat("SFXVolume01", value);
    }

    private void LoadVolumeSettings()
    {
        float musicVol = PlayerPrefs.GetFloat("MusicVolume01", 1f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume01", 1f);
        // Gọi hàm Set... mới để áp dụng volume ban đầu
        SetMusicVolume01(musicVol);
        SetSFXVolume01(sfxVol);
    }
}
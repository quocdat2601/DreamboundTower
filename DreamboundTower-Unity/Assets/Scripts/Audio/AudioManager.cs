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

    private AudioClip pausedMapClip = null;
    private float pausedMapClipTime = 0f;
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
        if (combatMusicClips == null || combatMusicClips.Count == 0)
        {
            Debug.LogWarning("AudioManager: No Combat Music Clips assigned.");
            return;
        }

        // --- ✅ LOGIC PAUSE MỚI ---
        // 1. Kiểm tra xem có phải nhạc Map đang chạy không
        if (musicSource.isPlaying && mapMusicClips.Contains(musicSource.clip))
        {
            // 2. Lưu lại bài nhạc và thời điểm
            pausedMapClip = musicSource.clip;
            pausedMapClipTime = musicSource.time;
            Debug.Log($"[Audio] Pausing map music '{pausedMapClip.name}' at {pausedMapClipTime}s.");
            // Không cần gọi Pause(), vì PlayMusic() ở dưới sẽ tự Stop
        }
        // --- KẾT THÚC LOGIC MỚI ---

        // 3. Phát nhạc combat (logic cũ của bạn)
        AudioClip clipToPlay = combatMusicClips[Random.Range(0, combatMusicClips.Count)];
        PlayMusic(clipToPlay); //
    }

    public void PlayRandomMapMusic()
    {
        if (mapMusicClips == null || mapMusicClips.Count == 0)
        {
            Debug.LogWarning("AudioManager: No Map Music Clips assigned.");
            return;
        }

        // --- ✅ LOGIC RESUME MỚI ---
        // ƯU TIÊN 1: Kiểm tra xem có bài nhạc Map nào đang chờ resume không
        if (pausedMapClip != null)
        {
            Debug.Log($"[Audio] Resuming map music '{pausedMapClip.name}' at {pausedMapClipTime}s.");

            // Lấy lại bài nhạc và thời gian
            AudioClip clipToResume = pausedMapClip;
            float timeToResume = pausedMapClipTime;

            // Xóa trạng thái pause
            pausedMapClip = null;
            pausedMapClipTime = 0f;

            // Phát lại đúng bài đó
            PlayMusic(clipToResume); //

            // Tua đến đúng thời điểm
            musicSource.time = timeToResume;
            return; // Xong
        }
        // --- KẾT THÚC LOGIC RESUME ---

        // ƯU TIÊN 2: (Logic cũ của bạn) Nếu nhạc Map đang phát, không làm gì cả
        if (musicSource.isPlaying && mapMusicClips.Contains(musicSource.clip))
        {
            return; // Đang phát nhạc map rồi, không đổi bài
        }

        // ƯU TIÊN 3: Nếu không có gì (vừa vào game / vừa hết bài)
        // Hoặc nếu đang phát nhạc Combat (thì phải đổi)
        // -> Phát một bài nhạc Map ngẫu nhiên MỚI
        AudioClip clipToPlay = mapMusicClips[Random.Range(0, mapMusicClips.Count)];
        PlayMusic(clipToPlay);
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
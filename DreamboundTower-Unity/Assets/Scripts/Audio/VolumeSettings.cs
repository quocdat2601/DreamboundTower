using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioMixer audioMixer; // optional
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    void Start()
    {
        // Initialize sliders from saved values
        float music = PlayerPrefs.GetFloat("MusicVolume01", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume01", 1f);

        if (musicSlider != null) musicSlider.value = music;
        if (sfxSlider != null) sfxSlider.value = sfx;

        ApplyVolumes();
        // Subscribe to changes
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChange);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChange);
    }

    void OnDestroy()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChange);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXChange);
    }

    void OnMusicChange(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume01(value);
    }

    void OnSFXChange(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume01(value);
    }

    void ApplyVolumes()
    {
        if (AudioManager.Instance == null) return;
        if (musicSlider != null) AudioManager.Instance.SetMusicVolume01(musicSlider.value);
        if (sfxSlider != null) AudioManager.Instance.SetSFXVolume01(sfxSlider.value);
    }
}

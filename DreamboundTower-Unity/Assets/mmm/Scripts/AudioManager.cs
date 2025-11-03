using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource SFXSource;

    public AudioClip background;
    public AudioClip death;
    public AudioClip jump;
    public AudioClip run;
    public AudioClip coin;
    public AudioClip motor;
    public AudioClip finish;

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }

    public void PauseAudio()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
        if (SFXSource != null && SFXSource.isPlaying)
        {
            SFXSource.Pause();
        }
    }

    public void ResumeAudio()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
        if (SFXSource != null)
        {
            SFXSource.UnPause();
        }
    }
}

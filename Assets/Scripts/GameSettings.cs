using UnityEngine;

public static class GameSettings
{
    public static float Master { get => PlayerPrefs.GetFloat("vol_master", 1f); set => PlayerPrefs.SetFloat("vol_master", value); }
    public static float Music { get => PlayerPrefs.GetFloat("vol_music", 1f); set => PlayerPrefs.SetFloat("vol_music", value); }
    public static float Sfx { get => PlayerPrefs.GetFloat("vol_sfx", 1f); set => PlayerPrefs.SetFloat("vol_sfx", value); }
    public static int Fullscreen { get => PlayerPrefs.GetInt("fullscreen", 1); set => PlayerPrefs.SetInt("fullscreen", value); }

    public static void ApplyAudio(UnityEngine.Audio.AudioMixer mixer)
    {
        mixer.SetFloat("MasterVolume", ToDecibel(Master));
        mixer.SetFloat("MusicVolume", ToDecibel(Music));
        mixer.SetFloat("SFXVolume", ToDecibel(Sfx));
    }

    static float ToDecibel(float v) => Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;  
}
using UnityEngine;

public static class GameSettings
{
    public static float Master { get => PlayerPrefs.GetFloat("vol_master", 1f); set => PlayerPrefs.SetFloat("vol_master", value); }
    public static float Music { get => PlayerPrefs.GetFloat("vol_music", 1f); set => PlayerPrefs.SetFloat("vol_music", value); }
    public static float Sfx { get => PlayerPrefs.GetFloat("vol_sfx", 1f); set => PlayerPrefs.SetFloat("vol_sfx", value); }
    public static int Fullscreen { get => PlayerPrefs.GetInt("fullscreen", 1); set => PlayerPrefs.SetInt("fullscreen", value); }
    public static bool GunSounds { get => PlayerPrefs.GetInt("snd_gun", 1) == 1; set => PlayerPrefs.SetInt("snd_gun", value ? 1 : 0); }
    public static bool HitSounds { get => PlayerPrefs.GetInt("snd_hit", 1) == 1; set => PlayerPrefs.SetInt("snd_hit", value ? 1 : 0); }

    // Online leaderboard: players can opt out, and choose the (only) name that gets sent
    public static bool SubmitScores { get => PlayerPrefs.GetInt("submit_scores", 1) == 1; set { PlayerPrefs.SetInt("submit_scores", value ? 1 : 0); PlayerPrefs.Save(); } }
    public static int MaskIndex { get => PlayerPrefs.GetInt("mask_index", 0); set { PlayerPrefs.SetInt("mask_index", value); PlayerPrefs.Save(); } }
    public static string PlayerName { get => PlayerPrefs.GetString("player_name", ""); set { PlayerPrefs.SetString("player_name", value); PlayerPrefs.Save(); } }
    // The intro plays once, on the very first start; a setting can turn it back on
    public static bool IntroSeen { get => PlayerPrefs.GetInt("intro_seen", 0) == 1; set { PlayerPrefs.SetInt("intro_seen", value ? 1 : 0); PlayerPrefs.Save(); } }

    public static void ApplyAudio(UnityEngine.Audio.AudioMixer mixer)
    {
        mixer.SetFloat("MasterVolume", ToDecibel(Master));
        mixer.SetFloat("MusicVolume", ToDecibel(Music));
        mixer.SetFloat("SFXVolume", ToDecibel(Sfx));
    }

    static float ToDecibel(float v) => Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;  
}
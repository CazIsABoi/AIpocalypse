using UnityEngine;

public static class GameSettings
{
    public static float Master { get => PlayerPrefs.GetFloat("vol_master", 1f); set => PlayerPrefs.SetFloat("vol_master", value); }
    public static float Music { get => PlayerPrefs.GetFloat("vol_music", 1f); set => PlayerPrefs.SetFloat("vol_music", value); }
    public static float Sfx { get => PlayerPrefs.GetFloat("vol_sfx", 1f); set => PlayerPrefs.SetFloat("vol_sfx", value); }
    public static int Fullscreen { get => PlayerPrefs.GetInt("fullscreen", 1); set => PlayerPrefs.SetInt("fullscreen", value); }   // old setting: only the default for WindowMode now

    // Display. WindowMode: 0 borderless, 1 exclusive fullscreen, 2 windowed. Resolution 0x0 = the monitor's own.
    public static readonly int[] FpsCaps = { 60, 120, 144, 165, 240, -1 };   // -1 = unlimited
    public static int WindowMode { get => PlayerPrefs.GetInt("window_mode", Fullscreen != 0 ? 0 : 2); set => PlayerPrefs.SetInt("window_mode", value); }
    public static int ResolutionWidth { get => PlayerPrefs.GetInt("res_w", 0); set => PlayerPrefs.SetInt("res_w", value); }
    public static int ResolutionHeight { get => PlayerPrefs.GetInt("res_h", 0); set => PlayerPrefs.SetInt("res_h", value); }
    public static bool VSync { get => PlayerPrefs.GetInt("vsync", 0) == 1; set => PlayerPrefs.SetInt("vsync", value ? 1 : 0); }
    public static int FpsCapIndex { get => PlayerPrefs.GetInt("fps_cap", DefaultFpsCap()); set => PlayerPrefs.SetInt("fps_cap", value); }

    // Capped by default (uncapped menus ran at 400+ FPS): the first cap at or above the monitor's refresh rate
    static int DefaultFpsCap()
    {
        double hz = Screen.currentResolution.refreshRateRatio.value;
        for (int i = 0; i < FpsCaps.Length - 1; i++) if (FpsCaps[i] >= hz - 1) return i;
        return FpsCaps.Length - 2;
    }

    public static FullScreenMode ScreenMode => WindowMode switch
    {
        1 => FullScreenMode.ExclusiveFullScreen,
        2 => FullScreenMode.Windowed,
        _ => FullScreenMode.FullScreenWindow
    };

    public static void ApplyDisplay()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync ? -1 : FpsCaps[Mathf.Clamp(FpsCapIndex, 0, FpsCaps.Length - 1)];
        int w = ResolutionWidth, h = ResolutionHeight;
        if (w <= 0 || h <= 0)
        {
            float share = WindowMode == 2 ? 0.75f : 1f;   // a window as big as the monitor would hide its own title bar
            w = Mathf.RoundToInt(Screen.currentResolution.width * share);
            h = Mathf.RoundToInt(Screen.currentResolution.height * share);
        }
        Screen.SetResolution(w, h, ScreenMode);
    }

    public static bool GunSounds { get => PlayerPrefs.GetInt("snd_gun", 1) == 1; set => PlayerPrefs.SetInt("snd_gun", value ? 1 : 0); }
    public static bool HitSounds { get => PlayerPrefs.GetInt("snd_hit", 1) == 1; set => PlayerPrefs.SetInt("snd_hit", value ? 1 : 0); }
    public static bool ShowDps { get => PlayerPrefs.GetInt("show_dps", 1) == 1; set => PlayerPrefs.SetInt("show_dps", value ? 1 : 0); }
    public static float Shake { get => PlayerPrefs.GetFloat("shake", 1f); set => PlayerPrefs.SetFloat("shake", value); }   // accessibility: 0 = no screen shake

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
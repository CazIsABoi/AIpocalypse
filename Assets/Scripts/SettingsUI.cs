using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.Audio.AudioMixer mixer;
    [SerializeField] private Slider master, music, sfx;
    [SerializeField] private TMPro.TextMeshProUGUI masterText, musicText, sfxText;
    [SerializeField] private Toggle fullscreen;
    [SerializeField] private Toggle gunSounds, hitSounds;
    [SerializeField] private Toggle submitScores;
    [SerializeField] private Toggle showDps;

    [Header("Display: hook the dropdowns to OnWindowMode / OnResolution / OnFpsCap (Dynamic int), vsync to OnVSync")]
    [SerializeField] private TMP_Dropdown windowMode;   // Borderless / Fullscreen / Windowed
    [SerializeField] private TMP_Dropdown resolution;
    [SerializeField] private TMP_Dropdown fpsCap;
    [SerializeField] private Toggle vsync;
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    [SerializeField] private Slider shake;                    // optional: screen shake strength (0 = off)
    [SerializeField] private TMPro.TextMeshProUGUI shakeText;
    [SerializeField] private TMPro.TMP_Text resetText;        // the reset button's label: asks for a second press
    [SerializeField] private string resetConfirm = "Press again to erase ALL progress";

    string resetLabel;
    bool resetArmed;

    [SerializeField] private GameObject[] tabs;
    [SerializeField] private Button[] tabButtons;

    void OnEnable()
    {
        SetupDisplay();
        master.SetValueWithoutNotify(GameSettings.Master);
        music.SetValueWithoutNotify(GameSettings.Music);
        sfx.SetValueWithoutNotify(GameSettings.Sfx);
        gunSounds.SetIsOnWithoutNotify(GameSettings.GunSounds);
        hitSounds.SetIsOnWithoutNotify(GameSettings.HitSounds);
        if (fullscreen != null) fullscreen.SetIsOnWithoutNotify(GameSettings.WindowMode != 2);
        if (submitScores != null) submitScores.SetIsOnWithoutNotify(GameSettings.SubmitScores);
        if (showDps != null) showDps.SetIsOnWithoutNotify(GameSettings.ShowDps);
        if (shake != null) { shake.SetValueWithoutNotify(GameSettings.Shake); UpdateText(shake, shakeText); }

        // opening the menu again always starts with an unarmed reset button
        if (resetText != null) { if (resetLabel == null) resetLabel = resetText.text; resetText.text = resetLabel; }
        resetArmed = false;

        UpdateText(master, masterText);
        UpdateText(music, musicText);
        UpdateText(sfx, sfxText);

        for (int i = 1; i < tabs.Length; i++)
        {
            tabs[i].SetActive(false);
            tabButtons[i].interactable = true;
        }
        tabButtons[0].interactable = false;
        tabs[0].SetActive(true);
    }

    // hook these up in each slider's On Value Changed (Dynamic float)
    public void OnMaster(float v) { GameSettings.Master = v; GameSettings.ApplyAudio(mixer); UpdateText(master, masterText);}
    public void OnMusic(float v) { GameSettings.Music = v; GameSettings.ApplyAudio(mixer); UpdateText(music, musicText); }
    public void OnSfx(float v) { GameSettings.Sfx = v; GameSettings.ApplyAudio(mixer); UpdateText(sfx, sfxText); }
    public void OnFullscreen(bool on) { OnWindowMode(on ? 0 : 2); if (windowMode != null) windowMode.SetValueWithoutNotify(GameSettings.WindowMode); }

    void SetupDisplay()
    {
        if (windowMode != null)
        {
            windowMode.ClearOptions();
            windowMode.AddOptions(new List<string> { "Borderless", "Fullscreen", "Windowed" });
            windowMode.SetValueWithoutNotify(GameSettings.WindowMode);
        }
        if (resolution != null)
        {
            resolutions.Clear();
            foreach (var r in Screen.resolutions)   // one entry per refresh rate: keep each size once
            {
                var size = new Vector2Int(r.width, r.height);
                if (!resolutions.Contains(size)) resolutions.Add(size);
            }
            resolutions.Sort((a, b) => a.x != b.x ? b.x - a.x : b.y - a.y);   // biggest first
            resolution.ClearOptions();
            resolution.AddOptions(resolutions.ConvertAll(s => $"{s.x} x {s.y}"));
            int current = resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height));
            resolution.SetValueWithoutNotify(Mathf.Max(0, current));
        }
        if (fpsCap != null)
        {
            fpsCap.ClearOptions();
            fpsCap.AddOptions(new List<string>(System.Array.ConvertAll(GameSettings.FpsCaps, n => n < 0 ? "Unlimited" : $"{n} FPS")));
            fpsCap.SetValueWithoutNotify(GameSettings.FpsCapIndex);
            fpsCap.interactable = !GameSettings.VSync;   // vsync decides the frame rate itself
        }
        if (vsync != null) vsync.SetIsOnWithoutNotify(GameSettings.VSync);
    }

    public void OnWindowMode(int i)
    {
        GameSettings.WindowMode = i;
        GameSettings.ApplyDisplay();
        if (fullscreen != null) fullscreen.SetIsOnWithoutNotify(i != 2);
    }

    public void OnResolution(int i)
    {
        if (i < 0 || i >= resolutions.Count) return;
        GameSettings.ResolutionWidth = resolutions[i].x;
        GameSettings.ResolutionHeight = resolutions[i].y;
        GameSettings.ApplyDisplay();
    }

    public void OnFpsCap(int i) { GameSettings.FpsCapIndex = i; GameSettings.ApplyDisplay(); }

    public void OnVSync(bool on)
    {
        GameSettings.VSync = on;
        GameSettings.ApplyDisplay();
        if (fpsCap != null) fpsCap.interactable = !on;
    }
    public void GoToMainMenu() { GamePause.Clear(); UnityEngine.SceneManagement.SceneManager.LoadScene("Menu"); }
    public void OnGunSounds(bool on) => GameSettings.GunSounds = on;
    public void OnHitSounds(bool on) => GameSettings.HitSounds = on;
    public void OnSubmitScores(bool on) => GameSettings.SubmitScores = on;   // hook to the toggle (Dynamic bool)
    public void OnShowDps(bool on) => GameSettings.ShowDps = on;
    public void OnShake(float v) { GameSettings.Shake = v; UpdateText(shake, shakeText); }   // slider 0..1 (Dynamic float)

    // Reset button: the first press only arms it (the label asks again), the second wipes everything and reloads the menu
    public void OnResetProgress()
    {
        if (!resetArmed)
        {
            resetArmed = true;
            if (resetText != null) resetText.text = resetConfirm;
            return;
        }
        Progress.ResetEverything();
        GoToMainMenu();
    }

    private void UpdateText(Slider slider, TMPro.TextMeshProUGUI text)
    {
        if (text != null) text.text = $"{slider.value * 100:0.0}%";
    }

    public void SwitchTab(int tabIndex)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].SetActive(i == tabIndex);
            tabButtons[i].interactable = i != tabIndex;
        }
    }
}
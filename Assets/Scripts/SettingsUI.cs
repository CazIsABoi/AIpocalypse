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

    [SerializeField] private GameObject[] tabs;
    [SerializeField] private Button[] tabButtons;

    void OnEnable()
    {
        master.SetValueWithoutNotify(GameSettings.Master);
        music.SetValueWithoutNotify(GameSettings.Music);
        sfx.SetValueWithoutNotify(GameSettings.Sfx);
        gunSounds.SetIsOnWithoutNotify(GameSettings.GunSounds);
        hitSounds.SetIsOnWithoutNotify(GameSettings.HitSounds);
        fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen != 0);
        if (submitScores != null) submitScores.SetIsOnWithoutNotify(GameSettings.SubmitScores);

        UpdateText(master, masterText);
        UpdateText(music, musicText);
        UpdateText(sfx, sfxText);

        for (int i = 1; i < tabs.Length; i++)
        {
            tabs[i].SetActive(false);
            tabButtons[i].interactable = true;
        }
        tabButtons[0].interactable = false;  // first tab is active by default
        tabs[0].SetActive(true); // ensure first tab is active
    }

    // hook these up in each slider's On Value Changed (Dynamic float)
    public void OnMaster(float v) { GameSettings.Master = v; GameSettings.ApplyAudio(mixer); UpdateText(master, masterText);}
    public void OnMusic(float v) { GameSettings.Music = v; GameSettings.ApplyAudio(mixer); UpdateText(music, musicText); }
    public void OnSfx(float v) { GameSettings.Sfx = v; GameSettings.ApplyAudio(mixer); UpdateText(sfx, sfxText); }
    public void OnFullscreen(bool on) { GameSettings.Fullscreen = on ? 1 : 0; Screen.fullScreen = on; }
    public void GoToMainMenu() { GamePause.Clear(); UnityEngine.SceneManagement.SceneManager.LoadScene("Menu"); }
    public void OnGunSounds(bool on) => GameSettings.GunSounds = on;
    public void OnHitSounds(bool on) => GameSettings.HitSounds = on;
    public void OnSubmitScores(bool on) => GameSettings.SubmitScores = on;   // hook to the toggle (Dynamic bool)

    private void UpdateText(Slider slider, TMPro.TextMeshProUGUI text)
    {
        text.text = $"{slider.value * 100:0.0}%";  
    }

    // Ability to switch between tabs in the settings menu
    public void SwitchTab(int tabIndex)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].SetActive(i == tabIndex);
            tabButtons[i].interactable = i != tabIndex;
        }
    }
}
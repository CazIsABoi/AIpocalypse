using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.Audio.AudioMixer mixer;
    [SerializeField] private UnityEngine.UI.Slider master, music, sfx;
    [SerializeField] private TMPro.TextMeshProUGUI masterText, musicText, sfxText;
    [SerializeField] private UnityEngine.UI.Toggle fullscreen;

    void OnEnable()
    {
        master.SetValueWithoutNotify(GameSettings.Master);
        music.SetValueWithoutNotify(GameSettings.Music);
        sfx.SetValueWithoutNotify(GameSettings.Sfx);
        fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen != 0);

        UpdateText(master, masterText);
        UpdateText(music, musicText);
        UpdateText(sfx, sfxText);
    }

    // hook these up in each slider's On Value Changed (Dynamic float)
    public void OnMaster(float v) { GameSettings.Master = v; GameSettings.ApplyAudio(mixer); UpdateText(master, masterText);}
    public void OnMusic(float v) { GameSettings.Music = v; GameSettings.ApplyAudio(mixer); UpdateText(music, musicText); }
    public void OnSfx(float v) { GameSettings.Sfx = v; GameSettings.ApplyAudio(mixer); UpdateText(sfx, sfxText); }
    public void OnFullscreen(bool on) { GameSettings.Fullscreen = on ? 1 : 0; Screen.fullScreen = on; }
    public void GoToMainMenu() { GamePause.Clear(); UnityEngine.SceneManagement.SceneManager.LoadScene("Menu"); }

    private void UpdateText(UnityEngine.UI.Slider slider, TMPro.TextMeshProUGUI text)
    {
        text.text = $"{slider.value * 100:0.0}%";  
    }
}
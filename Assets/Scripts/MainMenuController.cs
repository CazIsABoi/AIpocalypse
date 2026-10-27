using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string MainSceneName = "MainScene";
    [SerializeField] private UnityEngine.Audio.AudioMixer mixer;

    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;
    public void StartGame()
    {
        // Load the main game scene
        SceneManager.LoadScene(MainSceneName);
    }
    private void Start()
    {
        GameSettings.ApplyAudio(mixer);
        GameSettings.ApplyDisplay();
    }

    public void ToggleCredits()
    {
        if (creditsPanel != null)
        {
            if (creditsPanel.activeSelf)
            {
                creditsPanel.SetActive(false);
            }
            else
            {
                creditsPanel.SetActive(true);
            }
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit button pressed. Exiting the game.");
        Application.Quit();
    }
}

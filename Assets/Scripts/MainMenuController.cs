using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string MainSceneName = "MainScene";

    [SerializeField] private GameObject creditsPanel;
    public void StartGame()
    {
        // Load the main game scene
        SceneManager.LoadScene(MainSceneName);
    }

    public void ShowCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
    }

    public void HideCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit button pressed. Exiting the game.");
        Application.Quit();
    }
}

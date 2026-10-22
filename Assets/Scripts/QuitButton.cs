using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Name of the main menu scene
    public void ReturnToMainMenu()
    {
        Debug.Log("Quit button pressed. Exiting the game.");
        GamePause.Clear(); // Reset time scale to normal in case it was changed
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

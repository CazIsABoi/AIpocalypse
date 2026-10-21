using UnityEngine;
using UnityEngine.EventSystems;

public class QuitButton : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Quit button pressed. Exiting the game.");
        Time.timeScale = 1f; // Reset time scale to normal in case it was changed
        Application.Quit();
    }
}

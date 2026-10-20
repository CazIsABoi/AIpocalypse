using UnityEngine;
using UnityEngine.EventSystems;

public class QuitButton : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Quit button pressed. Exiting the game.");
        Application.Quit();
    }
}

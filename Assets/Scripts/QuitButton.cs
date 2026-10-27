using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    public void ReturnToMainMenu()
    {
        RunTelemetry.Instance?.Submit("quit", 0);   // a run left mid-way (does nothing if it already ended in a death)
        MenuFlow.OpenPickerOnLoad = true;           // the menu opens on the picker, not the title
        GamePause.Clear();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

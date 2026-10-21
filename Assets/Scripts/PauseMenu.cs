using UnityEngine;
using UnityEngine.InputSystem;
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private InputActionReference pauseAction, pauseActionWebGL;
    [SerializeField] private GameObject pauseMenuUI;
    private bool isOpen;
    void Update()
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL // WebGL build
        if (!pauseActionWebGL.action.WasPressedThisFrame()) return;
#endif
# if !UNITY_WEBPLAYER && !UNITY_WEBGL // Non-WebGL build
        if (!pauseAction.action.WasPressedThisFrame()) return;
#endif
        if (isOpen) Resume();
        else if (Time.timeScale > 0f) Open();     // someone else already paused (level-up, death, floor) → ignore
    }

    public void Open()
    {
        isOpen = true;
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isOpen = false;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }
}

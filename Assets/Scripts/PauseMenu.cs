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
        else if (!GamePause.IsPaused) Open();     // someone else already paused (level-up, death, floor) → ignore
    }

    public void Open()
    {
        isOpen = true;
        pauseMenuUI.SetActive(true);
        GamePause.Add(PauseReason.PauseMenu);
    }

    public void Resume()
    {
        isOpen = false;
        pauseMenuUI.SetActive(false);
        GamePause.Remove(PauseReason.PauseMenu);
    }
}

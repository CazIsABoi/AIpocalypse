using UnityEngine;
using UnityEngine.InputSystem;
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private InputActionReference pauseAction, pauseActionWebGL;   // Esc / Start, and Tab (browsers use Esc to leave fullscreen)
    [SerializeField] private GameObject pauseMenuUI;
    private bool isOpen;

    // actions only fire while enabled
    void OnEnable()
    {
        if (pauseAction != null) pauseAction.action.Enable();
        if (pauseActionWebGL != null) pauseActionWebGL.action.Enable();
    }

    // Both keys work everywhere: a compile-time #if picked only Tab whenever the editor's build target was WebGL
    void Update()
    {
        bool pressed = (pauseAction != null && pauseAction.action.WasPressedThisFrame())
                    || (pauseActionWebGL != null && pauseActionWebGL.action.WasPressedThisFrame());
        if (!pressed) return;
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
        PlayerPrefs.Save();
        GamePause.Remove(PauseReason.PauseMenu);
    }
}

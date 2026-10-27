using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Makes every menu work with a controller without setting anything up per panel.
// The stick/d-pad can only move between buttons when one is selected, and with a mouse nothing ever is.
// So while the controller is in use this keeps a button selected: the first one (top-left) of the
// front-most menu, picked again whenever the selected one disappears or a menu opens in front of it.
// Spawns itself, nothing to add to the scenes.
[AutoStaticsCleanup]
public partial class MenuFocus : MonoBehaviour
{
    public static bool UsingGamepad { get; private set; }   // the last input came from a controller (Aim reads this too)

    static Selectable[] buffer = new Selectable[64];
    static bool runScene;   // MainScene: only focus buttons while the game is paused (level up, chest, pause, death)
    Vector2 lastMouse;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        var go = new GameObject("MenuFocus");
        DontDestroyOnLoad(go);
        go.AddComponent<MenuFocus>();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => runScene = FindAnyObjectByType<GameController>() != null;
        runScene = FindAnyObjectByType<GameController>() != null;
    }

    void Update()
    {
        var pad = Gamepad.current;
        var mouse = Mouse.current;

        // Which device was used last: any controller input switches to it, moving/clicking the mouse switches back
        if (pad != null && pad.wasUpdatedThisFrame && PadActive(pad)) SetGamepad(true);
        if (mouse != null)
        {
            Vector2 pos = mouse.position.ReadValue();
            if ((pos - lastMouse).sqrMagnitude > 25f || mouse.leftButton.wasPressedThisFrame) SetGamepad(false);
            lastMouse = pos;
        }

        if (!UsingGamepad) return;
        var events = EventSystem.current;
        if (events == null) return;

        // Mid-fight nothing may stay selected: A (south) would press a HUD button instead of the shrine
        if (runScene && !GamePause.IsPaused)
        {
            if (events.currentSelectedGameObject != null) events.SetSelectedGameObject(null);
            return;
        }

        GameObject selected = events.currentSelectedGameObject;
        Selectable current = selected != null ? selected.GetComponent<Selectable>() : null;
        Selectable best = Best(out int frontOrder);
        if (best == null) return;

        bool lost = current == null || !current.isActiveAndEnabled || !current.IsInteractable();
        bool behind = current != null && Order(current) < frontOrder;   // a menu opened in front of it
        if (lost || behind) events.SetSelectedGameObject(best.gameObject);
    }

    // Ignore stick drift: only real presses / pushes count as "using the controller"
    static bool PadActive(Gamepad pad)
    {
        if (pad.leftStick.ReadValue().sqrMagnitude > 0.09f || pad.rightStick.ReadValue().sqrMagnitude > 0.09f) return true;
        foreach (var control in pad.allControls)
            if (control is UnityEngine.InputSystem.Controls.ButtonControl b && b.wasPressedThisFrame) return true;
        return false;
    }

    void SetGamepad(bool on)
    {
        if (UsingGamepad == on) return;
        UsingGamepad = on;
        // back to the mouse: drop the controller highlight (but never kick the player out of a text field)
        var events = EventSystem.current;
        if (!on && events != null && events.currentSelectedGameObject != null
            && events.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() == null)
            events.SetSelectedGameObject(null);
    }

    // The top-left usable button of the front-most canvas
    static Selectable Best(out int frontOrder)
    {
        if (Selectable.allSelectableCount > buffer.Length) buffer = new Selectable[Selectable.allSelectableCount * 2];
        int count = Selectable.AllSelectablesNoAlloc(buffer);

        Selectable best = null;
        frontOrder = int.MinValue;
        Vector3 bestPos = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            var s = buffer[i];
            if (s == null || !s.IsInteractable() || s.navigation.mode == Navigation.Mode.None) continue;
            int order = Order(s);
            Vector3 pos = s.transform.position;
            bool better = best == null || order > frontOrder
                       || (order == frontOrder && (pos.y > bestPos.y + 1f || (Mathf.Abs(pos.y - bestPos.y) <= 1f && pos.x < bestPos.x)));
            if (better) { best = s; frontOrder = order; bestPos = pos; }
        }
        return best;
    }

    static int Order(Selectable s)
    {
        var canvas = s.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.rootCanvas.sortingOrder : 0;
    }
}

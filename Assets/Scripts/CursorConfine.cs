using UnityEngine;

public class CursorConfine : MonoBehaviour
{
    void Start() => Cursor.lockState = CursorLockMode.Confined;

    // Alt-Tab releases the cursor; put it back when the window gets focus again
    void OnApplicationFocus(bool focused)
    {
        if (focused) Cursor.lockState = CursorLockMode.Confined;
    }

    void OnDestroy() => Cursor.lockState = CursorLockMode.None;   // free again when leaving the scene (menu)
}
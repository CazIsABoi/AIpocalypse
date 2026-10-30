using UnityEngine;

public class CursorConfine : MonoBehaviour
{
    [SerializeField] private Texture2D gameCursor;   // Texture Type: Cursor. Shown while playing, the normal arrow in menus
    [SerializeField] private Vector2 hotspot = new Vector2(32, 32);   // the pixel that clicks: the middle of a 64x64 crosshair
    private bool showingGame;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        SetGameCursor(true);
    }

    // every menu in MainScene (pause, level-up, floor change, death, cutscenes) pauses through GamePause
    void Update()
    {
        bool playing = !GamePause.IsPaused;
        if (playing != showingGame) SetGameCursor(playing);
    }

    void SetGameCursor(bool on)
    {
        showingGame = on;
        Cursor.SetCursor(on ? gameCursor : null, hotspot, CursorMode.Auto);
    }

    // Alt-Tab releases the cursor; put it back when the window gets focus again
    void OnApplicationFocus(bool focused)
    {
        if (focused) Cursor.lockState = CursorLockMode.Confined;
    }

    void OnDestroy()   // leaving the scene (menu): free and the normal arrow again
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
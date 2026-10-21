using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EntityHandler spawner;
    [SerializeField] private EventDirector director;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup fade;
    public int Floor { get; private set; } = 1;
    public event System.Action<int> FloorChanged;

    [Header("Minimap")]
    [SerializeField] private Camera minimapCam;
    [SerializeField] private MinimapController minimap;
    [SerializeField, Min(1)] private int renderEvery = 3;     // 1 = every frame
    private int frame;

    void OnEnable()
    {
        dungeon.Generated += OnDungeonReady;
        playerHealth.Died += OnPlayerDied;
    } 
    void OnDisable() {
        dungeon.Generated -= OnDungeonReady;   // stop listening
        playerHealth.Died -= OnPlayerDied;
    }

    void OnDungeonReady()
    {
        spawner.ClearEnemies();
        playerHealth.Heal(playerHealth.MaxHealth * 0.25f);   // heal a bit on floor change
        player.Teleport(dungeon.StartPosition);
        director.StartLoop();
    }

    void OnPlayerDied()
    {
        GamePause.Add(PauseReason.Death);   // never removed: only a restart/menu clears it
        gameOverPanel.SetActive(true);
    }

    void Update()
    {
        frame++;
        if (minimapCam != null && minimap != null)
        {
            minimapCam.enabled = minimap.IsBig || frame % renderEvery == 0;
        }
    }
    public void NextFloor()
    {
        if (playerHealth.IsDead) return;
        director.OnFloorChanged(Floor);   // stop any running event
        StartCoroutine(NextFloorRoutine());
    }

    IEnumerator NextFloorRoutine()
    {
        GamePause.Add(PauseReason.FloorChange);                // nothing can hit the player during the switch

        while (fade.alpha < 1f)
        {
            fade.alpha += Time.unscaledDeltaTime / 0.4f;       // fade alpha 0 → 1 over ~0.4 s
            yield return null;                                  // wait for next frame
        }

        Floor++;
        FloorChanged?.Invoke(Floor);
        dungeon.SetFloor(Floor);
        dungeon.Regenerate();
        yield return new WaitUntil(() => dungeon.IsReady);     // wait for generation to finish

        // fade alpha 1 → 0 the same way
        while (fade.alpha > 0f)
        {
            fade.alpha -= Time.unscaledDeltaTime / 0.4f;       // fade alpha 1 → 0 over ~0.4 s
            yield return null;                                  // wait for next frame
        }

        GamePause.Remove(PauseReason.FloorChange);             // still paused if something else (death, level-up) is active
    }

    public void Restart()
    {
        GamePause.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EntityHandler spawner;
    [SerializeField] private GameObject gameOverPanel;

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
        player.Teleport(dungeon.StartPosition);
    }

    void OnPlayerDied()
    {
        Time.timeScale = 0f;   // pause the game
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

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

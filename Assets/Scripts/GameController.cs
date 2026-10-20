using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private EntityHandler spawner;

    [Header("Minimap")]
    [SerializeField] private Camera minimapCam;
    [SerializeField] private MinimapController minimap;
    [SerializeField, Min(1)] private int renderEvery = 3;     // 1 = every frame
    private int frame;

    void OnEnable() => dungeon.Generated += OnDungeonReady;   // start listening
    void OnDisable() => dungeon.Generated -= OnDungeonReady;   // stop listening

    void OnDungeonReady()
    {
        spawner.ClearEnemies();
        player.Teleport(dungeon.StartPosition);
    }

    void Update()
    {
        frame++;
        if (minimapCam != null && minimap != null || frame % renderEvery == 0)
        {
            minimapCam.enabled = minimap.IsBig || frame % renderEvery == 0;
        }
    }
}

using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private EntityHandler spawner;

    void OnEnable() => dungeon.Generated += OnDungeonReady;   // start listening
    void OnDisable() => dungeon.Generated -= OnDungeonReady;   // stop listening

    void OnDungeonReady()
    {
        spawner.ClearEnemies();
        player.Teleport(dungeon.StartPosition);
    }
}

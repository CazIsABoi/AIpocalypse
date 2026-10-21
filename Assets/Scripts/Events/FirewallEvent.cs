using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Firewall")]
public class FirewallEvent : GameEvent
{
    public GameObject barrierPrefab;    // on the Barrier layer, BoxCollider, no NavMeshObstacle
    public float spawnMultiplier = 2f;
    public RewardPool firewallPool;

    private List<GameObject> barriers = new List<GameObject>();
    private bool failed;

    public override void Begin(EventContext ctx)
    {
        // In a corridor? Pull the player into the nearest room first, then lock that one
        if (!ctx.Dungeon.TryGetPlayerRoomDoors(out var doors)
            && ctx.Dungeon.TryGetNearestRoomEntry(ctx.Player.position, out Vector3 entry))
        {
            ctx.Movement.Teleport(entry);
        }

        if (doors.Count > 0 || ctx.Dungeon.TryGetPlayerRoomDoors(out doors))
        {
            foreach (var door in doors)
            {
                var barrier = Instantiate(barrierPrefab, door.pos, Quaternion.identity);
                if (!door.northSouth) barrier.transform.Rotate(0, 90, 0);
                barriers.Add(barrier);
            }
            ctx.Spawner.SpawnMultiplier *= spawnMultiplier;
        }
        else
        {
            Debug.LogWarning($"Lockdown: player not in a room and no room to teleport to (player at {ctx.Player.position})");
            failed = true;
            Duration = 0f;
        }
    }

    public override bool IsDone(EventContext ctx) => failed;   

    public override void End(EventContext ctx)
    {
        foreach (var barrier in barriers)
        {
            Destroy(barrier);
        }

        if (!failed)
        {
            ctx.Rewards.ShowChest(firewallPool);
            ctx.Spawner.SpawnMultiplier /= spawnMultiplier;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

// "Lockdown": the player's room is sealed and spawns speed up. Destroy enough AIs to break out early,
// or survive until the time limit opens the doors anyway. The kill target follows the spawn rate,
// so it asks for the same thing on floor 2 as in endless: "kill what ~20 seconds of spawns bring".
[CreateAssetMenu(menuName = "Events/Firewall")]
public class FirewallEvent : GameEvent
{
    public GameObject barrierPrefab;    // on the Barrier layer, BoxCollider, no NavMeshObstacle
    public float spawnMultiplier = 2f;
    public RewardPool firewallPool;

    [Header("Break out")]
    public float timeLimit = 30f;       // the doors open by themselves after this
    public float killWindow = 20f;      // kill target = enemies that spawn in this many seconds (during lockdown)
    public int minKills = 10;           // floor 1 spawns are slow: never ask for fewer than this
    public int maxKills = 50;           // late spawn rates asked for 500 kills: the alive cap and the sealed room never deliver that many
    [Range(0f, 1f)] public float targetShare = 0.6f;   // of what the spawn rate says, since most spawns land outside the room
    public RewardPool breakoutPool;     // optional: a better chest for breaking out on kills (empty = firewallPool)

    private List<GameObject> barriers = new List<GameObject>();
    private bool failed;
    private int kills, killTarget;
    private float timeLeft;

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
            // after the multiplier, so the target matches the faster lockdown spawns
            killTarget = Mathf.Clamp(Mathf.RoundToInt(ctx.Spawner.EnemiesPerSecond * killWindow * targetShare), minKills, maxKills);
            timeLeft = timeLimit;
            CombatEvents.EnemyKilled += OnKilled;
            AudioManager.Instance?.Play(Sfx.LockdownBarrier);
        }
        else
        {
            Debug.LogWarning($"Lockdown: player not in a room and no room to teleport to (player at {ctx.Player.position})");
            failed = true;
        }
    }

    void OnKilled(Vector3 position) => kills++;

    public override void Tick(EventContext ctx) => timeLeft -= Time.deltaTime;
    public override float TimeLeft(float elapsed) => Mathf.Max(0f, timeLeft);
    public override bool IsDone(EventContext ctx) => failed || kills >= killTarget || timeLeft <= 0f;
    public override bool Started => !failed;   // couldn't lock a room → skipped without alarm/title
    public override string Status => failed ? "" : $"break out {Mathf.Min(kills, killTarget)} / {killTarget}";
    public override string Outcome => failed ? "skipped" : $"{(kills >= killTarget ? "broke" : "held")} {kills}/{killTarget} in {timeLimit - Mathf.Max(0f, timeLeft):0}s";

    public override void End(EventContext ctx)
    {
        CombatEvents.EnemyKilled -= OnKilled;   // also runs when the floor changes mid-event
        foreach (var barrier in barriers)
        {
            if (barrier == null) continue;
            if (barrier.TryGetComponent(out BarrierFX fx)) fx.FadeOut();   // fades, then destroys itself
            else Destroy(barrier);
        }

        if (!failed)
        {
            bool brokeOut = kills >= killTarget;
            ctx.Rewards.ShowChest(brokeOut && breakoutPool != null ? breakoutPool : firewallPool);
            ctx.Spawner.SpawnMultiplier /= spawnMultiplier;
        }
    }
}

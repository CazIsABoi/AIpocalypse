using UnityEngine;

[CreateAssetMenu(menuName = "Events/Bounty")]
public class BountyEvent : GameEvent
{
    public GameObject markerPrefab;
    private GameObject marker;
    public float healthMultiplier = 5f;
    public int xpMultiplier = 5;
    public float timeLimit = 45f;
    public RewardPool bountyPool;

    [Header("Minimap")]
    public Material mapIconMaterial;    // replaces the normal red enemy icon
    public float mapIconScale = 2f;
    public float mapIconHeight = 3.6f;  // above the fog (3), so it shows even in unexplored rooms

    private EnemyController bounty;
    private float timeLeft;
    private bool killed;

    public override bool Started => bounty != null;

    public override void Begin(EventContext ctx)
    {
        // An explored room the player isn't in, so it isn't hidden under fog
        if (!ctx.Dungeon.TryGetVisibleSpawnPosition(out Vector3 spawnPos))
        {
            Debug.LogWarning("No spawn position found for bounty event");
            return;   // bounty stays null → Started is false → the director skips the event
        }
        int type = ctx.Spawner.RollEnemyWeightAndIndex();
        bounty = ctx.Spawner.SpawnEnemy(type, spawnPos);
        bounty.MaxHealth *= healthMultiplier;
        bounty.Xp *= xpMultiplier;
        if (markerPrefab != null) 
        {
            marker = Instantiate(markerPrefab, bounty.transform);
            marker.transform.position = bounty.transform.position + Vector3.up * 1.5f;
            marker.transform.localScale = Vector3.one * 2.5f;
        }
        HighlightMapIcon();
        timeLeft = timeLimit;
        AudioManager.Instance?.PlayAt(Sfx.BountySpawn, spawnPos);
    }

    // Recolour the enemy's own minimap icon, make it bigger and lift it above the fog
    void HighlightMapIcon()
    {
        MapIcon icon = bounty.GetComponentInChildren<MapIcon>();
        if (icon == null) return;
        icon.WorldHeight = mapIconHeight;
        icon.transform.localScale *= mapIconScale;
        if (mapIconMaterial != null && icon.TryGetComponent(out Renderer r)) r.sharedMaterial = mapIconMaterial;
    }

    public override void Tick(EventContext ctx)
    {
        timeLeft -= Time.deltaTime;
    }
    public override float TimeLeft(float elapsed) => timeLeft;

    public override bool IsDone(EventContext ctx) => bounty == null || timeLeft <= 0;

    public override void End(EventContext ctx)
    {
        killed = bounty == null && timeLeft > 0;
        if (killed)
        {
            CameraShake.Instance?.Add(0.5f);
            AudioManager.Instance?.Play(Sfx.BountyKilled);
            ctx.Rewards.ShowChest(bountyPool);
        }
        else if (marker != null)
        {
            Destroy(marker);
        }
    }
}
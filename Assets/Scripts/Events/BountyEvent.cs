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

    private EnemyController bounty;
    private float timeLeft;
    private bool killed;

    public override void Begin(EventContext ctx)
    {
        if (!ctx.Dungeon.TryGetSpawnPosition(out Vector3 spawnPos))
        {
            Debug.LogWarning("No spawn position found for bounty event");
            return;
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
        timeLeft = timeLimit;
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
            ctx.Rewards.ShowChest(bountyPool);
        }
        else
        {
            Destroy(marker);
        }
    }
}
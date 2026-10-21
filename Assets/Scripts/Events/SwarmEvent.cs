using UnityEngine;

[CreateAssetMenu(menuName = "Events/Swarm")]
public class SwarmEvent : GameEvent
{
    public float multiplier = 3f;
    private float originalMultiplier;

    public override void Begin(EventContext ctx)
    {
        originalMultiplier = ctx.Spawner.SpawnMultiplier;
        ctx.Spawner.SpawnMultiplier = multiplier;
        ctx.Spawner.SpawnSoon();
    }

    public override void End(EventContext ctx)
    {
        ctx.Spawner.SpawnMultiplier = originalMultiplier;
    }
}
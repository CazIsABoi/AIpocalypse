using UnityEngine;

[CreateAssetMenu(menuName = "Events/Download")]
public class DownloadEvent : GameEvent
{
    public RewardPool tradeOffPool;

    public override void Begin(EventContext ctx)
    {
        ctx.Rewards.ShowChest(tradeOffPool);
    }
}
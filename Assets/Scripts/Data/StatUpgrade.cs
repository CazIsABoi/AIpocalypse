using UnityEngine;

[CreateAssetMenu(menuName = "Rewards/Stat Upgrade")]
public class StatUpgrade : RewardData
{
    public StatType Stat;
    public float Amount;

    public override void Apply(PlayerStats stats) { stats.Add(Stat, Amount); }
    public override bool CanOffer(PlayerStats stats) => Amount <= 0 || !stats.IsCapped(Stat);
    public override RewardPath GetPath() => Amount > 0 ? PathOf(Stat) : RewardPath.None;
    public override string GetPreview(PlayerStats stats)
    {
        float before = stats.Get(Stat);
        float after = stats.Preview(Stat, Amount);
        return StatNames.Change(Stat, before, after);
    }
}
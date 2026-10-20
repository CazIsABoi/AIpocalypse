using UnityEngine;

[CreateAssetMenu(menuName = "Rewards/Stat Upgrade")]
public class StatUpgrade : RewardData
{
    public StatType Stat;
    public float Amount;

    public override void Apply(PlayerStats stats) { stats.Add(Stat, Amount); }
}
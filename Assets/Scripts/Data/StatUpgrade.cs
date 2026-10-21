using UnityEngine;

[CreateAssetMenu(menuName = "Rewards/Stat Upgrade")]
public class StatUpgrade : RewardData
{
    public StatType Stat;
    public float Amount;

    public override void Apply(PlayerStats stats) { stats.Add(Stat, Amount); }
    public override string GetPreview(PlayerStats stats)
    {
        float before = stats.Get(Stat);
        float after = stats.Preview(Stat, Amount);
        return $"{before:0.#} -> {after:0.#}";
    }
}
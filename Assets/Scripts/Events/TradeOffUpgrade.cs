using UnityEngine;

[CreateAssetMenu(menuName = "Rewards/Trade Off")]
public class TradeOffUpgrade : RewardData
{
    [System.Serializable]
    public struct StatChange { public StatType Stat; public float Amount; }

    public StatChange[] Changes;        // e.g. +0.3 DamagePercent, -20 MaxHealth

    public override void Apply(PlayerStats stats)
    {
        foreach (var change in Changes)
        {
            stats.Add(change.Stat, change.Amount);
        }
    }

    public override string GetPreview(PlayerStats stats)
    {
        string preview = "";
        foreach (var change in Changes)
        {
            float before = stats.Get(change.Stat);
            float after = stats.Preview(change.Stat, change.Amount);
            preview += $"{StatNames.Get(change.Stat)}: {StatNames.Format(change.Stat, before)} -> {StatNames.Format(change.Stat, after)}\n";
        }
        return preview.TrimEnd('\n');
    }
}
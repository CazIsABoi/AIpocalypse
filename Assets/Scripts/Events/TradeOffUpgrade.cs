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

    // Path = the one path its upsides feed (melee-only or gun-only); mixed or neither = no tag
    public override RewardPath GetPath()
    {
        RewardPath path = RewardPath.None;
        foreach (var change in Changes)
        {
            if (change.Amount <= 0) continue;
            RewardPath p = PathOf(change.Stat);
            if (p == RewardPath.None) continue;
            if (path != RewardPath.None && path != p) return RewardPath.None;
            path = p;
        }
        return path;
    }

    // Worth offering as long as at least one of its upsides isn't capped
    public override bool CanOffer(PlayerStats stats)
    {
        foreach (var change in Changes)
            if (change.Amount > 0 && !stats.IsCapped(change.Stat)) return true;
        return false;
    }

    public override string GetPreview(PlayerStats stats)
    {
        string preview = "";
        foreach (var change in Changes)
        {
            float before = stats.Get(change.Stat);
            float after = stats.Preview(change.Stat, change.Amount);
            preview += $"{StatNames.Get(change.Stat)}: {StatNames.Change(change.Stat, before, after)}\n";
        }
        return preview.TrimEnd('\n');
    }
}
using UnityEngine;

[CreateAssetMenu(menuName = "Rewards/Stat Upgrade")]
public class StatUpgrade : RewardData
{
    public StatType Stat;
    public float Amount;

    public override void Apply(PlayerStats stats) { stats.Add(Stat, Amount); }
    public override bool CanOffer(PlayerStats stats) => Amount <= 0 || !stats.IsCapped(Stat);
    public override RewardPath GetPath() => Amount > 0 ? PathOf(Stat) : RewardPath.None;
    public override string Line => StatNames.Get(Stat);
    const float diminishedBelow = 0.75f;   // less than this share of Amount arrives: show the real gain
    const float rareBelow = 0.25f;         // less than this: almost pointless, offer it less often
    const float rareScale = 0.25f;

    public override string GetDescription(PlayerStats stats)
    {
        if (stats.Efficiency(Stat, Amount) >= diminishedBelow) return Description;
        float before = stats.Get(Stat);
        float gain = stats.Preview(Stat, Amount) - before;
        string real = Stat switch
        {
            StatType.MoveSpeedPercent => $"+{gain / before * 100f:0}% move speed",
            StatType.MoveSpeed => $"+{gain:0.#} move speed",
            _ => $"+{gain:0.#} {StatNames.Get(Stat).ToLower()}"
        };
        return $"{real}\n<size=70%><color=#FFA040>DIMINISHED</color></size>";
    }

    public override float OfferScale(PlayerStats stats) => stats.Efficiency(Stat, Amount) < rareBelow ? rareScale : 1f;

    public override string GetPreview(PlayerStats stats)
    {
        float before = stats.Get(Stat);
        float after = stats.Preview(Stat, Amount);
        return StatNames.Change(Stat, before, after);
    }
}
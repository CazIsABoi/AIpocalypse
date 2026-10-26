using UnityEngine;

// A card that gives (or levels up) an on-hit / on-kill effect instead of a stat.
[CreateAssetMenu(menuName = "Rewards/Effect Upgrade")]
public class EffectUpgrade : RewardData
{
    public EffectType Effect;
    public int MaxLevel = 3;
    public RewardPath Path = RewardPath.None;

    public override void Apply(PlayerStats stats) => Effects(stats).Add(Effect, 1);

    public override bool CanOffer(PlayerStats stats) => Effects(stats).Level(Effect) < MaxLevel;

    public override RewardPath GetPath() => Path;

    public override string GetPreview(PlayerStats stats)
    {
        int level = Effects(stats).Level(Effect);
        return level == 0 ? $"<color={StatNames.Good}>NEW</color>" : $"Level {level} -> <color={StatNames.Good}>{level + 1}</color>";
    }

    static PlayerEffects Effects(PlayerStats stats)
    {
        var effects = stats.GetComponent<PlayerEffects>();
        return effects != null ? effects : stats.gameObject.AddComponent<PlayerEffects>();
    }
}

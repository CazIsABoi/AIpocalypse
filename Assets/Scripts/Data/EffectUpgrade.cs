using UnityEngine;

// A card that gives (or levels up) an on-hit / on-kill effect instead of a stat.
[CreateAssetMenu(menuName = "Rewards/Effect Upgrade")]
public class EffectUpgrade : RewardData
{
    public EffectType Effect;
    public int MaxLevel = 3;
    public RewardPath Path = RewardPath.None;
    public string[] LevelUps;   // what each level after the first adds: [0] = level 2, [1] = level 3

    public override void Apply(PlayerStats stats) => Effects(stats).Add(Effect, 1);

    public override bool CanOffer(PlayerStats stats) => Effects(stats).Level(Effect) < MaxLevel;

    public override RewardPath GetPath() => Path;
    public override string Line => Effect.ToString();

    public override string GetPreview(PlayerStats stats)
    {
        int level = Effects(stats).Level(Effect);
        if (level == 0) return $"<color={StatNames.Good}>NEW</color>";
        string change = $"Lv {level} -> <color={StatNames.Good}>{level + 1}</color>";
        int i = level - 1;
        return LevelUps != null && i < LevelUps.Length && !string.IsNullOrEmpty(LevelUps[i])
            ? $"{change}\n<size=75%><color={StatNames.Good}>{LevelUps[i]}</color></size>"
            : change;
    }

    static PlayerEffects Effects(PlayerStats stats)
    {
        var effects = stats.GetComponent<PlayerEffects>();
        return effects != null ? effects : stats.gameObject.AddComponent<PlayerEffects>();
    }
}

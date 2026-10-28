using UnityEngine;

// What an achievement watches. Counters add up (kills, damage); "best" stats keep the highest value reached.
// Assets store this as numbers: only ever add new values at the END.
public enum AchievementStat { Kills, MeleeKills, GunKills, EffectKills, GunDamage, MeleeDamage, BiggestHit, AttackSpeed, CritChance, Floor, BossesDefeated, Runs }

public enum AchievementScope { Lifetime, SingleRun }   // across every run, or within one run

// One achievement: "reach Target in Stat", optionally rewarding an Idea (e.g. Blademaster for 1000 melee kills).
[CreateAssetMenu(menuName = "Rewards/Achievement")]
public class AchievementData : ScriptableObject
{
    public string Id;                   // save key AND the Steam API name (Steamworks > Stats & Achievements): never change after release
    public string Title;
    [TextArea] public string Description;
    public Sprite Icon;
    public AchievementStat Stat;
    public AchievementScope Scope = AchievementScope.Lifetime;
    public float Target;
    public IdeaData Reward;             // optional: unlocked for free (mark that Idea "Unlocked By Achievement")

    // "Best" stats are a high score, not a sum: lifetime = the best any run reached
    public static bool IsBest(AchievementStat stat) =>
        stat == AchievementStat.BiggestHit || stat == AchievementStat.AttackSpeed ||
        stat == AchievementStat.CritChance || stat == AchievementStat.Floor;
}

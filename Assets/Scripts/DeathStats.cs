using System.Collections.Generic;
using TMPro;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
[AutoStaticsCleanup]
partial class DeathStats : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private TMP_Text buildText;
    [SerializeField] private RunClock clock;
    [SerializeField] private GameController game;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private EntityHandler spawner;

    [SerializeField] private RecallSequence recall;   // Thoughts count-up, recall bar, Idea pick

    // Shown as real per-hit values below instead of raw multipliers
    static readonly HashSet<StatType> replaced = new HashSet<StatType>
    {
        StatType.Damage, StatType.DamagePercent, StatType.CritDamage,
        StatType.MeleeDamagePercent, StatType.GunDamagePercent
    };

    void OnEnable()
    {
        string build =
            $"Gun Damage<pos=60%>{StatNames.Compact(stats.GunHit)}\n" +
            $"Gun Crit<pos=60%>{StatNames.Compact(stats.GunCritHit)}\n" +
            $"Melee Damage<pos=60%>{StatNames.Compact(stats.MeleeHit)}\n" +
            $"Melee Crit<pos=60%>{StatNames.Compact(stats.MeleeCritHit)}\n";

        var shown = new HashSet<string>();
        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
        {
            if (replaced.Contains(type)) continue;
            if ((type == StatType.Armor || type == StatType.Thorns || type == StatType.BlockChance) && stats.Get(type) <= 0f) continue;   // only once picked
            string name = StatNames.Get(type);
            if (!shown.Add(name)) continue;   // MoveSpeed + MoveSpeedPercent are both "Speed"
            build += $"{name}<pos=60%>{StatNames.Format(type, stats.Get(type))}\n";
        }
        buildText.text = build;

        int m = (int)(clock.Elapsed / 60), s = (int)(clock.Elapsed % 60);
        int runScore = ScoreService.Score(clock.Elapsed, game.Floor, spawner.Kills);
        text.text = $"SCORE  {runScore}\nTIME SURVIVED  {m:00}:{s:00}\nFLOOR  {game.Floor}\nLEVEL  {stats.Level}\nAIs DESTROYED  {spawner.Kills}";

        int best = PlayerPrefs.GetInt("best_score", 0);
        if (best > 0 && runScore > best) text.text += "\nNEW BEST!";
        if (runScore > best) { PlayerPrefs.SetInt("best_score", runScore); PlayerPrefs.Save(); }

        // once per run: Thoughts, telemetry, then the recall sequence
        if (!submitted)
        {
            submitted = true;
            // square root = diminishing returns: a 2 min run ~25, a 15 min boss run ~85, a 30 min endless run ~185
            float score = clock.Elapsed * 0.1f + spawner.Kills * 0.5f + game.Floor * 2f;
            int thoughtsEarned = Mathf.RoundToInt(Mathf.Sqrt(score) * 5f);
            Progress.AddThoughts(thoughtsEarned);   // save first...
            RunTelemetry.Instance?.Submit("death", thoughtsEarned);   // leaderboard + telemetry sheet
            recall.Play(thoughtsEarned);            // ...show second (count-up, bar, maybe a recall)
        }
    }
    private bool submitted;
}

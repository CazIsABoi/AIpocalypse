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
            string name = StatNames.Get(type);
            if (!shown.Add(name)) continue;   // MoveSpeed + MoveSpeedPercent are both "Speed"
            build += $"{name}<pos=60%>{StatNames.Format(type, stats.Get(type))}\n";
        }
        buildText.text = build;

        int m = (int)(clock.Elapsed / 60), s = (int)(clock.Elapsed % 60);
        text.text = $"TIME SURVIVED  {m:00}:{s:00}\nFLOOR  {game.Floor}\nLEVEL  {stats.Level}\nAIs DESTROYED  {spawner.Kills}";

        float best = PlayerPrefs.GetFloat("best_time", 0f);
        if (best > 0f && clock.Elapsed > best) text.text += "\nNEW BEST!";
        if (clock.Elapsed > best) { PlayerPrefs.SetFloat("best_time", clock.Elapsed); PlayerPrefs.Save(); }
    }
}

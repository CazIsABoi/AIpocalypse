using UnityEngine;

// On the Player in MainScene. Counts this run's achievement stats (kills by source, damage, biggest hit...),
// unlocks achievements the moment they're reached (popup + Idea reward), and adds the run to the
// lifetime totals when the scene closes (death -> menu, quit, or closing the game).
public class AchievementTracker : MonoBehaviour
{
    [SerializeField] private AchievementSet achievements;   // Data/Achievements/AllAchievements
    [SerializeField] private float checkEvery = 0.25f;      // seconds between checks (unscaled: also while paused)

    readonly float[] run = new float[System.Enum.GetValues(typeof(AchievementStat)).Length];
    PlayerStats stats;
    GameController game;
    RunClock clock;
    float nextCheck;
    bool committed;

    void Awake() => stats = GetComponent<PlayerStats>();

    void Start()
    {
        game = FindAnyObjectByType<GameController>();
        clock = FindAnyObjectByType<RunClock>();
    }

    void OnEnable()
    {
        CombatEvents.EnemyHit += OnHit;
        CombatEvents.EnemyKilledBy += OnKilled;
    }

    void OnDisable()
    {
        CombatEvents.EnemyHit -= OnHit;
        CombatEvents.EnemyKilledBy -= OnKilled;
    }

    void OnHit(EnemyController enemy, float damage, HitSource source)
    {
        if (source == HitSource.Gun) run[(int)AchievementStat.GunDamage] += damage;
        else if (source == HitSource.Melee) run[(int)AchievementStat.MeleeDamage] += damage;
        Best(AchievementStat.BiggestHit, damage);
    }

    void OnKilled(HitSource source)
    {
        run[(int)AchievementStat.Kills]++;
        switch (source)
        {
            case HitSource.Melee: run[(int)AchievementStat.MeleeKills]++; break;
            case HitSource.Gun: run[(int)AchievementStat.GunKills]++; break;
            default: run[(int)AchievementStat.EffectKills]++; break;
        }
    }

    void Best(AchievementStat stat, float value) => run[(int)stat] = Mathf.Max(run[(int)stat], value);

    void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + checkEvery;

        // values that are simply read from the game instead of counted
        if (stats != null)
        {
            Best(AchievementStat.AttackSpeed, stats.AttackSpeed);
            Best(AchievementStat.CritChance, stats.CritChance);
        }
        if (game != null) Best(AchievementStat.Floor, game.Floor);
        if (clock != null && clock.Endless) run[(int)AchievementStat.BossesDefeated] = 1;   // endless only starts after the boss

        Check();
    }

    void Check()
    {
        if (achievements == null) return;
        foreach (var a in achievements.Achievements)
        {
            if (a == null || AchievementProgress.IsUnlocked(a)) continue;
            if (AchievementProgress.Value(a, run) >= a.Target && AchievementProgress.Unlock(a))
            {
                AchievementPopup.Show(a);
                RunTelemetry.Instance?.AchievementUnlocked(a.Id);
            }
        }
    }

    // OnDestroy runs on every way out of the scene, so the run is added exactly once
    void OnDestroy()
    {
        if (committed) return;
        committed = true;
        run[(int)AchievementStat.Runs] = 1;
        Check();   // e.g. "play 10 runs" can only be reached now (no popup: the scene is closing)
        AchievementProgress.AddRun(run);
    }
}

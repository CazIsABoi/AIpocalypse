using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[AutoStaticsCleanup]
partial class RunTelemetry : MonoBehaviour
{
    public static RunTelemetry Instance { get; private set; }

    GameController game;
    RunClock clock;
    EntityHandler spawner;
    PlayerStats stats;
    PlayerHealth health;

    bool submitted, bossReached;
    float damageGun, damageMelee, damageTaken, healed, healedOrbs, lowestHp = 1f;
    int hitsTaken, chests, healthOrbs;
    float thornsDamage, blockedDamage, armorPrevented;
    int blocks, professionXp;
    int rerolls, skips;
    readonly List<string> banished = new List<string>();
    bool usedGamepad;
    readonly List<string> eventLog = new List<string>();
    readonly List<string> achievements = new List<string>();
    float floorStart;
    readonly List<string> floorTimes = new List<string>();
    readonly List<string> picked = new List<string>();
    readonly List<string> offered = new List<string>();
    readonly List<string> pickSources = new List<string>();

    // FPS: overall average and the worst full second
    int frames, secondFrames;
    float frameTime, secondTime, worstFps = 9999f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;   // -= first: never subscribed twice
        SceneManager.sceneLoaded += OnSceneLoaded;
        Spawn();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Spawn();

    static void Spawn()
    {
        if (Instance == null && FindAnyObjectByType<GameController>() != null)
            new GameObject("RunTelemetry").AddComponent<RunTelemetry>();
    }

    void Awake()
    {
        Instance = this;
        game = FindAnyObjectByType<GameController>();
        clock = FindAnyObjectByType<RunClock>();
        spawner = FindAnyObjectByType<EntityHandler>();
        stats = FindAnyObjectByType<PlayerStats>();
        if (stats != null) health = stats.GetComponent<PlayerHealth>();
        floorStart = Time.time;   // Time.time counts from app start, not from this run
        DpsMeter.Reset();
    }

    void OnEnable()
    {
        CombatEvents.EnemyHit += OnEnemyHit;
        if (health != null) { health.Damaged += OnDamaged; health.Healed += OnHealed; }
        if (game != null) game.FloorChanged += OnFloorChanged;
        if (clock != null) clock.BossTime += OnBossTime;
        Application.quitting += OnQuitting;
    }

    void OnDisable()
    {
        CombatEvents.EnemyHit -= OnEnemyHit;
        if (health != null) { health.Damaged -= OnDamaged; health.Healed -= OnHealed; }
        if (game != null) game.FloorChanged -= OnFloorChanged;
        if (clock != null) clock.BossTime -= OnBossTime;
        Application.quitting -= OnQuitting;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        frames++; frameTime += dt;
        secondFrames++; secondTime += dt;
        if (secondTime >= 1f)
        {
            worstFps = Mathf.Min(worstFps, secondFrames / secondTime);
            secondFrames = 0; secondTime = 0f;
        }
        if (!usedGamepad && UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.wasUpdatedThisFrame) usedGamepad = true;
        if (health != null && stats != null && stats.MaxHealth > 0f)
            lowestHp = Mathf.Min(lowestHp, Mathf.Clamp01(health.Health / stats.MaxHealth));
    }

    // ---------- things other scripts report ----------
    public void CardPicked(RewardData card, List<RewardData> shown, string source)
    {
        // Ids, not Titles: renaming a card must not split its history in the sheet
        picked.Add(card.Id);
        var names = new List<string>();
        foreach (var r in shown) if (r != null) names.Add(r.Id);
        offered.Add(string.Join(",", names));
        pickSources.Add(source);
    }

    public void ChestOpened() => chests++;
    public void CardsRerolled() => rerolls++;
    public void CardsSkipped() => skips++;
    public void LineBanished(string line) => banished.Add(line);

    public void ThornsDealt(float amount) => thornsDamage += amount;
    public void HitBlocked(float amount) { blocks++; blockedDamage += amount; }
    public void ArmorPrevented(float amount) => armorPrevented += amount;
    public void ProfessionXp(int xp) => professionXp = xp;
    public void AchievementUnlocked(string id) => achievements.Add(id);

    // "3:Lockdown:broke 34/40" = floor 3, the event, how it ended
    public void EventEnded(GameEvent e, bool interrupted)
    {
        string outcome = interrupted ? "left floor" : e.Outcome;
        eventLog.Add($"{(game != null ? game.Floor : 0)}:{e.Title}" + (outcome != "" ? ":" + outcome : ""));
    }

    public void HealthOrbCollected(float amount) { healthOrbs++; healedOrbs += amount; }

    void OnEnemyHit(EnemyController enemy, float damage, HitSource source)
    {
        if (source == HitSource.Gun) damageGun += damage;
        else if (source == HitSource.Melee) damageMelee += damage;
    }

    void OnDamaged(float amount) { damageTaken += amount; hitsTaken++; }
    void OnHealed(float amount) => healed += amount;
    void OnBossTime() => bossReached = true;

    void OnFloorChanged(int floor)
    {
        floorTimes.Add(Mathf.RoundToInt(Time.time - floorStart).ToString());
        floorStart = Time.time;
    }

    void OnQuitting() => Submit("closed", 0);

    // A Google Sheets cell holds at most 50,000 characters: long runs keep the end of the list
    static string Fit(string s) => s.Length <= 45000 ? s : "..." + s.Substring(s.Length - 45000);

    // ---------- the one send ----------
    // endReason: "death", "quit" (back to menu), "closed" (window closed mid-run)
    public void Submit(string endReason, int thoughtsEarned)
    {
        if (submitted || stats == null) return;
        submitted = true;
        if (endReason == "death") ProfessionApplier.AwardNow();

        int runNumber = PlayerPrefs.GetInt("run_count", 0) + 1;
        PlayerPrefs.SetInt("run_count", runNumber);
        PlayerPrefs.Save();

        var times = new List<string>(floorTimes) { Mathf.RoundToInt(Time.time - floorStart).ToString() };   // + the floor it ended on
        float elapsed = clock != null ? clock.Elapsed : Time.time;
        var effects = stats.GetComponent<PlayerEffects>();   // added mid-run by the first effect card, so look now

        ScoreService.Submit(new ScoreService.Run
        {
            // the original columns (same names, so the old sheet keeps working)
            name = GameSettings.PlayerName,
            seconds = elapsed, floor = game != null ? game.Floor : 0, level = stats.Level,
            kills = spawner != null ? spawner.Kills : 0, threat = spawner != null ? spawner.Threat : 1f,
            gunHit = stats.GunHit, meleeHit = stats.MeleeHit,
            killer = health != null ? health.LastHitBy : "",
            coreFound = FindAnyObjectByType<Core>() is Core c && c.Discovered ? 1 : 0,
            floorTime = game != null ? Time.time - game.FloorStartedAt : 0f,
            score = ScoreService.Score(elapsed, game != null ? game.Floor : 0, spawner != null ? spawner.Kills : 0),

            // who & which run
            playerId = PlayerId, runNumber = runNumber, endReason = endReason,

            // outcome
            bossReached = bossReached ? 1 : 0, bossWon = clock != null && clock.Endless ? 1 : 0,
            endlessSeconds = clock != null ? clock.EndlessTime : 0f,
            floorTimes = string.Join("|", times),

            // combat
            damageGun = damageGun, damageMelee = damageMelee, damageTaken = damageTaken, hitsTaken = hitsTaken,
            dpsPeak = DpsMeter.Peak, dpsEnd = DpsMeter.LastMinute,
            healed = healed, healedOrbs = healedOrbs, lowestHpPct = Mathf.RoundToInt(lowestHp * 100f),
            chests = chests, healthOrbs = healthOrbs,

            // build
            mask = GameSettings.MaskIndex,
            cardsPicked = Fit(string.Join("|", picked)), cardsOffered = Fit(string.Join("|", offered)),
            pickSources = Fit(string.Join("|", pickSources)), effects = effects != null ? effects.Describe() : "",
            maxHp = stats.MaxHealth, damage = stats.Damage, attackSpeed = stats.AttackSpeed,
            critChance = stats.CritChance, critDamage = stats.CritDamage, lifesteal = stats.Lifesteal,
            moveSpeed = stats.Speed, bullets = stats.BulletAmount, pierce = stats.BulletPierce,
            regen = stats.regenAmount, pickupRange = stats.pickupRange, projectileSpeed = stats.ProjectileSpeed,
            meleeRange = stats.MeleeRadius,
            moveSpeedRaw = stats.RawSpeed, pickupRangeRaw = stats.pickupRange, effectivePickup = stats.PickupRange,
            armor = stats.Armor, thorns = stats.Thorns, blockChance = stats.BlockChance,
            thornsDamage = thornsDamage, blocks = blocks, blockedDamage = blockedDamage, armorPrevented = armorPrevented,
            events = Fit(string.Join("|", eventLog)), achievements = string.Join("|", achievements),
            profession = ProfessionApplier.Current != null ? ProfessionApplier.Current.Id : "",
            professionLevel = ProfessionApplier.Current != null ? ProfessionProgress.Level(ProfessionApplier.Current) : 0,
            professionXp = professionXp,
            rerolls = rerolls, skips = skips, banished = string.Join("|", banished),

            // meta-progression (before this run's recall: that one is sent as its own "recall" event)
            thoughtsEarned = thoughtsEarned, thoughtsTotal = Progress.Thoughts, recalls = Progress.Recalls,
            ideas = Progress.UnlockedIdeaList, loadout = RunLoadout.Describe,

            // performance & setup
            avgFps = frameTime > 0f ? frames / frameTime : 0f, worstFps = worstFps > 9000f ? 0f : worstFps,
            resolution = $"{Screen.width}x{Screen.height}", fullscreen = Screen.fullScreen ? 1 : 0,
            gpu = SystemInfo.graphicsDeviceName, ramMb = SystemInfo.systemMemorySize,
            gunSounds = GameSettings.GunSounds ? 1 : 0, hitSounds = GameSettings.HitSounds ? 1 : 0,
            gamepad = usedGamepad ? 1 : 0,
        });
    }

    // Random id per install: tells returning players apart without knowing who anyone is
    public static string PlayerId
    {
        get
        {
            string id = PlayerPrefs.GetString("player_id", "");
            if (id == "")
            {
                id = System.Guid.NewGuid().ToString("N").Substring(0, 12);
                PlayerPrefs.SetString("player_id", id);
                PlayerPrefs.Save();
            }
            return id;
        }
    }
}

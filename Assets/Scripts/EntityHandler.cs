using System.Collections.Generic;
using UnityEngine;

public class EntityHandler : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private RunClock clock;
    [SerializeField] private EnemyData[] enemies = new EnemyData[0];

    private float GameTime => clock.Elapsed;
    private float SpawnCD;
    [SerializeField] private float CurrentSpawnCD;
    [SerializeField] private int MinSpawnCount;
    [SerializeField] private int MaxSpawnCount;
    [SerializeField] private List<GameObject> spawnedEnemies = new List<GameObject>();
    public float SpawnMultiplier = 1f;
    public int Kills { get; private set; }
    public void RegisterKill() => Kills++;

    [Header("Grace period (new players)")]
    [SerializeField] private float[] graceSpawnRate = { 0.5f, 0.75f };  // spawn speed on floor 1, floor 2 (1 = normal)
    [SerializeField] private float[] graceDamage = { 0.6f, 0.8f };      // enemy damage on floor 1, floor 2
    private GameController game;

    [Header("Enemy HP scaling")]
    [SerializeField] private float preBossHealthGrowth = 1.0042f;  // HP x this per second until the boss (~x40 at 15:00)

    [Header("Endless (after the boss)")]
    [SerializeField] private float endlessHealthGrowth = 1.08f;   // enemy HP x this per minute...
    [SerializeField] private float endlessHealthAccel = 0.003f;   // ...and the growth itself speeds up: late minutes scale much harder
    [SerializeField] private float endlessDamageGrowth = 1.13f;   // enemy damage x this per minute: danger leads, sponginess trails
    [SerializeField] private float eliteInterval = 150f;          // seconds between elites
    [SerializeField] private float eliteHealth = 3f, eliteDamage = 2f, eliteXp = 3f, eliteSize = 1.35f;
    [SerializeField] private Color eliteTint = new Color(1f, 0.35f, 0.3f, 1f);
    [SerializeField] private Material eliteMapIcon;               // optional: e.g. the bounty's yellow icon
    private float eliteTimer;

    float EndlessMinutes => clock.EndlessTime / 60f;
    // How much harder enemies hit than when endless began (1.0 = just started). Shown as THREAT on the timer.
    public float Threat => clock.Endless ? Mathf.Pow(endlessDamageGrowth, EndlessMinutes) : 1f;

    void Start()
    {
        game = FindAnyObjectByType<GameController>();
    }

    // 1 on floor 3+ and in endless; lower on the first floors so new players get a gentler start
    float Grace(float[] perFloor)
    {
        if (game == null || clock.Endless) return 1f;
        int i = game.Floor - 1;
        return i >= 0 && i < perFloor.Length ? perFloor[i] : 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (!dungeon.IsReady) return;
        SpawnCD -= Time.deltaTime;

        if (clock.Endless)
        {
            eliteTimer += Time.deltaTime;
            if (eliteTimer >= eliteInterval) { eliteTimer = 0f; SpawnElite(); }
        }

        if (clock.Endless)
        {
            float minutes = clock.EndlessTime / 60f;
            CurrentSpawnCD = Mathf.Max(0.5f, 2f / (1f + 0.1f * minutes));
            MaxSpawnCount = Mathf.Min(10, 1 + (int)(minutes / 2f)); // Max spawn count increases by 1 every 2 minutes, up to 10
        }
        else if (GameTime <= 600)
        {
            CurrentSpawnCD = Mathf.Lerp(4f, 2f, GameTime / 600f);  // goes from 4 to 2 seconds
            MinSpawnCount = (int)Mathf.Lerp(1f, 3f, GameTime / 600f); // Minimum spawn amount goes from 1 to 3, over the first 10 minutes
            MaxSpawnCount = (int)Mathf.Lerp(1f, 6f, GameTime / 600f);   // Max from 1 to 6 enemies spawned per wave. At 10 minute, it can spawn 6 enemies every 2 seconds.
        }
        else
        {
            CurrentSpawnCD = 2;
            MinSpawnCount = 3;
            MaxSpawnCount = 6;
        }

        int CurrentWeightTableIndex = 0;
        if (GameTime <= 600)
        {
            CurrentWeightTableIndex = (int)(GameTime / 30f);
        }
        else CurrentWeightTableIndex = 20;
        // Debug.Log("Current Index: " + CurrentWeightTableIndex);

        if(SpawnCD <= 0 && spawnedEnemies.Count < 100)
        {
            SpawnCD = CurrentSpawnCD / (SpawnMultiplier * Grace(graceSpawnRate));
            int RollQuantity = Random.Range(MinSpawnCount, MaxSpawnCount + 1);

            for (int i = 0; i < RollQuantity; i++)
            {
                int rolledEnemy = RollEnemyWeightAndIndex();

                if (!dungeon.TryGetSpawnPosition(out Vector3 spawnPos)) return;   // no spot found this time, try next cooldown

                SpawnEnemy(rolledEnemy, spawnPos);
            }
        }
        else if(spawnedEnemies.Count >= 100)
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] != null) spawnedEnemies[i].GetComponent<EnemyController>().SpeedBoostIncrease = true;
                else UpdateEnemyList();
            }
        }
    }

    public int RollEnemyWeightAndIndex()
    {
        int CurrentWeightTableIndex = 0;
        if (GameTime <= 600)
        {
            CurrentWeightTableIndex = (int)(GameTime / 30f);
        }
        else CurrentWeightTableIndex = 20;

        int TotalWeight = 0;
        for(int i = 0; i < enemies.Length; i++)
        {
            TotalWeight += enemies[i].SpawnWeights[CurrentWeightTableIndex];
        }
        int RolledWeight = Random.Range(0, TotalWeight);
        int CurrentWeight = 0;
        int PickedEnemyIndex = 0;
        for(int i = 0; i < enemies.Length; i++)
        {
            if (RolledWeight >= CurrentWeight && RolledWeight < CurrentWeight + enemies[i].SpawnWeights[CurrentWeightTableIndex])
            {
                // Debug.Log("Rolled This one");
                PickedEnemyIndex = i; break;
            }
            else CurrentWeight += enemies[i].SpawnWeights[CurrentWeightTableIndex];
        }

        return PickedEnemyIndex;
    }

    public void UpdateEnemyList()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
                continue;
            }
        }
    }

    public void ClearEnemies()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
    public EnemyController SpawnEnemy(int index, Vector3 pos)
    {
        GameObject spawnedEnemy = Instantiate(enemies[index].EnemyPrefab, pos, Quaternion.identity, transform);
        EnemyController enemy = spawnedEnemy.GetComponent<EnemyController>();
        enemy.handler = this;
        spawnedEnemies.Add(spawnedEnemy);
        UpdateEnemyList();
        enemy.MaxHealth = enemies[index].BaseHealth * HealthMultiplier();
        enemy.Xp = enemies[index].XpValue;
        enemy.Damage = enemies[index].BaseDamage * DamageMultiplier() * Grace(graceDamage);
        AudioManager.Instance?.PlayAt(Sfx.EnemySpawn, pos);   // 3D + long min interval in the AudioManager, so waves don't spam
        return enemy;
    }
    // A tougher, bigger, red-tinted enemy every few minutes in endless: variety instead of only bigger numbers
    void SpawnElite()
    {
        if (!dungeon.TryGetSpawnPosition(out Vector3 pos)) return;
        EnemyController e = SpawnEnemy(RollEnemyWeightAndIndex(), pos);
        e.IsElite = true;
        e.MaxHealth *= eliteHealth;
        e.Damage *= eliteDamage;
        e.Xp = Mathf.RoundToInt(e.Xp * eliteXp);
        e.transform.localScale *= eliteSize;

        var block = new MaterialPropertyBlock();
        foreach (Renderer r in e.GetComponentsInChildren<Renderer>())
        {
            MapIcon icon = r.GetComponent<MapIcon>();
            if (icon != null)
            {
                if (eliteMapIcon != null) r.sharedMaterial = eliteMapIcon;
                continue;
            }
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", eliteTint);   // URP Lit/Unlit tint
            r.SetPropertyBlock(block);
        }
        AudioManager.Instance?.PlayAt(Sfx.BountySpawn, pos);
    }

    public void SpawnSoon() 
    {
        SpawnCD = 0f;
    }
    float DamageMultiplier()
    {
        if (clock.Endless)
            return 3f * Mathf.Pow(endlessDamageGrowth, EndlessMinutes);   // continues from the pre-boss x3

        if (GameTime < 20)
        {
            return 1f;
        }
        else if (GameTime < 720)
        {
            return Mathf.Pow(1.0016f, GameTime - 20f); // 0.33
        }
        else if (GameTime >= 720)
        {
            return 3f;    //10f
        }
        return 1f;
    }

    // HP keeps growing all the way to the boss, then endless continues from there and accelerates:
    // exp(a*m + b*m^2): x2.9 after 10 min of endless, x13 after 20, x150 after 30 (on top of the boss value)
    float HealthMultiplier()
    {
        float atBoss = Mathf.Pow(preBossHealthGrowth, Mathf.Max(0f, clock.BossAt - 20f));
        if (clock.Endless)
        {
            float m = EndlessMinutes;
            return atBoss * Mathf.Exp(Mathf.Log(endlessHealthGrowth) * m + endlessHealthAccel * m * m);
        }
        if (GameTime < 20) return 1f;
        return Mathf.Pow(preBossHealthGrowth, Mathf.Min(GameTime, clock.BossAt) - 20f);
    }
 }
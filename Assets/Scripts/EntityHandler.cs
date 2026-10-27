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

    [Header("Threat")]
    [SerializeField] private float floorStep = 1.11f;   // x per floor
    [SerializeField] private float timeStep = 1.03f;    // x per minute of the whole run
    [SerializeField] private float stayGrace = 45f;   // seconds on a floor before it starts
    [SerializeField] private float stayCap = 2f;      // never more than x2 from lingering
    [SerializeField] private float stayStep = 1.5f;    // x per minute on THIS floor (resets with the core)
    float Stay => Mathf.Min(stayCap, Mathf.Pow(stayStep, Mathf.Max(0f, clock.FloorTime - stayGrace) / 60f));
    [SerializeField] private float xpFloorStep = 1.04f;   // XP per enemy x this per floor (x2.1 at floor 20, x4.6 at 40)
    [SerializeField, Range(0f, 1f)] private float damageShare = 0.6f;   // damage grows slower than HP
    public void RegisterKill() => Kills++;
    // Roughly how many enemies spawn per second right now (average wave size / time between waves).
    // Events use it to scale with the game, e.g. Lockdown's kill target.
    public float EnemiesPerSecond => (MinSpawnCount + MaxSpawnCount) / 2f / Mathf.Max(0.1f, CurrentSpawnCD / (SpawnMultiplier * Grace(graceSpawnRate)));

    [Header("Grace period (new players)")]
    [SerializeField] private float[] graceSpawnRate = { 0.5f, 0.75f, 0.9f };  // spawn speed on floors 1-3 (1 = normal)
    [SerializeField] private float[] graceDamage = { 0.6f, 0.8f, 0.9f };      // enemy damage on floors 1-3
    private GameController game;

    [Header("Endless (after the boss)")]
    [SerializeField] private float eliteInterval = 150f;          // seconds between elites
    [SerializeField] private float eliteHealth = 3f, eliteDamage = 2f, eliteXp = 3f, eliteSize = 1.35f;
    [SerializeField] private Color eliteTint = new Color(1f, 0.35f, 0.3f, 1f);
    [SerializeField] private Material eliteMapIcon;              
    private float eliteTimer;
    private int floor = 1;
    public float Threat => Mathf.Pow(floorStep, floor - 1) * Mathf.Pow(timeStep, clock.Elapsed / 60f) * Stay;

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

    void Update()
    {
        if (game != null && floor != game.Floor) floor = game.Floor;

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
        enemy.Xp = Mathf.RoundToInt(enemies[index].XpValue * Mathf.Pow(xpFloorStep, floor - 1));
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

    float DamageMultiplier() => Mathf.Pow(Threat, damageShare);

    float HealthMultiplier() => Threat;
}
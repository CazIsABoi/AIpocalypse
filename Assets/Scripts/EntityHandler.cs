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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!dungeon.IsReady) return;
        SpawnCD -= Time.deltaTime;

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
            SpawnCD = CurrentSpawnCD / SpawnMultiplier;
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
        enemy.Damage = enemies[index].BaseDamage * DamageMultiplier();
        return enemy;
    }
    public void SpawnSoon() 
    {
        SpawnCD = 0f;
    }
    float DamageMultiplier()
    {
        if (clock.Endless)
            return 3f * Mathf.Pow(1.1f, clock.EndlessTime / 60f);   // +10% per minute, on top of 

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

    float HealthMultiplier()
    {
        if (clock.Endless)
            return 25f * Mathf.Pow(1.1f, clock.EndlessTime / 60f);   // +10% per minute, on top of 

        if (GameTime < 20)
        {
            return 1f;
        }
        else if (GameTime < 720)
        {
            return Mathf.Pow(1.0046f, GameTime - 20f); // 0.33
        }
        else if (GameTime >= 720)
        {
            return 25f;    //10f
        }
        return 1f;
    }
 }
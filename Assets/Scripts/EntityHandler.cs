using System.Collections.Generic;
using UnityEngine;

public class EntityHandler : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private EnemyData[] enemies = new EnemyData[0];

    [SerializeField] private float GameTime;
    private float SpawnCD;
    [SerializeField] private float CurrentSpawnCD;
    [SerializeField] private int MinSpawnCount;
    [SerializeField] private int MaxSpawnCount;
    [SerializeField] private List<GameObject> spawnedEnemies = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!dungeon.IsReady) return;
        GameTime += Time.deltaTime;
        SpawnCD -= Time.deltaTime;

        if (GameTime <= 600)
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
        Debug.Log("Current Index: " + CurrentWeightTableIndex);

        if(SpawnCD <= 0 && spawnedEnemies.Count < 100)
        {
            SpawnCD = CurrentSpawnCD;
            int RollQuantity = Random.Range(MinSpawnCount, MaxSpawnCount + 1);

            for (int i = 0; i < RollQuantity; i++)
            {
                int rolledEnemy = RollEnemyWeightAndIndex();

                if (!dungeon.TryGetSpawnPosition(out Vector3 spawnPos)) return;   // no spot found this time, try next cooldown
                GameObject spawnedEnemy = Instantiate(enemies[rolledEnemy].EnemyPrefab, spawnPos, Quaternion.identity, transform);
                EnemyController enemy = spawnedEnemy.GetComponent<EnemyController>();
                enemy.handler = this;
                spawnedEnemies.Add(spawnedEnemy);
                UpdateEnemyList();
                if (GameTime < 20)
                {
                    enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * 1f;
                    enemy.Damage = enemies[rolledEnemy].BaseDamage * 1f;
                }
                else if (GameTime < 720)
                {
                    enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * Mathf.Pow(1.0046f, GameTime - 20f); // 0.33
                    enemy.Damage = enemies[rolledEnemy].BaseDamage * Mathf.Pow(1.0016f, GameTime - 20f);
                }
                else if (GameTime >= 720)
                {
                    enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * 25f;    //10f
                    enemy.Damage = enemies[rolledEnemy].BaseDamage * 3f;
                }
                enemy.Xp = enemies[rolledEnemy].XpValue;
            }
        }
        else if(spawnedEnemies.Count >= 100)
        {
            for (int i = 0; i < spawnedEnemies.Count; i++)
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
                Debug.Log("Rolled This one");
                PickedEnemyIndex = i; break;
            }
            else CurrentWeight += enemies[i].SpawnWeights[CurrentWeightTableIndex];
        }

        return PickedEnemyIndex;
    }

    public void UpdateEnemyList()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
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
}

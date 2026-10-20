using UnityEngine;

public class EntityHandler : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private EnemyData[] enemies = new EnemyData[0];

    [SerializeField] private float GameTime;
    private float SpawnCD;

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

        if(SpawnCD <= 0)
        {
            int rolledEnemy = Random.Range(0, enemies.Length);
            SpawnCD = 3;
            if (!dungeon.TryGetSpawnPosition(out Vector3 spawnPos)) return;   // no spot found this time, try next cooldown
            GameObject spawnedEnemy = Instantiate(enemies[rolledEnemy].EnemyPrefab, spawnPos, Quaternion.identity, transform);
            EnemyController enemy = spawnedEnemy.GetComponent<EnemyController>();
            if (GameTime < 20)
            {
                enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * 1f;
                enemy.Damage = enemies[rolledEnemy].BaseDamage * 1f;
                enemy.Xp = enemies[rolledEnemy].XpValue * 1;
            }
            else if(GameTime < 720)
            {
                enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * Mathf.Pow(1.0046f, GameTime - 20f); // 0.33
                enemy.Damage = enemies[rolledEnemy].BaseDamage * Mathf.Pow(1.0046f, GameTime - 20f); // 0.33
                enemy.Xp = enemies[rolledEnemy].XpValue * 10; // 0.33
            }
            else if(GameTime >= 720)
            {
                enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * 25f;    //10f
                enemy.Damage = enemies[rolledEnemy].BaseDamage * 25f;    //10f
                enemy.Xp = enemies[rolledEnemy].XpValue * 25;    //10f
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

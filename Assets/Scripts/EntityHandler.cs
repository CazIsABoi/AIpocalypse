using UnityEngine;

public class EntityHandler : MonoBehaviour
{
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
        GameTime += Time.deltaTime;
        SpawnCD -= Time.deltaTime;

        if(SpawnCD <= 0)
        {
            int rolledEnemy = Random.Range(0, enemies.Length);
            SpawnCD = 3;
            GameObject spawnedEnemy = Instantiate(enemies[rolledEnemy].EnemyPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
            EnemyController enemy = spawnedEnemy.GetComponent<EnemyController>();
            if (GameTime < 20)
            {
                enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * 1f;
            }
            else if(GameTime < 720)
            {
                enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * Mathf.Pow(1.0046f, GameTime - 20f); // 0.33
            }
            else if(GameTime >= 720)
            {
                enemy.MaxHealth = enemies[rolledEnemy].BaseHealth * 25f;    //10f
            }
        }
    }
}

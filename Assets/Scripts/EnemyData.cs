using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemies/EnemyData")]
public class EnemyData : ScriptableObject
{
    public GameObject EnemyPrefab;
    public float BaseHealth;
    public float BaseDamage;

    public int[] SpawnWeights = new int[21];    // Its spawn chances for every 30 seconds interval, up to 10 minutes+
    public int XpValue;
}

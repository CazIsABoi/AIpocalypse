using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemies/EnemyData")]
public class EnemyData : ScriptableObject
{
    public GameObject EnemyPrefab;
    public float BaseHealth;
    public float BaseDamage;
    public int XpValue;
}

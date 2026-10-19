using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public GameObject EnemyPrefab;
    public float BaseHealth;
    public float BaseDamage;
}

using UnityEngine;

// Enemies sometimes drop a health orb. Put this on any object in MainScene.
// The lower the player's HP, the likelier a drop: a struggling player gets help, a healthy one barely any.
public class HealthDrops : MonoBehaviour
{
    [SerializeField] private HealthOrb orbPrefab;
    [SerializeField, Range(0f, 1f)] private float baseChance = 0.02f;   // at full HP
    [SerializeField, Range(0f, 1f)] private float lowHpChance = 0.12f;  // at (almost) 0 HP
    [SerializeField] private int maxOnFloor = 4;                        // never flood the room
    [SerializeField] private float heightOffset = 0.5f;

    private PlayerHealth health;
    private PlayerStats stats;

    void Awake()
    {
        health = FindAnyObjectByType<PlayerHealth>();
        if (health != null) stats = health.GetComponent<PlayerStats>();
    }

    void OnEnable() => CombatEvents.EnemyKilled += OnKilled;
    void OnDisable() => CombatEvents.EnemyKilled -= OnKilled;

    void OnKilled(Vector3 position)
    {
        if (orbPrefab == null || health == null || health.IsDead) return;
        if (FindObjectsByType<HealthOrb>().Length >= maxOnFloor) return;

        float missing = 1f - Mathf.Clamp01(health.Health / stats.MaxHealth);   // 0 = full HP, 1 = nearly dead
        if (Random.value > Mathf.Lerp(baseChance, lowHpChance, missing)) return;

        Instantiate(orbPrefab, position + Vector3.up * heightOffset, Quaternion.identity);
    }
}

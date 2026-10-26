using UnityEngine;

// Volatile Code: enemies sometimes explode when they die, damaging enemies around them.
// Kills from an explosion can explode too, so crowds chain-pop (spread over frames, never all at once).
public class VolatileCodeEffect : PlayerEffect
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject explosionPrefab;      // e.g. the drone explosion
    [SerializeField] private float baseChance = 0.25f;        // level 1: 25%, +15% per level after
    [SerializeField] private float chancePerLevel = 0.15f;
    [SerializeField] private float radius = 2.5f;             // +0.5 per level after 1
    [SerializeField] private float damageFactor = 0.6f;       // x player Damage, x level

    private readonly Collider[] buffer = new Collider[32];

    protected override EffectType Type => EffectType.VolatileCode;

    void OnEnable() => CombatEvents.EnemyKilled += OnKilled;
    void OnDisable() => CombatEvents.EnemyKilled -= OnKilled;

    void OnKilled(Vector3 position)
    {
        if (Level == 0) return;
        if (Random.value > baseChance + chancePerLevel * (Level - 1)) return;

        float r = radius + 0.5f * (Level - 1);
        DamageAround(position, r, stats.Damage * damageFactor * Level, enemyLayer, buffer);

        if (explosionPrefab != null) Destroy(Instantiate(explosionPrefab, position, Quaternion.identity), 3f);
        AudioManager.Instance?.PlayAt(Sfx.DroneDeath, position);
        CameraShake.Instance?.Add(0.1f);
    }
}

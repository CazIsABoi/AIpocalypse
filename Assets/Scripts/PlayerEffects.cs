using System.Collections.Generic;
using UnityEngine;

// Assets store EffectType as numbers: only ever add new values at the END
public enum EffectType { VolatileCode, Riposte, OverclockedCore, FirewallAura, ShockwaveSwing, ChainLightning, Ricochet, SecondBoost }

// Per-run effect levels, on the Player. Cards (EffectUpgrade) raise a level; effect components read it.
// Level 0 = the player doesn't have it, so the effect does nothing.
public class PlayerEffects : MonoBehaviour
{
    private readonly Dictionary<EffectType, int> levels = new Dictionary<EffectType, int>();

    public int Level(EffectType type) => levels.TryGetValue(type, out int level) ? level : 0;

    public void Add(EffectType type, int amount)
    {
        levels[type] = Level(type) + amount;
    }

    // Testing: pick an effect here, then in Play mode use this component's ⋮ menu
    [Header("Debug")]
    [SerializeField] private EffectType debugEffect;

    [ContextMenu("Debug: add a level of Debug Effect")]
    void DebugAdd()
    {
        Add(debugEffect, 1);
        Debug.Log($"{debugEffect} is now level {Level(debugEffect)}");
    }
}

// Base for every on-hit / on-kill effect. Put the effect components on the Player prefab:
// they sit idle at level 0 and switch on when a card gives them a level.
public abstract class PlayerEffect : MonoBehaviour
{
    protected PlayerEffects effects;
    protected PlayerStats stats;
    protected PlayerHealth health;

    protected abstract EffectType Type { get; }
    protected int Level => effects.Level(Type);

    protected virtual void Awake()
    {
        effects = GetComponent<PlayerEffects>();
        if (effects == null) effects = gameObject.AddComponent<PlayerEffects>();
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
    }

    // Damage enemies in a radius without raising EnemyHit (see CombatEvents). Returns how many were hit.
    protected int DamageAround(Vector3 centre, float radius, float damage, LayerMask enemyLayer, Collider[] buffer)
    {
        int count = Physics.OverlapSphereNonAlloc(centre, radius, buffer, enemyLayer);
        int hit = 0;
        for (int i = 0; i < count; i++)
        {
            // InParent: an enemy's collider can sit on a child object (OverloadDischarge does the same)
            EnemyController enemy = buffer[i].GetComponentInParent<EnemyController>();
            if (enemy == null || enemy.Health <= 0f) continue;
            enemy.TakeDamage(damage, stats.CritChance, stats.CritDamage);
            hit++;
        }
        return hit;
    }
}

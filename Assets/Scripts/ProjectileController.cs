using System.Collections.Generic;
using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    public Vector3 Direction;
    public LayerMask hittableLayers;

    public string ProjectileType;

    public float BulletSpeed;
    public float Damage;
    public float CritChance;
    public float CritDamage;

    public float DamageCD;
    public int BulletPierce;

    public float Lifetime = 1f;
    public float Lifesteal;
    public PlayerHealth Owner;

    [Header("Walls")]
    public int Bounces;                         // set by PlayerCombat from the Ricochet level, 0 = none
    public LayerMask wallLayers;                // what stops bullets: walls (Terrain), props, barriers
    [SerializeField] private float bounceLifetime = 0.4f;   // a bounce tops the lifetime back up to at least this

    [Header("Ricochet: out of pierce, bounce on to the nearest enemy")]
    [SerializeField] private float enemyBounceRange = 6f;

    [Header("Overpenetration / Fragmentation")]
    public bool Overpenetrate;                  // kills don't use up pierce
    public int FragmentLevel;                   // 0 = off; a kill splits the bullet into 1 + level fragments
    [SerializeField] private float fragmentDamage = 0.3f;     // x this bullet's damage, +0.1 per level after 1
    [SerializeField] private float fragmentLifetime = 0.35f;
    [SerializeField] private float fragmentScale = 0.6f;
    private bool isFragment;                    // fragments never split again

    private HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();   // a bullet never hits the same enemy twice

    void Update()
    {
        if (HitWall()) return;
        transform.position = Vector3.MoveTowards(transform.position, transform.position + Direction, BulletSpeed * Time.deltaTime);

        if (DamageCD <= 0)
        {
            if (ProjectileType == "Player Bullet")
            {
                if (PlayerHits()) return;
            }
            else if(ProjectileType == "Tripod Bullet")
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 0.15f, hittableLayers);
                foreach (Collider c in hits)
                {

                    if (c.gameObject.GetComponent<PlayerHealth>())
                    {
                        c.gameObject.GetComponent<PlayerHealth>().TakeDamage(Damage, "Tripod shot");

                    }

                    Destroy(gameObject);
                }
            }
        }
        else DamageCD -= Time.deltaTime;

        if (Lifetime > 0) Lifetime -= Time.deltaTime;
        else Destroy(gameObject);
    }

    // Returns true if the bullet is gone
    bool PlayerHits()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.15f, hittableLayers);
        foreach (Collider c in hits)
        {
            if (c.TryGetComponent(out EnemyController enemy))
            {
                if (!hitEnemies.Add(enemy)) continue;
                float healthBefore = enemy.Health;
                float amount = enemy.TakeDamage(Damage, CritChance, CritDamage) * Lifesteal;
                Owner?.HealLifesteal(Mathf.Clamp(amount, 0f, Mathf.Max(0f, healthBefore))); // never more than the enemy had, never negative (enemy already dead this frame)
                CombatEvents.Hit(enemy, healthBefore - enemy.Health, HitSource.Gun);

                bool killed = healthBefore > 0f && enemy.Health <= 0f;
                if (killed) Fragment(enemy);
                if (!(killed && Overpenetrate)) BulletPierce--;
            }
            else if (c.TryGetComponent(out Core core))
            {
                core.TakeBulletDamage(Damage);
                BulletPierce--;
                DamageCD = 0.1f;
            }
            else if (c.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(Damage);
                BulletPierce--;
                DamageCD = 0.1f;
            }
            else continue;   // walls are HitWall's job

            if (BulletPierce <= 0 && !BounceToEnemy()) { Destroy(gameObject); return true; }
        }
        return false;
    }

    // Ricochet: instead of stopping, head for the nearest enemy this bullet hasn't hit yet (uses a bounce)
    bool BounceToEnemy()
    {
        if (Bounces <= 0) return false;
        EnemyController next = null;
        float best = float.MaxValue;
        foreach (Collider c in Physics.OverlapSphere(transform.position, enemyBounceRange, hittableLayers))
        {
            if (!c.TryGetComponent(out EnemyController e) || e.Health <= 0f || hitEnemies.Contains(e)) continue;
            float d = (e.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; next = e; }
        }
        if (next == null) return false;

        Vector3 to = next.transform.position - transform.position;
        to.y = 0f;
        Direction = to.normalized;
        Bounces--;
        BulletPierce = 1;
        Lifetime = Mathf.Max(Lifetime, bounceLifetime);
        return true;
    }

    // Fragmentation: a kill splits the bullet into smaller ones flying out all around
    void Fragment(EnemyController killed)
    {
        if (FragmentLevel <= 0 || isFragment) return;
        int count = 1 + FragmentLevel;
        float start = Random.Range(0f, 360f);
        float damage = Damage * (fragmentDamage + 0.1f * (FragmentLevel - 1));
        for (int i = 0; i < count; i++)
        {
            var shard = Instantiate(gameObject, transform.position, transform.rotation).GetComponent<ProjectileController>();
            shard.isFragment = true;
            shard.FragmentLevel = 0;
            shard.Direction = Quaternion.Euler(0f, start + 360f * i / count, 0f) * Vector3.forward;
            shard.Damage = damage;
            shard.BulletPierce = 1;
            shard.Bounces = 0;
            shard.DamageCD = 0f;
            shard.Lifetime = fragmentLifetime;
            shard.hitEnemies.Add(killed);
            shard.transform.localScale = transform.localScale * fragmentScale;
        }
    }

    // Would this frame's step go into a wall? A ray covers the whole step, so fast bullets can't skip thin walls.
    // Ricochet bullets bounce off it, everything else stops there. Returns true if the bullet is gone.
    bool HitWall()
    {
        float step = BulletSpeed * Time.deltaTime;
        if (!Physics.Raycast(transform.position, Direction, out RaycastHit hit, step, wallLayers, QueryTriggerInteraction.Ignore)) return false;
        if (hit.normal.y > 0.5f) return false;   // the floor (also on Default): only walls and props stop bullets
        if (Bounces <= 0) { Destroy(gameObject); return true; }

        Direction = Vector3.Reflect(Direction, hit.normal);
        Direction.y = 0f;   // stay flat: a sloped wall normal would send it into the floor or sky
        Direction.Normalize();
        transform.position = hit.point + hit.normal * 0.05f;   // just off the wall, or the next raycast starts inside it
        Bounces--;
        Lifetime = Mathf.Max(Lifetime, bounceLifetime);
        return false;
    }
}

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

    void Update()
    {
        if (HitWall()) return;
        if(ProjectileType == "Player Bullet") transform.position = Vector3.MoveTowards(transform.position, transform.position + Direction, BulletSpeed * Time.deltaTime);
        else if(ProjectileType == "Tripod Bullet") transform.position = Vector3.MoveTowards(transform.position, transform.position + Direction, BulletSpeed * Time.deltaTime);


        if (DamageCD <= 0)
        {
            if (ProjectileType == "Player Bullet")
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 0.15f, hittableLayers);
                bool hitSomething = false;
                foreach (Collider c in hits)
                {
                    float amount = 0f;
                    float healthBefore = 0f;
                    if (BulletPierce > 0)
                    {
                        if (c.TryGetComponent(out EnemyController enemy))
                        {
                            hitSomething = true;
                            healthBefore = enemy.Health;
                            amount = enemy.TakeDamage(Damage, CritChance, CritDamage);   // enemies: with crits
                            amount = amount * Lifesteal;
                            Owner?.HealLifesteal(Mathf.Clamp(amount, 0f, Mathf.Max(0f, healthBefore))); // never more than the enemy had, never negative (enemy already dead this frame)
                            CombatEvents.Hit(enemy, healthBefore - enemy.Health, HitSource.Gun);
                        }
                        else if (c.TryGetComponent(out Core core))
                        {
                            hitSomething = true;
                            core.TakeBulletDamage(Damage);
                        }
                        else if (c.TryGetComponent(out IDamageable target))
                        {
                            hitSomething = true;
                            target.TakeDamage(Damage);                          // anything else hittable, 
                        }
                        
                        BulletPierce--;
                        if(BulletPierce <= 0) Destroy(gameObject);
                    }
                    
                    if (hitSomething) DamageCD = 0.1f;
                }
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

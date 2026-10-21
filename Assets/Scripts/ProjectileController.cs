using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    public Vector3 Direction;
    public LayerMask hittableLayers;

    public float BulletSpeed;
    public float Damage;
    public float CritChance;
    public float CritDamage;

    public float DamageCD;
    public int BulletPierce;

    public float Lifetime = 1f;
    public float Lifesteal;
    public PlayerHealth Owner;

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, transform.position + Direction, BulletSpeed * Time.deltaTime);

        if (DamageCD <= 0)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 1f, hittableLayers);
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
                        amount = amount * Lifesteal; // Lifesteal: heal for a percentage of the damage dealt
                        Owner?.Heal(Mathf.Min(amount, healthBefore)); // Don't heal for more than the enemy's health before the attack
                    }
                    else if (c.TryGetComponent(out IDamageable target))
                    {
                        hitSomething = true;
                        target.TakeDamage(Damage);                          // anything else hittable, e.g. the core
                    }
                }
                else Destroy(gameObject);
                BulletPierce--;
            }
            if (hitSomething) DamageCD = 0.1f;
        }
        else DamageCD -= Time.deltaTime;

        if (Lifetime > 0) Lifetime -= Time.deltaTime;
        else Destroy(gameObject);
    }
}

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

    // Update is called once per frame
    void Update()
    {
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
                            amount = amount * Lifesteal; // Lifesteal: heal for a percentage of the damage dealt
                            Owner?.Heal(Mathf.Clamp(amount, 0f, Mathf.Max(0f, healthBefore))); // never more than the enemy had, never negative (enemy already dead this frame)
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
                //bool hitSomething = false;
                foreach (Collider c in hits)
                {

                    if (c.gameObject.GetComponent<PlayerHealth>())
                    {
                        //hitSomething = true;
                        c.gameObject.GetComponent<PlayerHealth>().TakeDamage(Damage);

                    }
                    
                    Destroy(gameObject);
                }
            }
        }
        else DamageCD -= Time.deltaTime;

        if (Lifetime > 0) Lifetime -= Time.deltaTime;
        else Destroy(gameObject);
    }
}

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

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
                if (BulletPierce > 0)
                {
                    if (c.gameObject.GetComponent<EnemyController>())
                    {
                        hitSomething = true;
                        c.gameObject.GetComponent<EnemyController>().TakeDamage(Damage, CritChance, CritDamage);
                        
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

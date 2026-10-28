using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] private float flightTime = 0.6f;
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private GameObject explosionEffect;   

    Vector3 start, target;
    float damage, radius, timer;

    // branch upgrades, set by FragGrenadeAbility right after Throw
    float critChance, critDamage = 1f;
    PlayerHealth healTarget; float healPerEnemy; int maxHealed;
    int bomblets; float bombletDamage, bombletSpread;
    PlayerStats smokeStats; float smokeDuration, smokeArmor; GameObject smokeEffect;

    public void Throw(Vector3 target, float damage, float radius)
    {
        start = transform.position;
        this.target = target;
        this.damage = damage;
        this.radius = radius;
    }

    public void Crit(float chance, float damage) { critChance = chance; critDamage = damage; }
    public void Heal(PlayerHealth player, float perEnemy, int max) { healTarget = player; healPerEnemy = perEnemy; maxHealed = max; }
    public void Cluster(int count, float damageScale, float spread) { bomblets = count; bombletDamage = damageScale; bombletSpread = spread; }
    public void Smoke(PlayerStats player, float duration, float armor, GameObject effect)
    {
        smokeStats = player; smokeDuration = duration; smokeArmor = armor; smokeEffect = effect;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / flightTime;   // 0 at the start, 1 when it lands

        transform.position = Vector3.Lerp(start, target, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * arcHeight;
        if (t >= 1f) Explode();
    }

    void Explode()
    {  
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayers);
        int enemiesHit = 0;
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null) continue;
            enemy.TakeDamage(damage, critChance, critDamage);
            enemiesHit++;
        }

        if (healTarget != null && enemiesHit > 0)
            healTarget.Heal(healPerEnemy * Mathf.Min(enemiesHit, maxHealed));

        // the bomblets are grenades too, just smaller and without upgrades of their own (no endless splitting)
        for (int i = 0; i < bomblets; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(bombletSpread * 0.5f, bombletSpread);
            var bomblet = Instantiate(this, transform.position, Quaternion.identity);
            bomblet.flightTime = flightTime * 0.6f;
            bomblet.arcHeight = arcHeight * 0.5f;
            bomblet.Throw(transform.position + new Vector3(offset.x, 0f, offset.y), damage * bombletDamage, radius * 0.5f);
            bomblet.Crit(critChance, critDamage);
            bomblet.transform.localScale = transform.localScale * 0.6f;
        }

        if (smokeStats != null)
            SmokeCloud.Spawn(transform.position, radius, smokeDuration, smokeArmor, smokeStats, smokeEffect);
        if (explosionEffect != null) Instantiate(explosionEffect, transform.position, Quaternion.identity);
        AudioManager.Instance?.PlayAt(Sfx.GrenadeExplode, transform.position);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

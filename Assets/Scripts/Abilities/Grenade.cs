using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] private float flightTime = 0.6f;
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private GameObject explosionEffect;   

    Vector3 start, target;
    float damage, radius, timer;

    public void Throw(Vector3 target, float damage, float radius)
    {
        start = transform.position;
        this.target = target;
        this.damage = damage;
        this.radius = radius;
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
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(damage, 0f, 1f);
        }
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

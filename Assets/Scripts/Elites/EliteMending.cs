using UnityEngine;

public class EliteMending : EliteBehaviour
{
    protected override float Interval => 3f;
    float radius = 7f;
    float healPercent = 0.1f;

    protected override void Tick()
    {   
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemy.handler.EnemyLayers);
        foreach (var hit in hits)
        {
            EnemyController e = hit.GetComponent<EnemyController>();
            if (e == null || e.IsBoss || e.IsElite) continue;
            float heal = 0;
            heal += e.MaxHealth * healPercent;
            e.Health = Mathf.Min(e.Health + heal, e.MaxHealth);
        }
    }
}
using UnityEngine;

public class EliteFirewall : EliteBehaviour
{
    protected override float Interval => 0.25f;
    float radius = 6f;

    protected override void Tick()
    {
        if (Vector3.Distance(player.position, transform.position) < radius) return; // the bubble is broken
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemy.handler.EnemyLayers);
        foreach (var hit in hits)
        {
            EnemyController e = hit.GetComponent<EnemyController>();
            if (e == null || e.IsBoss || e.IsElite) continue;
            e.ShieldedUntil = Time.time + 0.5f;
        }
    }
}
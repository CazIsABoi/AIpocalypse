using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Chain Lightning: a bullet hit has a chance to arc to nearby enemies, jumping from one to the next.
public class ChainLightningEffect : PlayerEffect
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float chance = 0.2f;          // +10% per level after 1
    [SerializeField] private int jumps = 2;                // +1 per level after 1
    [SerializeField] private float jumpRange = 4f;
    [SerializeField] private float damageFactor = 0.5f;    // x the hit's damage, per jump

    [Header("Look")]
    [SerializeField] private Material arcMaterial;         // the OverloadArc material works
    [SerializeField] private int maxArcs = 8;              // pooled LineRenderers, reused round-robin
    [SerializeField] private float arcTime = 0.15f;
    [SerializeField] private float jitter = 0.18f;
    [SerializeField] private float chestHeight = 1f;

    private readonly Collider[] buffer = new Collider[16];
    private readonly List<EnemyController> chained = new List<EnemyController>();   // reused: no garbage per hit
    private LineRenderer[] arcs;
    private int nextArc;

    protected override EffectType Type => EffectType.ChainLightning;

    protected override void Awake()
    {
        base.Awake();
        arcs = new LineRenderer[maxArcs];
        for (int i = 0; i < maxArcs; i++) arcs[i] = MakeArc("ChainArc" + i);
    }

    void OnEnable() => CombatEvents.EnemyHit += OnHit;
    void OnDisable()
    {
        CombatEvents.EnemyHit -= OnHit;
        if (arcs != null) foreach (var lr in arcs) if (lr) lr.enabled = false;
    }

    void OnHit(EnemyController enemy, float damage, HitSource source)
    {
        if (Level == 0 || source != HitSource.Gun || enemy == null) return;
        if (Random.value > chance + 0.1f * (Level - 1)) return;

        chained.Clear();
        chained.Add(enemy);   // the one the bullet hit: never jump back to it
        Vector3 from = enemy.transform.position;
        float jumpDamage = damage * damageFactor;

        for (int i = 0; i < jumps + (Level - 1); i++)
        {
            EnemyController next = FindNearest(from, chained);
            if (next == null) break;   // nobody left in range

            Vector3 to = next.transform.position;
            next.TakeDamage(jumpDamage, 0f, 1f);   // no crit: the bullet hit already rolled one
            chained.Add(next);
            DrawArc(from, to);
            from = to;
        }

        if (chained.Count > 1) AudioManager.Instance?.PlayAt(Sfx.HazardZap, enemy.transform.position);
    }

    EnemyController FindNearest(Vector3 from, List<EnemyController> exclude)
    {
        int count = Physics.OverlapSphereNonAlloc(from, jumpRange, buffer, enemyLayer);
        EnemyController best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            EnemyController e = buffer[i].GetComponentInParent<EnemyController>();
            if (e == null || e.Health <= 0f || exclude.Contains(e)) continue;
            float d = (e.transform.position - from).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = e; }
        }
        return best;
    }

    void DrawArc(Vector3 a, Vector3 b)
    {
        LineRenderer lr = arcs[nextArc];
        nextArc = (nextArc + 1) % arcs.Length;
        StartCoroutine(Flash(lr, a + Vector3.up * chestHeight, b + Vector3.up * chestHeight));
    }

    // Same look as OverloadDischarge: a jittery line, re-randomised a few times so it flickers
    IEnumerator Flash(LineRenderer lr, Vector3 a, Vector3 b)
    {
        lr.enabled = true;
        for (float t = 0; t < arcTime; t += 0.03f)
        {
            for (int p = 0; p < lr.positionCount; p++)
            {
                float k = p / (lr.positionCount - 1f);
                Vector3 offset = (p == 0 || p == lr.positionCount - 1) ? Vector3.zero : Random.insideUnitSphere * jitter;
                lr.SetPosition(p, Vector3.Lerp(a, b, k) + offset);
            }
            yield return new WaitForSeconds(0.03f);
        }
        lr.enabled = false;
    }

    LineRenderer MakeArc(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 8;
        lr.widthMultiplier = 0.07f;
        lr.material = arcMaterial;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.enabled = false;
        return lr;
    }
}

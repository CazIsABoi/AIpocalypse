using System.Collections;
using UnityEngine;

// Standing inside a crowd of robots makes them overload: a charge builds while 'minEnemies' are close,
// then they discharge lightning into the player. Quick melee dips never fill it (it drains twice as fast).
// Cheap on purpose: one OverlapSphere 10x/sec on the player, and a few pooled LineRenderers for the arcs.
public class OverloadDischarge : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private float radius = 2.8f;          // just past Walker/Frog attack range (2.5): they stop there to attack
    [SerializeField] private int minEnemies = 3;
    [SerializeField] private float chargeTime = 1f;       // seconds surrounded before it discharges
    [SerializeField] private float drainSpeed = 2f;       // charge drains this many times faster when you step out
    [SerializeField] private LayerMask enemyLayer = 1 << 8;

    [Header("Damage")]
    [SerializeField] private float baseDamage = 3f;
    [SerializeField] private float perEnemy = 2f;

    [Header("Look")]
    [SerializeField] private Material arcMaterial;        // OverloadArc (additive glow)
    [SerializeField] private int maxArcs = 3;
    [SerializeField] private float arcTime = 0.15f;
    [SerializeField] private float jitter = 0.18f;        // how wild the lightning zig-zags
    [SerializeField] private float chestHeight = 1f;      // arcs hit the body, not the feet

    private readonly Collider[] hits = new Collider[24];  // reused buffer: no garbage each check
    private readonly Transform[] nearby = new Transform[24];
    private int nearbyCount;
    private float charge, checkTimer, warnTimer;
    private LineRenderer[] arcs;
    private LineRenderer warnArc;
    private PlayerHealth health;

    public float Charge => charge;   // 0..1, e.g. for a HUD warning later

    // Melee hits call this: fighting back in a crowd delays the discharge
    public void ReduceCharge(float amount) => charge = Mathf.Max(0f, charge - amount);

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        arcs = new LineRenderer[maxArcs];
        for (int i = 0; i < maxArcs; i++) arcs[i] = MakeArc("Arc" + i, 0.07f);
        warnArc = MakeArc("WarnArc", 0.03f);
    }

    LineRenderer MakeArc(string name, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 8;
        lr.widthMultiplier = width;
        lr.material = arcMaterial;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.enabled = false;
        return lr;
    }

    void Update()
    {
        if (health != null && health.IsDead) { charge = 0f; return; }

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f) { checkTimer = 0.1f; CountNearby(); }

        bool surrounded = nearbyCount >= minEnemies;
        float before = charge;
        charge = Mathf.Clamp01(charge + (surrounded ? 1f : -drainSpeed) * Time.deltaTime / chargeTime);
        if (before == 0f && charge > 0f) AudioManager.Instance?.PlayAt(Sfx.HazardWarn, transform.position);   // "it's building"

        // Telegraph: small crackles toward a nearby robot, more often the fuller the charge
        if (charge > 0f && nearbyCount > 0)
        {
            warnTimer -= Time.deltaTime;
            if (warnTimer <= 0f)
            {
                warnTimer = Mathf.Lerp(0.35f, 0.06f, charge);
                StartCoroutine(Flash(warnArc, nearby[Random.Range(0, nearbyCount)], 0.06f));
            }
        }

        if (charge >= 1f) Discharge();
    }

    void CountNearby()
    {
        nearbyCount = 0;
        int n = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, enemyLayer);
        for (int i = 0; i < n; i++)
        {
            // only real robots count (the Core is on the Enemy layer too)
            EnemyController e = hits[i].GetComponentInParent<EnemyController>();
            if (e != null) nearby[nearbyCount++] = e.transform;
        }
    }

    void Discharge()
    {
        charge = 0f;
        health?.TakeDamage(baseDamage + perEnemy * nearbyCount);   // goes through the i-frame window like any hit
        AudioManager.Instance?.PlayAt(Sfx.HazardZap, transform.position);
        CameraShake.Instance?.Add(0.25f);
        for (int i = 0; i < Mathf.Min(maxArcs, nearbyCount); i++)
            StartCoroutine(Flash(arcs[i], nearby[i], arcTime));
    }

    // Draws a jittery line player → enemy, re-randomised a few times so it flickers like lightning
    IEnumerator Flash(LineRenderer lr, Transform target, float time)
    {
        if (lr == null || target == null) yield break;
        lr.enabled = true;
        for (float t = 0; t < time; t += 0.03f)
        {
            if (target == null) break;
            Vector3 a = transform.position + Vector3.up * chestHeight;
            Vector3 b = target.position + Vector3.up * chestHeight;
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

    void OnDisable() { foreach (var lr in arcs) if (lr) lr.enabled = false; if (warnArc) warnArc.enabled = false; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

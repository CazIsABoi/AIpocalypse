using UnityEngine;

// Shockwave Swing: every Nth melee swing releases an electric shockwave around the swing.
public class ShockwaveSwingEffect : PlayerEffect
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int swingsNeeded = 3;
    [SerializeField] private float radius = 3f;            // +0.5 per level after 1
    [SerializeField] private float damageFactor = 1f;      // x MeleeHit, x level
    [SerializeField] private GameObject shockwavePrefab;  

    private readonly Collider[] buffer = new Collider[32];

    protected override EffectType Type => EffectType.ShockwaveSwing;
    private int swingsSinceLastShockwave = 0;

    void OnEnable() => CombatEvents.MeleeSwung += OnSwing;
    void OnDisable() => CombatEvents.MeleeSwung -= OnSwing;

    void OnSwing(Vector3 centre, bool hitAny)
    {
        if (Level == 0) return;

        if (!hitAny) return;
        swingsSinceLastShockwave++;

        if (swingsSinceLastShockwave >= swingsNeeded)
        {
            swingsSinceLastShockwave = 0;
            float r = radius + 0.5f * (Level - 1);
            DamageAround(centre, r, stats.MeleeHit * damageFactor * Level, enemyLayer, buffer);
            if (shockwavePrefab != null)
            {
                GameObject fx = Instantiate(shockwavePrefab, centre, Quaternion.identity);
                fx.transform.localScale = Vector3.one * r;   // the prefab is built at radius 1 (needs Scaling Mode = Hierarchy)
                Destroy(fx, 3f);
            }
            AudioManager.Instance?.PlayAt(Sfx.Shockwave, centre);
            CameraShake.Instance?.Add(0.25f);
        }
    }
}

using Unity.Scripting.LifecycleManagement;
using UnityEngine;

public enum HitSource { Gun, Melee, Effect }

// The game announces combat moments here; on-hit effects (PlayerEffect) listen.
// Only the player's gun and melee raise EnemyHit. Effect damage does NOT, so an effect can never
// trigger itself (chain lightning hitting -> more chain lightning -> ... forever).
[AutoStaticsCleanup]
static partial class CombatEvents
{
    public static event System.Action<EnemyController, float, HitSource> EnemyHit;   // (enemy, damage dealt, source)
    public static event System.Action<Vector3> EnemyKilled;                           // where it died (any cause)
    public static event System.Action<Vector3, bool> MeleeSwung;                      // (swing centre, hit anything?)

    public static void Hit(EnemyController enemy, float damage, HitSource source) => EnemyHit?.Invoke(enemy, damage, source);
    public static void Killed(Vector3 position) => EnemyKilled?.Invoke(position);
    public static void Swung(Vector3 centre, bool hitAny) => MeleeSwung?.Invoke(centre, hitAny);
}

using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Frag Grenade")]
public class FragGrenadeAbility : ClassAbility
{
    public Grenade grenadePrefab;
    public float throwDistance = 8f;      // max range: lands on the cursor if it's closer
    public float damageMultiplier = 3f;   // times the player's Damage stat
    public float radius = 4f;

    [Header("Frontline: Concussion")]
    public float concussionRadius = 1.5f;     // x the blast radius
    public float healPerEnemy = 0.02f;        // fraction of max HP per enemy hit
    public int maxHealedEnemies = 10;

    [Header("Marksman: Cluster")]
    public int bomblets = 4;
    public float bombletDamage = 0.35f;       // x the grenade's damage
    public float bombletSpread = 3f;

    [Header("Bulwark: Smoke")]
    public float smokeDuration = 5f;
    public float smokeArmor = 30f;            // while standing in it
    public GameObject smokeEffect;            // optional particles, should last smokeDuration

    public override void Activate(Transform player)
    {
        var user = player.GetComponent<AbilityController>();
        var stats = player.GetComponent<PlayerStats>();

        Vector3 target = Aim.Point(player.position, throwDistance);
        float damage = stats.Damage * damageMultiplier * stats.AbilityPowerMult;
        var grenade = Instantiate(grenadePrefab, player.position, Quaternion.identity);

        if (user.Has(AbilityUpgrade.Concussion))
        {
            grenade.Throw(target, damage, radius * concussionRadius);
            grenade.Heal(player.GetComponent<PlayerHealth>(), healPerEnemy * stats.MaxHealth * stats.AbilityPowerMult, maxHealedEnemies);
        }
        else grenade.Throw(target, damage, radius);

        if (user.Has(AbilityUpgrade.Cluster))
        {
            grenade.Crit(stats.CritChance, stats.CritDamage);
            grenade.Cluster(bomblets, bombletDamage, bombletSpread);
        }

        if (user.Has(AbilityUpgrade.Smoke))
            grenade.Smoke(stats, smokeDuration, smokeArmor, smokeEffect);

        AudioManager.Instance?.Play(Sfx.GrenadeThrow);
    }
}

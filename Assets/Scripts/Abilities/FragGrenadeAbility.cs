using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Frag Grenade")]
public class FragGrenadeAbility : ClassAbility
{
    public Grenade grenadePrefab;
    public float throwDistance = 8f;      // max range: lands on the cursor if it's closer
    public float damageMultiplier = 3f;   // times the player's Damage stat
    public float radius = 4f;

    public override void Activate(Transform player)
    {
        Vector3 target = Aim.Point(player.position, throwDistance);
        float damage = player.GetComponent<PlayerStats>().Damage * damageMultiplier;
        Instantiate(grenadePrefab, player.position, Quaternion.identity).Throw(target, damage, radius);
        AudioManager.Instance?.Play(Sfx.GrenadeThrow);
    }
}

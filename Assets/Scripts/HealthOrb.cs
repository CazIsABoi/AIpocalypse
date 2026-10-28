using UnityEngine;

// Heals a share of max HP. Stays on the floor while the player is at full health, so it isn't wasted.
public class HealthOrb : Pickup
{
    [SerializeField, Range(0f, 1f)] private float healFraction = 0.15f;   // of max HP
    [SerializeField] private float attractRange = 2f;                      // smaller than XP: you go and get it
    [SerializeField, Range(0f, 1f)] private float bonusShare = 0.5f;       // + this share of the player's extra pickup range
    [SerializeField] private float lifetime = 20f;                         // don't litter the floor forever
    private PlayerHealth health;

    protected override void Awake()
    {
        base.Awake();
        if (player != null) health = player.GetComponent<PlayerHealth>();
        Destroy(gameObject, lifetime);
    }

    protected override float AttractRange => attractRange + player.PickupBonus * bonusShare;   // pickup range upgrades pull health too
    protected override bool CanAttract => health != null && health.Health < player.MaxHealth;

    protected override void Collect()
    {
        health.Heal(player.MaxHealth * healFraction);
        RunTelemetry.Instance?.HealthOrbCollected(player.MaxHealth * healFraction);
        AudioManager.Instance?.Play(Sfx.HealthPickup);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Saved by number on perk nodes: only ever add new values at the END
public enum AbilityUpgrade { None, Concussion, Cluster, Smoke }

public class AbilityController : MonoBehaviour
{
    [SerializeField] private InputActionReference abilityAction;
    [SerializeField] private ClassAbility ability;   

    public ClassAbility Ability => ability;
    public float CooldownLeft { get; private set; }
    public bool Ready => ability != null && CooldownLeft <= 0f;
    public float Cooldown => ability != null ? ability.Cooldown * (1f - (stats != null ? stats.AbilityCooldown : 0f)) : 1f;

    private PlayerStats stats;
    private readonly HashSet<AbilityUpgrade> upgrades = new HashSet<AbilityUpgrade>();
    public bool Has(AbilityUpgrade upgrade) => upgrades.Contains(upgrade);
    public void AddUpgrade(AbilityUpgrade upgrade) => upgrades.Add(upgrade);

    void Awake() => stats = GetComponent<PlayerStats>();

    void OnEnable() => abilityAction.action.Enable();

    public void Equip(ClassAbility newAbility)
    {
        ability = newAbility;
        CooldownLeft = 0f;
    }

    void Update()
    {
        if (ability == null) return;

        bool wasCooling = CooldownLeft > 0f;
        CooldownLeft = Mathf.Max(0f, CooldownLeft - Time.deltaTime);
        if (wasCooling && CooldownLeft <= 0f) AudioManager.Instance?.Play(Sfx.AbilityReady);

        if (abilityAction.action.WasPressedThisFrame() && Ready)
        {
            ability.Activate(transform);
            CooldownLeft = Cooldown;
        }
    }
}

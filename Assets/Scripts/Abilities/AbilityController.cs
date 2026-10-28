using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityController : MonoBehaviour
{
    [SerializeField] private InputActionReference abilityAction;
    [SerializeField] private ClassAbility ability;   

    public ClassAbility Ability => ability;
    public float CooldownLeft { get; private set; }
    public bool Ready => ability != null && CooldownLeft <= 0f;

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
            CooldownLeft = ability.Cooldown;
        }
    }
}

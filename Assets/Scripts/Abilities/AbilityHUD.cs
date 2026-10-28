using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityHUD : MonoBehaviour
{
    [SerializeField] private AbilityController controller;
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownFill;  
    [SerializeField] private TMP_Text readyText;   // "READY" while usable, hidden during the cooldown

    void Update()
    {
        if (controller.Ability == null)
        {
            icon.enabled = false;
            cooldownFill.fillAmount = 0f;
            if (readyText != null) readyText.enabled = false;
            return;
        }
        icon.enabled = true;
        icon.sprite = controller.Ability.Icon;
        cooldownFill.fillAmount = controller.CooldownLeft / controller.Ability.Cooldown;
        if (readyText != null) readyText.enabled = controller.Ready;
    }
}

using UnityEngine;

public class HealthText : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    private void Start()
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateHealthText;
            UpdateHealthText(playerHealth.Health, playerHealth.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealthText;
        }
    }

    private void UpdateHealthText(float currentHealth, float maxHealth)
    {
        int healthpercentage = Mathf.RoundToInt((currentHealth / maxHealth) * 100);
        healthText.text = $"Health: {healthpercentage}%";
    }
}

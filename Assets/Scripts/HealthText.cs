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
        healthText.text = $"Health: {currentHealth}/{maxHealth}";
    }
}

using UnityEngine;

public class HealthText : MonoBehaviour
{
    private void Awake()
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateHealthText;
            UpdateHealthText(playerHealth.Health, playerHealth.MaxHealth);
        }
    }

   private void UpdateHealthText(float currentHealth, float maxHealth)
    {
        GetComponent<TMPro.TextMeshProUGUI>().text = $"Health: {currentHealth}/{maxHealth}";
    }
}

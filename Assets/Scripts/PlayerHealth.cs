using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float invulnerableTime = 0.3f;   // short protection after a hit

    [SerializeField] private UnityEngine.UI.Slider slider;
    private PlayerStats stats;

    public float Health { get; private set; }
    public float MaxHealth => stats.MaxHealth;
    public bool IsDead => Health <= 0;

    public event System.Action<float, float> HealthChanged;   // (current, max)
    public event System.Action Died;

    private float invulnerableTimer;

    void Awake() { stats = GetComponent<PlayerStats>(); Health = stats.MaxHealth; invulnerableTimer = 0f; UpdateBar(Health, stats.MaxHealth);  }
    void Update() { invulnerableTimer = Mathf.Max(0f, invulnerableTimer - Time.deltaTime); }


    public void TakeDamage(float amount)
    {
        if (IsDead || invulnerableTimer > 0f) return;
        Health = Mathf.Max(0f, Health - amount);
        invulnerableTimer = invulnerableTime;
        HealthChanged?.Invoke(Health, stats.MaxHealth);
        UpdateBar(Health, stats.MaxHealth);
        if (IsDead) Died?.Invoke();
    }
    void UpdateBar(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;
    }

    public void ResetHealth() { Health = stats.MaxHealth; invulnerableTimer = 0f; HealthChanged?.Invoke(Health, stats.MaxHealth); UpdateBar(Health, stats.MaxHealth); }
}
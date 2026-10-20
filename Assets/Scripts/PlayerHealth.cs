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
    private float lastMaxHealth;                               

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        lastMaxHealth = stats.MaxHealth;
        SetHealth(stats.MaxHealth);
    }

    void OnEnable() { stats.StatsChanged += OnStatsChanged; }  
    void OnDisable() { stats.StatsChanged -= OnStatsChanged; }   

    void Update() { invulnerableTimer = Mathf.Max(0f, invulnerableTimer - Time.deltaTime); }

    public void TakeDamage(float amount)
    {
        if (IsDead || invulnerableTimer > 0f) return;
        invulnerableTimer = invulnerableTime;
        SetHealth(Health - amount);
        if (IsDead) Died?.Invoke();
    }

    public void ResetHealth()
    {
        invulnerableTimer = 0f;
        SetHealth(stats.MaxHealth);
    }

    void OnStatsChanged(StatType type)
    {
        if (type != StatType.MaxHealth) return;

        float gained = stats.MaxHealth - lastMaxHealth;
        lastMaxHealth = stats.MaxHealth;
        SetHealth(Health + gained);          // heal by the amount max health went up
    }

    void SetHealth(float value)
    {
        Health = Mathf.Clamp(value, 0f, stats.MaxHealth);
        HealthChanged?.Invoke(Health, stats.MaxHealth);
        UpdateBar(Health, stats.MaxHealth);
    }

    void UpdateBar(float current, float max)
    {
        if (slider == null) return;
        slider.maxValue = max;
        slider.value = current;
    }
}
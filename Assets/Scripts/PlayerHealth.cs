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

    public void Heal(float amount)
    {
        if (IsDead) return;
        SetHealth(Health + amount);
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

        if (gained > 0)
            SetHealth(Health + gained);    // max went up: heal by the same amount 
        else
            SetHealth(Health);             // max went down: just re-clamp to the new max
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
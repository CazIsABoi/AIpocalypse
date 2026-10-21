using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float invulnerableTime = 0.3f;   // short protection after a hit
    [SerializeField] private float regenDelay = 15f;    // pause after taking damage
    [SerializeField, Range(0f, 1f)] float pausedRegenFactor = 0.2f;   // 20% of normal regen while paused
    [SerializeField] private float minShake = 0.1f;    // camera shake on hit
    [SerializeField] private float maxShake = 0.5f;    // camera shake on hit
    [SerializeField] private float k = 2f;            // sensitivity of shake to damage
    private float regenTimer;

    [SerializeField] private UnityEngine.UI.Slider slider;
    private PlayerStats stats;

    public float Health { get; private set; }
    public float MaxHealth => stats.MaxHealth;
    public bool IsDead => Health <= 0;

    public event System.Action<float, float> HealthChanged;   // (current, max)
    public event System.Action Died;
    public event System.Action<float> Damaged;

    private float invulnerableTimer;
    private float lastMaxHealth;   
    private UnityEngine.UI.Image sliderImage;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        sliderImage = slider.fillRect.GetComponent<UnityEngine.UI.Image>();
        lastMaxHealth = stats.MaxHealth;
        SetHealth(stats.MaxHealth);
    }

    void OnEnable() { stats.StatsChanged += OnStatsChanged; }  
    void OnDisable() { stats.StatsChanged -= OnStatsChanged; }   

    void Update() 
    {
        invulnerableTimer = Mathf.Max(0f, invulnerableTimer - Time.deltaTime);

        regenTimer = Mathf.Max(0f, regenTimer - Time.deltaTime);
        bool paused = regenTimer > 0f;
        float rate = paused ? pausedRegenFactor : 1f;          // the ternary picks one of two values

        if (!IsDead && Health < MaxHealth)
            Heal(stats.MaxHealth * stats.regenAmount * rate * Time.deltaTime);

        sliderImage.color = paused ? Color.teal : Color.white;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || invulnerableTimer > 0f) return;
        invulnerableTimer = invulnerableTime;
        regenTimer = regenDelay;
        SetHealth(Health - amount);
        float fraction = amount / stats.MaxHealth;      // 0.1 = this hit took 10% of max HP
        float shake = Mathf.Lerp(minShake, maxShake, fraction * k);
        CameraShake.Instance?.Add(shake);
        if (IsDead) { Died?.Invoke(); AudioManager.Instance.Play(Sfx.PlayerDeath); CameraShake.Instance?.Add(0.8f); }
        AudioManager.Instance.Play(Sfx.PlayerHurt);
        Damaged?.Invoke(amount);
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
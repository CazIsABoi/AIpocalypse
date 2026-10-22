using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float invulnerableTime = 0.3f;   // short protection after a hit
    private float windowDamage;
    [SerializeField] private float regenDelay = 15f;    // pause after taking damage
    [SerializeField, Range(0f, 1f)] float pausedRegenFactor = 0.2f;   // 20% of normal regen while paused
    [SerializeField] private float minShake = 0.1f;    // camera shake on hit
    [SerializeField] private float maxShake = 0.5f;    // camera shake on hit
    [SerializeField] private float k = 2f;            // sensitivity of shake to damage
    private float regenTimer;
    [SerializeField, Range(0f, 1f)] private float lowHealthFraction = 0.25f;   // LowHealth beep below this
    private bool lowWarned;

    [SerializeField] private UnityEngine.UI.Slider slider;
    private PlayerStats stats;

    public float Health { get; private set; }
    public float MaxHealth => stats.MaxHealth;
    public bool IsDead => Health <= 0;

    public event System.Action<float, float> HealthChanged;   // (current, max)
    public event System.Action<float> Healed;                 // amount healed
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
            Heal(stats.MaxHealth * stats.regenAmount * rate * Time.deltaTime, false);

        sliderImage.color = paused ? Color.teal : Color.white;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (invulnerableTimer > 0f)
        {
            if (amount <= windowDamage) return;     // smaller or equal hit: blocked, as before
            float extra = amount - windowDamage;    // bigger hit: only the part above what you already took
            windowDamage = amount;
            amount = extra;                         // then continue as a normal hit with 'extra'
        }
        else
        {
            invulnerableTimer = invulnerableTime;   // a fresh window starts
            windowDamage = amount;
        }
        regenTimer = regenDelay;
        SetHealth(Health - amount);
        float fraction = amount / stats.MaxHealth;      // 0.1 = this hit took 10% of max HP
        float shake = Mathf.Lerp(minShake, maxShake, fraction * k);
        CameraShake.Instance?.Add(shake);
        if (fraction > 0.2f) Hitstop.Instance?.Stop(0.05f);
        if (!IsDead) AudioManager.Instance?.Play(Sfx.PlayerHurt);   // death sound is played in Die()
        Damaged?.Invoke(amount);
    }

    public void Heal(float amount, bool show = true)
    {
        if (IsDead || amount <= 0f) return;   // a heal must never be able to hurt
        float healthBefore = Health;
        SetHealth(Health + amount);
        float actualHealed = Health - healthBefore;
        if (show) Healed?.Invoke(actualHealed);
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

    // Every health change goes through here, so death is checked HERE:
    // whatever drops HP to 0 (damage, a bad heal, a max-HP change) always fires Died exactly once.
    void SetHealth(float value)
    {
        bool wasDead = IsDead;
        Health = Mathf.Clamp(value, 0f, stats.MaxHealth);
        HealthChanged?.Invoke(Health, stats.MaxHealth);
        UpdateBar(Health, stats.MaxHealth);
        if (!wasDead && IsDead) Die();

        // Low-health warning: once when dropping below the line, re-armed after healing a bit above it
        if (!IsDead && !lowWarned && Health <= stats.MaxHealth * lowHealthFraction)
        {
            lowWarned = true;
            AudioManager.Instance?.Play(Sfx.LowHealth);
        }
        else if (Health > stats.MaxHealth * (lowHealthFraction + 0.1f)) lowWarned = false;
    }

    void Die()
    {
        Died?.Invoke();
        AudioManager.Instance?.Play(Sfx.PlayerDeath);
        CameraShake.Instance?.Add(0.8f);
    }

    void UpdateBar(float current, float max)
    {
        if (slider == null) return;
        slider.maxValue = max;
        slider.value = current;
    }
}
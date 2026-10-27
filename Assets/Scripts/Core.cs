using UnityEngine;

public class Core : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float baseHealth = 200f;
    [SerializeField] private float perFloor = 0.35f;     
    [SerializeField] private float perMinute = 0.05f;   
    [SerializeField] private float bulletDamageMultiplier = 0.1f;
    [SerializeField] private GameObject DamagePopup;
    [SerializeField] private Vector3 popupOffset = new Vector3(2.2f, 2.5f, 0f);   // world offset: +X = screen right in the top-down view
    [SerializeField] private float popupSumWindow = 0.5f;   // hits closer together than this add onto the same number
    private DamagePopup popup;
    private float popupTotal, lastPopupTime;
    private float health;
    private bool destroyed;
    private GameController game;
    private Transform player;
    public bool Discovered { get; private set; }   // the player got near it (telemetry: did they find the Core?)
    [SerializeField] private float discoverDistance = 10f;

    void Awake()
    {
        game = FindAnyObjectByType<GameController>();
        var ph = FindAnyObjectByType<PlayerHealth>();
        if (ph != null) player = ph.transform;
        var clock = FindAnyObjectByType<RunClock>();
        maxHealth = baseHealth * (1 + perFloor * (game.Floor - 1)) * (1 + perMinute * clock.Elapsed / 60f);
        health = maxHealth;
    }

    void Update() => CheckDiscovered();

    void CheckDiscovered()
    {
        if (Discovered || player == null) return;
        Vector3 d = player.position - transform.position; d.y = 0f;
        if (d.magnitude < discoverDistance) Discovered = true;
    }

    public void TakeDamage(float amount)
    {
        if (destroyed) return;
        health -= amount;
        if (health <= 0)
        {
            destroyed = true;
            AudioManager.Instance?.Play(Sfx.CoreDestroyed);
            game.NextFloor();
        }
        // Rapid hits add up on one number (like enemies); also stops 10% bullet hits showing as "0"
        if (popup != null && Time.time - lastPopupTime < popupSumWindow)
        {
            popupTotal += amount;
            popup.Refresh();
        }
        else
        {
            // beside the core, not inside it: the model hides anything spawned in its middle
            Vector3 start = transform.position + popupOffset;
            popup = Instantiate(DamagePopup, start, Quaternion.identity).GetComponent<DamagePopup>();
            popup.TargetPosition = start + Vector3.up * 1f;
            popupTotal = amount;
        }
        popup.PopupDisplayContent = StatNames.Compact(Mathf.CeilToInt(popupTotal));
        lastPopupTime = Time.time;

        if (!destroyed) AudioManager.Instance.Play(Sfx.CoreHit);   // the killing hit plays CoreDestroyed instead
    }

    public void TakeBulletDamage(float amount) => TakeDamage(amount * bulletDamageMultiplier);
}
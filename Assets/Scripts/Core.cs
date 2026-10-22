using UnityEngine;

public class Core : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float baseHealth = 200f;
    [SerializeField] private float perFloor = 0.35f;     
    [SerializeField] private float perMinute = 0.05f;   
    [SerializeField] private float bulletDamageMultiplier = 0.1f;
    [SerializeField] private GameObject DamagePopup;
    [SerializeField] private float popupSumWindow = 0.5f;   // hits closer together than this add onto the same number
    private DamagePopup popup;
    private float popupTotal, lastPopupTime;
    private float health;
    private bool destroyed;
    private GameController game;

    void Awake()
    {
        game = FindAnyObjectByType<GameController>();
        var clock = FindAnyObjectByType<RunClock>();
        maxHealth = baseHealth * (1 + perFloor * (game.Floor - 1)) * (1 + perMinute * clock.Elapsed / 60f);
        health = maxHealth;
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
            popup = Instantiate(DamagePopup, transform.position, Quaternion.identity).GetComponent<DamagePopup>();
            popup.TargetPosition = transform.position + new Vector3(0, 1.5f, 0);
            popupTotal = amount;
        }
        popup.PopupDisplayContent = Mathf.CeilToInt(popupTotal).ToString();
        lastPopupTime = Time.time;

        if (!destroyed) AudioManager.Instance.Play(Sfx.CoreHit);   // the killing hit plays CoreDestroyed instead
    }

    public void TakeBulletDamage(float amount) => TakeDamage(amount * bulletDamageMultiplier);
}
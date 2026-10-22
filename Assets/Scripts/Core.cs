using UnityEngine;

public class Core : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private float baseHealth = 200f;
    [SerializeField] private float perFloor = 0.35f;     
    [SerializeField] private float perMinute = 0.05f;   
    [SerializeField] private float bulletDamageMultiplier = 0.1f;
    [SerializeField] private GameObject DamagePopup;
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
        GameObject spawnedPopup = Instantiate(DamagePopup, transform.position, Quaternion.identity);
        spawnedPopup.GetComponent<DamagePopup>().TargetPosition = transform.position + new Vector3(0, 1.5f, 0);
        spawnedPopup.GetComponent<DamagePopup>().PopupDisplayContent = ((int)amount).ToString();

        if (!destroyed) AudioManager.Instance.Play(Sfx.CoreHit);   // the killing hit plays CoreDestroyed instead
    }

    public void TakeBulletDamage(float amount) => TakeDamage(amount * bulletDamageMultiplier);
}
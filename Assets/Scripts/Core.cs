using UnityEngine;

public class Core : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private GameObject DamagePopup;
    private float health;
    private bool destroyed;
    private GameController game;

    void Awake()
    {
        health = maxHealth;
        game = FindAnyObjectByType<GameController>();
    }

    public void TakeDamage(float amount)
    {
        if (destroyed) return;
        health -= amount;
        if (health <= 0)
        {
            destroyed = true;
            game.NextFloor();
        }
        GameObject spawnedPopup = Instantiate(DamagePopup, transform.position, Quaternion.identity);
        spawnedPopup.GetComponent<DamagePopup>().TargetPosition = transform.position + new Vector3(0, 1.5f, 0);
        spawnedPopup.GetComponent<DamagePopup>().PopupDisplayContent = ((int)amount).ToString();
    }
}
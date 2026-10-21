using UnityEngine;

public class DamageVignette : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image image;
    [SerializeField] private float flashAlpha = 0.6f;   // how red a hit gets
    [SerializeField] private float fadeSpeed = 1.5f;     // alpha per second back to 0
    [SerializeField] private float lowHealthAlpha = 0.3f; // how red when low health
    [SerializeField] private float lowHealthThreshold = 0.3f; // threshold for low health effect

    private PlayerHealth health;
    private float alpha;

    void Awake() { health = FindAnyObjectByType<PlayerHealth>(); }
    void OnEnable() { if (health != null) health.Damaged += OnDamaged; }
    void OnDisable() { if (health != null) health.Damaged -= OnDamaged; }

    void OnDamaged(float amount)
    {
        float t = amount / health.MaxHealth * 4f;         
        alpha = Mathf.Max(alpha, Mathf.Lerp(0.3f, flashAlpha, t));
    }

    void Update()
    {
        bool lowHealth = !health.IsDead && health.Health <= health.MaxHealth * lowHealthThreshold;
        float target = lowHealth ? lowHealthAlpha : 0f;     
        alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }
}
using UnityEngine;

public class HealPopups : MonoBehaviour
{
    [SerializeField] private DamagePopup popupPrefab;   // your green prefab variant
    [SerializeField] private float interval = 0.5f;
    private PlayerHealth health;
    private float pending, timer;

    void Awake() => health = GetComponent<PlayerHealth>();
    void OnEnable() => health.Healed += OnHealed;     // subscribe/unsubscribe in pairs, like your other listeners
    void OnDisable() => health.Healed -= OnHealed;

    void OnHealed(float amount) => pending += amount;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0 && pending >= 1)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;   // spawn above the player
            DamagePopup popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity);
            popup.PopupDisplayContent = "+" + pending.ToString("0");
            AudioManager.Instance?.Play(Sfx.Heal);   // once per summed popup, not per tiny heal
            popup.TargetPosition = transform.position + Vector3.up * 2.5f;    
            pending = 0;
            timer = interval;
        }
    }
}
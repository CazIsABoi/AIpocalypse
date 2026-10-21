using UnityEngine;

public class StatText : MonoBehaviour
{
    [SerializeField] private StatType stat;
    [SerializeField] private string label;
    private TMPro.TextMeshProUGUI text;
    private PlayerStats stats;

    private void Awake()
    {
        stats = FindAnyObjectByType<PlayerStats>();
        if (stats == null)
        {
            Debug.LogError("PlayerStats not found in the scene.");
            return;
        }
        text = GetComponent<TMPro.TextMeshProUGUI>();
    }
    void OnEnable() { stats.StatsChanged += Refresh; Refresh(default); }
    void OnDisable() { stats.StatsChanged -= Refresh; }

    void Refresh(StatType _)
    {
        string value = StatNames.Format(stat, stats.Get(stat));
        if (stat == StatType.CritDamage)
        {
            value += $" ({stats.CritHitDamage:0.#} on crit)";
        }
        text.text = $"{label}: {value}";
    }
}

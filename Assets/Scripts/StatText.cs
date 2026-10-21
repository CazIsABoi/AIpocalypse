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
        text.text = $"{label}: {stats.Get(stat):0.#}";
    }
}

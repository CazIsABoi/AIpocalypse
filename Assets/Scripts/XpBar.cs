using UnityEngine;

public class XpBar : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Slider xpBarSlider;
    [SerializeField] private TMPro.TextMeshProUGUI xpText;
    PlayerStats stats;
    private void Start()
    {
        stats = FindAnyObjectByType<PlayerStats>();
        if (stats != null)
        {
            stats.XpChanged += UpdateXpText;
            UpdateXpText(stats.Xp, stats.XpToNext);
            stats.XpChanged += UpdateXpBar;
            UpdateXpBar(stats.Xp, stats.XpToNext);
        }
    }

    private void OnDestroy()
    {
        stats.XpChanged -= UpdateXpText;
        stats.XpChanged -= UpdateXpBar;
    }

    private void UpdateXpText(int currentXp, int xpToNext)
    {
        int xpPercentage = Mathf.RoundToInt((currentXp / (float)xpToNext) * 100);
        xpText.text = $"XP: {xpPercentage}%";
    }

    private void UpdateXpBar(int currentXp, int xpToNext)
    {
        xpBarSlider.maxValue = xpToNext;
        xpBarSlider.value = currentXp;
    }


}

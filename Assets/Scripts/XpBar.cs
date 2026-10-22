using UnityEngine;
using UnityEngine.UI;

public class XpBar : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Slider xpBarSlider;
    [SerializeField] private TMPro.TextMeshProUGUI xpText;
    PlayerStats stats;

    [SerializeField] private RunClock clock;
    [SerializeField] private EnemyController boss;
    [SerializeField] private Gradient bossbarColor;
    [SerializeField] private Gradient shieldColor;

    private void Start()
    {
        clock = FindAnyObjectByType<RunClock>();
        stats = FindAnyObjectByType<PlayerStats>();

        if (clock.Elapsed < 900)
        {
            if (stats != null)
            {
                stats.XpChanged += UpdateXpText;
                UpdateXpText(stats.Xp, stats.XpToNext);
                stats.XpChanged += UpdateXpBar;
                UpdateXpBar(stats.Xp, stats.XpToNext);
            }
        }
        
    }
    private void Update()
    {
        if (clock.Elapsed >= 900)
        {
            
            stats.XpChanged -= UpdateXpText;
            UpdateXpText(stats.Xp, stats.XpToNext);
            stats.XpChanged -= UpdateXpBar;
            UpdateXpBar(stats.Xp, stats.XpToNext);

            Debug.Log("Updating Stats");
            if (boss == null) return;
            xpText.text = "HP: " + boss.Health.ToString("N0") + "/" + boss.MaxHealth.ToString("N0");

            xpBarSlider.maxValue = boss.MaxHealth;
            xpBarSlider.value = boss.Health;
            if(!boss.ProtectiveBarrierActive) xpBarSlider.fillRect.GetComponent<Image>().color = bossbarColor.Evaluate(boss.Health / boss.MaxHealth);
            else xpBarSlider.fillRect.GetComponent<Image>().color = shieldColor.Evaluate(boss.Health / boss.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (stats == null) return;
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

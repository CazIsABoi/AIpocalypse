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

    private bool showingBoss;
    private Color xpColor;   // the normal fill color, restored after the boss bar

    private void Start()
    {
        clock = FindAnyObjectByType<RunClock>();
        stats = FindAnyObjectByType<PlayerStats>();
        xpColor = xpBarSlider.fillRect.GetComponent<Image>().color;
        ShowXp(true);
    }

    // Subscribe/unsubscribe once when the mode changes, not every frame
    void ShowXp(bool on)
    {
        if (stats == null) return;
        stats.XpChanged -= UpdateXpText;
        stats.XpChanged -= UpdateXpBar;
        if (!on) return;
        xpBarSlider.fillRect.GetComponent<Image>().color = xpColor;
        stats.XpChanged += UpdateXpText;
        stats.XpChanged += UpdateXpBar;
        UpdateXpText(stats.Xp, stats.XpToNext);
        UpdateXpBar(stats.Xp, stats.XpToNext);
    }

    private void Update()
    {
        // boss bar only while the fight is on: after the boss dies (endless) it's the XP bar again
        bool bossFight = clock.BossActive && !clock.Endless && boss != null;
        if (bossFight != showingBoss)
        {
            showingBoss = bossFight;
            ShowXp(!bossFight);
        }

        if (bossFight)
        {
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

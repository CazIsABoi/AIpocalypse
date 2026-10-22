using UnityEngine;

public class RunTimerUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI text;
    [SerializeField] private RunClock clock;
    private UnityEngine.UI.Slider slider;
    [SerializeField] private float timerWarningAt = 60f;   // seconds left: "one minute" warning
    [SerializeField] private float bossWarningAt = 8f;     // seconds left: BossWarning (the clip is 8 s, so it ends as the boss arrives)
    private float lastTimeLeft = float.MaxValue;

    private void Awake()
    {
        slider = GetComponent<UnityEngine.UI.Slider>();
    }
    private void Update()
    {
        if (!clock.Endless)
        {
            slider.value = 1f - clock.Progress;              // full → empty (slider Min 0, Max 1)
            int m = Mathf.FloorToInt(clock.TimeLeft / 60f);
            int s = Mathf.FloorToInt(clock.TimeLeft % 60f);
            text.text = $"{m:00}:{s:00}";                    // "14:05"

            // play each warning once, the frame the countdown crosses its mark
            if (lastTimeLeft > timerWarningAt && clock.TimeLeft <= timerWarningAt) AudioManager.Instance?.Play(Sfx.TimerWarning);
            if (lastTimeLeft > bossWarningAt && clock.TimeLeft <= bossWarningAt) AudioManager.Instance?.Play(Sfx.BossWarning);
            lastTimeLeft = clock.TimeLeft;
        }
        else
        {
            // hide slider in endless mode
            slider.fillRect.gameObject.SetActive(false);
            int m = Mathf.FloorToInt(clock.EndlessTime / 60f);
            int s = Mathf.FloorToInt(clock.EndlessTime % 60f);
            text.text = $"ENDLESS: {m:00}:{s:00}";                    // "ENDLESS: 14:05"
        }
    }
}

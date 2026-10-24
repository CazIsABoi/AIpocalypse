using UnityEngine;

public class RunTimerUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI text;
    [SerializeField] private RunClock clock;
    private UnityEngine.UI.Slider slider;
    [SerializeField] private float timerWarningAt = 60f;   // seconds left: "one minute" warning
    [SerializeField] private float bossWarningAt = 8f;     // seconds left: BossWarning (the clip is 8 s, so it ends as the boss arrives)
    private float lastTimeLeft = float.MaxValue;

    [Header("Endless threat")]
    [SerializeField] private float popScale = 1.3f;    // how big the text pops each time THREAT goes up by 0.1
    [SerializeField] private float popTime = 0.25f;
    private EntityHandler spawner;
    private int lastThreatStep;
    private float popTimer;
    private Vector3 textBaseScale;

    private void Awake()
    {
        slider = GetComponent<UnityEngine.UI.Slider>();
        spawner = FindAnyObjectByType<EntityHandler>();
        textBaseScale = text.rectTransform.localScale;
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
            float threat = spawner != null ? spawner.Threat : 1f;
            text.text = $"ENDLESS: {m:00}:{s:00}   <color=#FF6464>THREAT x{threat:0.0}</color>";

            // every +0.1 threat: a quick pop so the escalation is felt
            int step = Mathf.FloorToInt(threat * 10f);
            if (step > lastThreatStep)
            {
                if (lastThreatStep > 0) popTimer = popTime;   // not on the very first frame of endless
                lastThreatStep = step;
            }
        }

        // pop: jump big, ease back to normal (unscaled, so it still finishes during pauses)
        if (popTimer > 0f)
        {
            popTimer -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(popTimer / popTime);            // 1 → 0
            text.rectTransform.localScale = textBaseScale * Mathf.Lerp(1f, popScale, k * k);
        }
        else text.rectTransform.localScale = textBaseScale;
    }
}

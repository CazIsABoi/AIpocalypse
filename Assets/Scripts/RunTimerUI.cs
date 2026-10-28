using UnityEngine;

public class RunTimerUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI text;
    [SerializeField] private RunClock clock;
    [SerializeField] private VitalLine vitals;
    [SerializeField] private TMPro.TextMeshProUGUI detail;    // small line under the threat: "LINGERING x1.40"
    [SerializeField] private float detailInterval = 0.25f;
    private float detailTimer;
    private UnityEngine.UI.Slider slider;
    [System.Serializable] public struct ThreatTier { public float from; public string label; public Color color; }
    [SerializeField] private ThreatTier[] tiers;   

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
        float threat = spawner != null ? spawner.Threat : 1f;
        int threatIndex = 0;
        if (tiers.Length == 0) return;
        ThreatTier currentTier = tiers[0];
        for (int i = 0; i < tiers.Length; i++)
        {
            if (threat >= tiers[i].from) { currentTier = tiers[i]; threatIndex = i; }
            else break;
        }

        slider.value = tiers.Length > 1 ? Mathf.InverseLerp(currentTier.from, tiers[Mathf.Min(tiers.Length - 1, threatIndex + 1)].from, threat) : 1f;
        slider.fillRect.GetComponent<UnityEngine.UI.Image>().color = new Color(currentTier.color.r, currentTier.color.g, currentTier.color.b, 0.22f);

        int m = Mathf.FloorToInt(clock.Elapsed / 60f);
        int s = Mathf.FloorToInt(clock.Elapsed % 60f);
        text.text = $"TIME: {m:00}:{s:00}   <color=#{ColorUtility.ToHtmlStringRGB(currentTier.color)}>{currentTier.label}</color> x{threat:0.0}"; 
        if (vitals != null) vitals.Show(threatIndex, currentTier.color, threat);
        UpdateDetail();

        int step = Mathf.FloorToInt(Mathf.Log(threat) / Mathf.Log(1.1f));
        if (step > lastThreatStep)
        {
            if (lastThreatStep > 0) popTimer = popTime;   
            lastThreatStep = step;
        }

        if (popTimer > 0f)
        {
            popTimer -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(popTimer / popTime);            // 1 → 0
            text.rectTransform.localScale = textBaseScale * Mathf.Lerp(1f, popScale, k * k);
        }
        else text.rectTransform.localScale = textBaseScale;
    }

    void UpdateDetail()
    {
        if (detail == null) return;
        detailTimer -= Time.unscaledDeltaTime;
        if (detailTimer > 0f) return;
        detailTimer = detailInterval;

        float stay = spawner != null ? spawner.Stay : 1f;
        detail.text = stay > 1.005f ? $"<color={StatNames.Bad}>LINGERING x{stay:0.00}</color>" : "";
    }
}

using UnityEngine;

public class RunTimerUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI text;
    [SerializeField] private RunClock clock;
    private UnityEngine.UI.Slider slider;

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

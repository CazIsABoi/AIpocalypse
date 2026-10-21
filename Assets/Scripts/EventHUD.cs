using UnityEngine;

public class EventHUD : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI eventText;
    [SerializeField] private TMPro.TextMeshProUGUI eventTimerText;
    [SerializeField] private EventDirector director;

    // Update is called once per frame
    void Update()
    {
        if (director.Current != null)
        {
            eventText.text = director.Current.Title;
            int m = Mathf.FloorToInt(director.EventTimeLeft / 60f);
            int s = Mathf.FloorToInt(director.EventTimeLeft % 60f);
            eventTimerText.text = $"{m:00}:{s:00}";
        }
        else
        {
            eventText.text = "";
            int m = Mathf.FloorToInt(director.NextEventIn / 60f);
            int s = Mathf.FloorToInt(director.NextEventIn % 60f);
            eventTimerText.text = $"Next event in: {m:00}:{s:00}";
        }
    }
}

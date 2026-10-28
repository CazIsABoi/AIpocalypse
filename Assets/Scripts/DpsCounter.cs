using TMPro;
using UnityEngine;

// Shows the player's damage per second (DpsMeter). Put it on any TextMeshPro text in the HUD.
// Hidden while "Show DPS" is off in the settings.
[RequireComponent(typeof(TMP_Text))]
public class DpsCounter : MonoBehaviour
{
    [SerializeField] private string format = "DPS {0}";   // {0} = the number, e.g. "1.2M"
    [SerializeField] private float interval = 0.25f;      // DPS changes every hit: refresh a few times a second so it's readable

    private TMP_Text text;
    private float timer;

    void Awake() => text = GetComponent<TMP_Text>();

    void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = interval;

        text.enabled = GameSettings.ShowDps;
        if (text.enabled) text.text = string.Format(format, StatNames.Compact(DpsMeter.Current));
    }
}

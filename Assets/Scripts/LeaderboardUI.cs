using TMPro;
using UnityEngine;

// Main menu: shows the online top 10 and lets the player pick the name their runs are sent with.
public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text list;            // one text, the rows are lines
    [SerializeField] private TMP_InputField nameInput; // optional
    [SerializeField] private int count = 10;

    void Start()
    {
        if (nameInput != null)
        {
            nameInput.characterLimit = 16;
            nameInput.text = GameSettings.PlayerName;
            nameInput.onEndEdit.AddListener(v => GameSettings.PlayerName = v.Trim());
        }
        Refresh();
    }

    public void Refresh()   // also usable from a "Refresh" button
    {
        if (!ScoreService.Configured) { list.text = "Leaderboard offline"; return; }
        list.text = "Loading...";
        ScoreService.FetchTop(count, entries =>
        {
            if (this == null) return;   // menu was left before the reply came back
            if (entries == null) { list.text = "Couldn't load leaderboard"; return; }
            if (entries.Length == 0) { list.text = "No runs yet. Be the first!"; return; }

            string me = GameSettings.PlayerName;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                string row = $"{i + 1}.<pos=10%>{e.name}<pos=48%>{e.score}<pos=68%>F{e.floor}<pos=80%>{ScoreService.FormatTime(e.seconds)}";
                // highlight your own runs in the game's green
                if (!string.IsNullOrEmpty(me) && e.name == me) row = $"<color=#00FF41>{row}</color>";
                sb.AppendLine(row);
            }
            list.text = sb.ToString();
        });
    }
}

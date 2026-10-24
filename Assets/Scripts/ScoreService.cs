using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Talks to the Google Apps Script leaderboard 
public static class ScoreService
{
    public const string Url = "https://script.google.com/macros/s/AKfycbwPAf1o5esUizkuuq7_zZWCPuFzEW1zuWrchC0J_4xO89RML3pfub6vIDbxiJyGSlxvXQ/exec";
    public static bool Configured => Url.StartsWith("https://");

    [Serializable]
    public class Run
    {
        public string name;
        public float seconds;
        public int floor, level, kills;
        public float threat, gunHit, meleeHit;
        public string version, platform;
        public string killer;        // what dealt the last hit
        public int coreFound;        // 1 = they got near this floor's Core
        public float floorTime;      // seconds spent on the floor they died on
    }

    [Serializable] public class Entry { public string name; public float seconds; public int floor; public float threat; }
    [Serializable] class Board { public Entry[] entries; }

    public static void Submit(Run run)
    {
        if (!Configured || !GameSettings.SubmitScores) return;
        run.version = Application.version;
        run.platform = Application.platform.ToString();

        var req = new UnityWebRequest(Url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(run)));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "text/plain;charset=utf-8");
        req.SendWebRequest().completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success) Debug.LogWarning($"Score submit failed: {req.error}");
            req.Dispose();
        };
    }

    public static void FetchTop(int count, Action<Entry[]> done)
    {
        if (!Configured) { done(null); return; }
        var req = UnityWebRequest.Get($"{Url}?n={count}");
        req.SendWebRequest().completed += _ =>
        {
            Entry[] entries = null;
            if (req.result == UnityWebRequest.Result.Success)
            {
                try { entries = JsonUtility.FromJson<Board>(req.downloadHandler.text)?.entries; }
                catch (Exception e) { Debug.LogWarning($"Leaderboard parse failed: {e.Message}"); }
            }
            else Debug.LogWarning($"Leaderboard fetch failed: {req.error}");
            req.Dispose();
            done(entries);
        };
    }

    // "1:23:45" or "23:45"
    public static string FormatTime(float seconds)
    {
        int t = Mathf.FloorToInt(seconds);
        return t >= 3600 ? $"{t / 3600}:{t / 60 % 60:00}:{t % 60:00}" : $"{t / 60:00}:{t % 60:00}";
    }
}

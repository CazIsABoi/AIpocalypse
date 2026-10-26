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

        // Everything below is filled by RunTelemetry. The sheet adds a column for any new field by itself.
        public string playerId;      // random per install, not personal
        public int runNumber;        // this player's 1st, 2nd... run
        public string endReason;     // death / quit / closed
        public int bossReached, bossWon;
        public float endlessSeconds;
        public string floorTimes;    // seconds per floor, "45|62|80"

        public float damageGun, damageMelee, damageTaken, healed, healedOrbs;
        public int hitsTaken, lowestHpPct, chests, healthOrbs;

        public int mask;
        public string cardsPicked;   // in order, "Turbo Mode|Hollow Points"
        public string cardsOffered;  // what each pick was chosen from, "A,B,C|D,E,F"
        public string pickSources;   // Level / ChestPool / CorePool..., one per pick
        public string effects;       // "ChainLightning:2|Ricochet:1"
        public float maxHp, damage, attackSpeed, critChance, critDamage, lifesteal, moveSpeed;
        public int bullets, pierce;
        public float regen, pickupRange, projectileSpeed, meleeRange;

        public int thoughtsEarned, thoughtsTotal, recalls;
        public string ideas;         // unlocked Ideas before this run's recall
        public string loadout;       // the Ideas equipped for this run

        public float avgFps, worstFps;
        public string resolution, gpu;
        public int fullscreen, ramMb, gunSounds, hitSounds;
    }

    // Sent after the death screen recall, so it gets its own sheet ("recall")
    [Serializable]
    public class RecallEvent
    {
        public string @event = "recall";
        public string playerId, idea, choices, version, platform;
        public int recallNumber, cost, thoughtsLeft;
    }

    [Serializable] public class Entry { public string name; public float seconds; public int floor; public float threat; }
    [Serializable] class Board { public Entry[] entries; }

    public static void Submit(Run run)
    {
        run.version = Application.version;
        run.platform = Application.platform.ToString();
        Post(JsonUtility.ToJson(run));
    }

    public static void SubmitRecall(RecallEvent e)
    {
        e.version = Application.version;
        e.platform = Application.platform.ToString();
        e.playerId = RunTelemetry.PlayerId;
        Post(JsonUtility.ToJson(e));
    }

    static void Post(string json)
    {
        if (!Configured || !GameSettings.SubmitScores) return;
        var req = new UnityWebRequest(Url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
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

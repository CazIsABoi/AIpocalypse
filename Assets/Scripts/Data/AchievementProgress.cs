using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// The saved side of achievements: which are unlocked, and the lifetime totals/bests they count toward.
// The current run's numbers live in AchievementTracker and are added here when the run ends.
[AutoStaticsCleanup]
static partial class AchievementProgress
{
    [System.Serializable]
    class Data
    {
        public List<string> unlocked = new List<string>();
        public List<float> stats = new List<float>();   // indexed by AchievementStat (JsonUtility can't save dictionaries)
    }

    static Data data;
    static Data D { get { if (data == null) data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString("achievements", "{}")); return data; } }
    static void Save() { PlayerPrefs.SetString("achievements", JsonUtility.ToJson(D)); PlayerPrefs.Save(); }

    public static IReadOnlyList<string> Unlocked => D.unlocked;
    public static bool IsUnlocked(AchievementData a) => D.unlocked.Contains(a.Id);

    public static float Lifetime(AchievementStat stat)
    {
        int i = (int)stat;
        return i < D.stats.Count ? D.stats[i] : 0f;
    }

    // What counts toward an achievement right now: this run alone, or this run on top of every earlier run
    public static float Value(AchievementData a, float[] run)
    {
        float current = run[(int)a.Stat];
        if (a.Scope == AchievementScope.SingleRun) return current;
        return AchievementData.IsBest(a.Stat) ? Mathf.Max(Lifetime(a.Stat), current) : Lifetime(a.Stat) + current;
    }

    // Marks it unlocked (once), hands out its Idea and tells Steam. Returns false if it was already unlocked.
    public static bool Unlock(AchievementData a)
    {
        if (IsUnlocked(a)) return false;
        D.unlocked.Add(a.Id);
        Save();
        if (a.Reward != null) Progress.Grant(a.Reward);
        SteamAchievements.Unlock(a.Id);
        return true;
    }

    // End of a run: add its counters to the totals, keep the better of its "best" stats
    public static void AddRun(float[] run)
    {
        while (D.stats.Count < run.Length) D.stats.Add(0f);
        for (int i = 0; i < run.Length; i++)
        {
            if (AchievementData.IsBest((AchievementStat)i)) D.stats[i] = Mathf.Max(D.stats[i], run[i]);
            else D.stats[i] += run[i];
        }
        Save();
    }

    public static void ResetAll() { PlayerPrefs.DeleteKey("achievements"); PlayerPrefs.Save(); data = null; }
}

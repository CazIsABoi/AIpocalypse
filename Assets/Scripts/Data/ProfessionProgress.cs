using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// The permanent side of professions: XP per profession and which perk nodes have been bought.
// Saved like Progress. Which bought nodes are switched ON for a run is RunProfession's job.
[AutoStaticsCleanup]
static partial class ProfessionProgress
{
    [System.Serializable]
    class ProfessionSave
    {
        public string id;                                   // ProfessionData.Id
        public int xp;                                      // all XP ever earned with it (never goes down)
        public List<string> nodes = new List<string>();     // Ids of the bought PerkNodes
    }

    // JsonUtility can't save a Dictionary, so it's a list of ProfessionSaves
    [System.Serializable]
    class Data { public List<ProfessionSave> professions = new List<ProfessionSave>(); }

    static Data data;
    static Data D { get { if (data == null) data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString("professions", "{}")); return data; } }
    static void Save() { PlayerPrefs.SetString("professions", JsonUtility.ToJson(D)); PlayerPrefs.Save(); }

    // This profession's save data, created empty the first time it's asked for
    static ProfessionSave Entry(ProfessionData profession)
    {
        var entry = D.professions.Find(p => p.id == profession.Id);
        if (entry == null)
        {
            entry = new ProfessionSave { id = profession.Id };
            D.professions.Add(entry);
        }
        return entry;
    }

    public static int Xp(ProfessionData profession) => Entry(profession).xp;

    // ---------- levels ----------

    // XP needed to go from 'level' to 'level + 1': XpPerLevel at level 1, then XpGrowth (15%) more each level.
    // With XpPerLevel 100: 1->2 = 100, 2->3 = 115, 3->4 = 132...
    public static int XpForLevel(ProfessionData profession, int level)
        => Mathf.RoundToInt(profession.XpPerLevel * Mathf.Pow(profession.XpGrowth, level - 1));

    // Climbs the levels like stairs: pay for each step out of the total XP until the next one is too expensive.
    // 0 xp = level 1, 100 = level 2, 214 = level 2, 215 = level 3.
    public static int Level(ProfessionData profession)
    {
        int xp = Xp(profession);
        int level = 1;
        while (level < profession.MaxLevel && xp >= XpForLevel(profession, level))
        {
            xp -= XpForLevel(profession, level);
            level++;
        }
        return level;
    }

    // How far into the current level the player is (0..1), for the XP bar. Same stairs, then look at what's left.
    public static float LevelProgress(ProfessionData profession)
    {
        int xp = Xp(profession);
        int level = 1;
        while (level < profession.MaxLevel && xp >= XpForLevel(profession, level))
        {
            xp -= XpForLevel(profession, level);
            level++;
        }
        if (level >= profession.MaxLevel) return 1f;   // maxed: full bar
        return (float)xp / XpForLevel(profession, level);
    }

    // Adds a run's XP and saves. Returns how many levels it went up (for a "LEVEL UP!" message).
    public static int AddXp(ProfessionData profession, int amount)
    {
        int before = Level(profession);
        Entry(profession).xp += amount;
        Save();
        return Level(profession) - before;
    }

    // ---------- points ----------

    // One point per level above 1: a brand-new profession (level 1) has none to spend
    public static int PointsEarned(ProfessionData profession) => Level(profession) - 1;

    // The Cost of every bought node added up
    public static int PointsSpent(ProfessionData profession)
    {
        int totalSpent = 0;
        var bought = Entry(profession).nodes;
        foreach (var node in profession.Nodes)
        {
            if (node != null && bought.Contains(node.Id)) totalSpent += node.Cost;
        }
        return totalSpent;
    }

    public static int PointsLeft(ProfessionData profession) => PointsEarned(profession) - PointsSpent(profession);

    // ---------- buying nodes ----------

    // Bought = in the save, or the free root (every profession starts with its signature node)
    public static bool IsBought(ProfessionData profession, PerkNode node)
    {
        if (profession == null || node == null) return false;
        if (node.Tier == 0 && node.Cost == 0) return true;
        return Entry(profession).nodes.Contains(node.Id);
    }

    // Can the player buy it right now? Not owned yet, affordable, and everything below it is owned.
    public static bool CanBuy(ProfessionData profession, PerkNode node)
    {
        if (IsBought(profession, node)) return false;
        if (PointsLeft(profession) < node.Cost) return false;
        foreach (var required in node.Requires)
        {
            if (!IsBought(profession, required)) return false;
        }
        return true;
    }

    // Buys the node if allowed (checked again here: never trust the button) and saves
    public static bool Buy(ProfessionData profession, PerkNode node)
    {
        if (!CanBuy(profession, node)) return false;
        Entry(profession).nodes.Add(node.Id);
        Save();
        return true;
    }

    // Respec: forget the bought nodes (XP and level stay)
    public static void Refund(ProfessionData profession)
    {
        Entry(profession).nodes.Clear();
        Save();
    }

    // Debug: wipe every profession
    public static void ResetAll() { PlayerPrefs.DeleteKey("professions"); PlayerPrefs.Save(); data = null; }
}

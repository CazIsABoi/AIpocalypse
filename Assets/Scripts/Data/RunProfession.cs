using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// The per-run side of professions (like RunLoadout for Ideas): which profession Wrench plays, and which
// MAJOR node is switched on at each tier. Majors on the same tier are exclusive; minors simply follow the
// branch they sit on. Chosen in the Run Picker, remembered, reused by Quick Start.
[AutoStaticsCleanup]
static partial class RunProfession
{
    [System.Serializable]
    class Data
    {
        public string profession = "";                       // ProfessionData.Id ("" = never chosen)
        public List<string> majors = new List<string>();     // Ids of the chosen majors (at most one per tier)
    }

    static Data data;
    static Data D { get { if (data == null) data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString("run_profession", "{}")); return data; } }
    static void Save() { PlayerPrefs.SetString("run_profession", JsonUtility.ToJson(D)); PlayerPrefs.Save(); }
    public static void ResetAll() { PlayerPrefs.DeleteKey("run_profession"); PlayerPrefs.Save(); data = null; }

    public static string ProfessionId => D.profession;
    public static void SetProfession(ProfessionData profession) { D.profession = profession != null ? profession.Id : ""; Save(); }

    public static bool IsChosen(PerkNode major) => D.majors.Contains(major.Id);

    // Makes this major the active one of its tier: un-picks any other major on the same tier first.
    // (D.majors only holds Ids, so the tier is looked up on the PerkNode assets.)
    public static void Choose(ProfessionData profession, PerkNode major)
    {
        for (int i = D.majors.Count - 1; i >= 0; i--)   // backwards, so RemoveAt doesn't skip the next one
        {
            var otherMajorId = D.majors[i];
            var otherMajor = profession.Nodes.Find(n => n.Id == otherMajorId);
            if (otherMajor != null && otherMajor.IsMajor && otherMajor.Tier == major.Tier)
            {
                D.majors.RemoveAt(i);
            }
        }
        if (!D.majors.Contains(major.Id))
        {
            D.majors.Add(major.Id);
        }
        Save();
    }

    // Is this node switched on for the run? It must be bought, chosen (if it's a major), and everything it
    // requires must be active too. That last check calls IsActive again on the nodes below (recursion),
    // so picking the other major on a tier switches off the whole branch above the unpicked one.
    public static bool IsActive(ProfessionData profession, PerkNode node)
    {
        if (!ProfessionProgress.IsBought(profession, node)) return false;
        if (node.IsMajor && !IsChosen(node)) return false;
        for (int i = 0; i < node.Requires.Count; i++)
        {
            if (!IsActive(profession, node.Requires[i])) return false;
        }
        return true;
    }

    // Every active node of the profession: what ProfessionApplier puts on the player at the start of a run
    public static List<PerkNode> ActiveNodes(ProfessionData profession)
    {
        var active = new List<PerkNode>();
        for (int i = 0; i < profession.Nodes.Count; i++)
        {
            var node = profession.Nodes[i];
            if (IsActive(profession, node))
                active.Add(node);
        }
        return active;
    }
}

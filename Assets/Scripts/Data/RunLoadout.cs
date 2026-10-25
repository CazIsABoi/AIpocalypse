using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// What Wrench brings into a run: up to MaxSlots Ideas. Chosen in the Run Picker, remembered between
// sessions, and used by Quick Start as-is. Cards that start unlocked are always in; locked cards only
// show up if one of the equipped Ideas contains them.
[AutoStaticsCleanup]
static partial class RunLoadout
{
    public const int MaxSlots = 3;

    [System.Serializable]
    class Data
    {
        public bool chosen;                                   // false = never opened the picker (old saves: everything unlocked is in)
        public List<string> ideas = new List<string>();       // equipped Idea Ids
        public List<string> cards = new List<string>();       // the card Ids those Ideas add (so MainScene needs no IdeaSet)
    }

    static Data data;
    static Data D { get { if (data == null) data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString("loadout", "{}")); return data; } }

    public static IReadOnlyList<string> EquippedIdeas => D.ideas;
    public static bool IsEquipped(IdeaData idea) => D.ideas.Contains(idea.Id);
    public static string Describe => string.Join("|", D.ideas);   // telemetry

    // Can this card show up in this run? (Progress.IsUnlocked is checked separately)
    public static bool Allows(RewardData card) => !D.chosen || card.StartsUnlocked || D.cards.Contains(card.Id);

    public static void Set(List<IdeaData> ideas)
    {
        D.chosen = true;
        D.ideas.Clear();
        D.cards.Clear();
        foreach (var idea in ideas)
        {
            if (idea == null || D.ideas.Count >= MaxSlots) continue;
            D.ideas.Add(idea.Id);
            foreach (var card in idea.Cards) if (card != null && !D.cards.Contains(card.Id)) D.cards.Add(card.Id);
        }
        PlayerPrefs.SetString("loadout", JsonUtility.ToJson(D));
        PlayerPrefs.Save();
    }
}

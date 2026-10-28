using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// What Wrench brings into a run: up to MaxSlots Ideas. Chosen in the Run Picker, remembered between
// sessions, and used by Quick Start as-is. Cards that start unlocked are always in; locked cards only
// show up if one of the equipped Ideas contains them.
[AutoStaticsCleanup]
static partial class RunLoadout
{
    public const int BaseSlots = 3;
    public static int MaxSlots => BaseSlots + Progress.ExtraSlots;

    [System.Serializable]
    class Data
    {
        public bool chosen;                                   // false = never opened the picker (old saves: everything unlocked is in)
        public List<string> ideas = new List<string>();       // equipped Idea Ids
        public List<string> cards = new List<string>();       // the card Ids those Ideas add (so MainScene needs no IdeaSet)
        public List<string> poolCards = new List<string>();   // card Ids of recalled AlwaysInPool Ideas: in every run
    }

    static Data data;
    static Data D { get { if (data == null) data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString("loadout", "{}")); return data; } }

    public static IReadOnlyList<string> EquippedIdeas => D.ideas;
    public static bool IsEquipped(IdeaData idea) => D.ideas.Contains(idea.Id);
    public static string Describe => string.Join("|", D.ideas);   // telemetry

    // Can this card show up in this run? (Progress.IsUnlocked is checked separately)
    public static bool Allows(RewardData card) => !D.chosen || card.StartsUnlocked || D.cards.Contains(card.Id) || D.poolCards.Contains(card.Id);

    // equipped = the picked Ideas; allIdeas = every Idea, to collect the recalled AlwaysInPool ones
    public static void Set(List<IdeaData> equipped, List<IdeaData> allIdeas)
    {
        D.chosen = true;
        D.ideas.Clear();
        D.cards.Clear();
        D.poolCards.Clear();
        foreach (var idea in equipped)
        {
            if (idea == null || idea.AlwaysInPool || D.ideas.Count >= MaxSlots) continue;
            D.ideas.Add(idea.Id);
            foreach (var card in idea.Cards) if (card != null && !D.cards.Contains(card.Id)) D.cards.Add(card.Id);
        }
        foreach (var idea in allIdeas)
        {
            if (idea != null && idea.AlwaysInPool && Progress.IsUnlocked(idea)) AddPoolCards(idea);
        }
        Save();
    }

    // Called on recall, so the new Idea counts right away (Quick Start never opens the picker):
    // a pool Idea joins the pool, any other is equipped if a slot is still free
    public static void OnRecalled(IdeaData idea)
    {
        if (idea.AlwaysInPool) AddPoolCards(idea);
        else if (D.chosen && D.ideas.Count < MaxSlots && !D.ideas.Contains(idea.Id))
        {
            D.ideas.Add(idea.Id);
            foreach (var card in idea.Cards) if (card != null && !D.cards.Contains(card.Id)) D.cards.Add(card.Id);
        }
        Save();
    }

    public static void ResetAll() { PlayerPrefs.DeleteKey("loadout"); PlayerPrefs.Save(); data = null; }

    static void AddPoolCards(IdeaData idea)
    {
        foreach (var card in idea.Cards) if (card != null && !D.poolCards.Contains(card.Id)) D.poolCards.Add(card.Id);
    }

    static void Save()
    {
        PlayerPrefs.SetString("loadout", JsonUtility.ToJson(D));
        PlayerPrefs.Save();
    }
}

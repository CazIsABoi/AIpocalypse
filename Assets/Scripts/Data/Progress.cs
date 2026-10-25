using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[AutoStaticsCleanup]
static partial class Progress
{
    [System.Serializable]
    class Data
    {
        public int thoughts;
        public List<string> unlockedCards = new List<string>();
        public List<string> unlockedIdeas = new List<string>();
        public int recalls;
    }

    static Data data;
    static Data D { get { if (data == null) { data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString("ProgressData", "{}")); } return data; } }


    public static int Thoughts => D.thoughts;
    public static void AddThoughts(int amount) { D.thoughts += amount; Save(); }
    public static bool IsUnlocked(RewardData card) { if (card.StartsUnlocked || D.unlockedCards.Contains(card.Id)) { return true; } return false; }
    public static void Unlock(RewardData card) {D.unlockedCards.Add(card.Id); Save(); }
    static void Save() { PlayerPrefs.SetString("ProgressData", JsonUtility.ToJson(D)); PlayerPrefs.Save(); }
    public static int UnlockedCount => D.unlockedCards.Count;

    // debug only (UnlockDebug): back to a brand-new save, also clears the cached data
    public static void ResetAll() { PlayerPrefs.DeleteKey("ProgressData"); PlayerPrefs.Save(); data = null; }
    public static RewardData DrawLocked(RewardPool allCards)
    {
        List<RewardData> lockedCards = new List<RewardData>();
        foreach (var card in allCards.Rewards)
        {
            if (!IsUnlocked(card))
            {
                lockedCards.Add(card);
            }
        }
        if (lockedCards.Count == 0)
        {
            Debug.LogWarning("All cards are unlocked! No locked card to draw.");
            return null; // or handle this case as needed
        }
        RewardData drawnCard = lockedCards[Random.Range(0, lockedCards.Count)];
        Unlock(drawnCard);
        return drawnCard;
    }
    public static RewardData BuyDraw(RewardPool allCards, int cost)
    {
        bool success = true;
        if (Thoughts < cost)
        {
            Debug.LogWarning("Not enough Thoughts to buy a draw.");
            success = false;
        }
        if (allCards.Rewards.Count == 0)
        {
            Debug.LogWarning("No cards available in the pool to draw.");
            success = false;
        }
        if (IsUnlocked(allCards.Rewards[0]) && allCards.Rewards.TrueForAll(card => IsUnlocked(card)))
        {
            Debug.LogWarning("All cards are already unlocked. Cannot draw a new card.");
            success = false;
        }
        if (!success)
        {
            return null; // or handle this case as needed
        }
        AddThoughts(-cost);
        return DrawLocked(allCards);
    }
    public static int Recalls => D.recalls;
    public static string UnlockedIdeaList => string.Join("|", D.unlockedIdeas);   // telemetry
    // The very first death always unlocks something: the first recall costs what the player has,
    // rounded down to a ten (14 -> 10, 37 -> 30), so the bar looks like it filled up by itself
    public static int RecallCost => D.recalls == 0 ? FirstRecallCost : 80 + ((D.recalls - 1) * 40);
    static int FirstRecallCost => Thoughts >= 10 ? Thoughts / 10 * 10 : Mathf.Max(1, Thoughts);

    public static bool IsUnlocked(IdeaData idea)
    {
        return D.unlockedIdeas.Contains(idea.Id);
    }

    public static bool CanRecall(IdeaSet ideas)
    {
        bool hasEnoughThoughts = Thoughts >= RecallCost;
        bool hasLockedIdeas = ideas.Ideas.Exists(idea => !IsUnlocked(idea));
        return hasEnoughThoughts && hasLockedIdeas;
    }

    public static List<IdeaData> RecallChoices(IdeaSet ideas, int count)
    {
        List<IdeaData> locked = ideas.Ideas.FindAll(idea => idea != null && !IsUnlocked(idea));
        List<IdeaData> choices = new List<IdeaData>();
        while (choices.Count < count && locked.Count > 0)
        {
            int i = Random.Range(0, locked.Count);
            choices.Add(locked[i]);
            locked.RemoveAt(i);
        }
        return choices;
    }

    public static void Recall(IdeaData idea)
    {
        if (IsUnlocked(idea) || Thoughts < RecallCost) return;

        D.thoughts -= RecallCost;
        D.recalls++;
        D.unlockedIdeas.Add(idea.Id);
        foreach (var card in idea.Cards)
        {
            if (card != null && !D.unlockedCards.Contains(card.Id)) D.unlockedCards.Add(card.Id);
        }
        Save();
    }
}

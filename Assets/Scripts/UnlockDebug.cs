using System.Collections.Generic;
using UnityEngine;

// Test helper for meta-progression. Put it on any active object in the scene, enter Play mode,
// then use the component's ⋮ menu (top right of the component in the Inspector) to run these.
public class UnlockDebug : MonoBehaviour
{
    [SerializeField] private IdeaSet ideas;
    [SerializeField] private RewardPool allCards;
    [SerializeField] private RecallSequence recall;
    [SerializeField] private IdeaPicker picker;
    [SerializeField] private UnlockScreen unlockScreen;
    [SerializeField] private int testCardCount = 5;    // > 3 to test the paging arrows
    [SerializeField] private int thoughtsToAdd = 100;

    [ContextMenu("Play end-of-run sequence (adds Thoughts)")]
    void PlaySequence()
    {
        Progress.AddThoughts(thoughtsToAdd);   // same order as DeathStats: save first, then show
        recall.Play(thoughtsToAdd);
    }

    [ContextMenu("Show Idea picker (unlocks nothing)")]
    void ShowPicker()
    {
        var pool = new List<IdeaData>(ideas.Ideas);   // a copy, so removing picks never touches the asset
        var test = new List<IdeaData>();
        while (test.Count < 3 && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            test.Add(pool[i]);
            pool.RemoveAt(i);
        }
        picker.Show(test, idea => Debug.Log($"Picked {idea.Title}"));
    }

    [ContextMenu("Show test unlock screen (unlocks nothing)")]
    void ShowTestScreen()
    {
        // a random run of neighbouring cards: different ones each time, never the same card twice
        var test = new List<RewardData>();
        int count = allCards.Rewards.Count, start = Random.Range(0, count);
        for (int i = 0; i < testCardCount && i < count; i++)
            test.Add(allCards.Rewards[(start + i) % count]);
        unlockScreen.Show(test, () => Debug.Log("Unlock screen closed"), "TEST IDEA");
    }

    [ContextMenu("Add Thoughts")]
    void AddThoughts()
    {
        Progress.AddThoughts(thoughtsToAdd);
        LogState();
    }

    [ContextMenu("Log progress")]
    void LogState()
    {
        var lockedIdeas = new List<string>();
        foreach (var idea in ideas.Ideas)
            if (!Progress.IsUnlocked(idea)) lockedIdeas.Add(idea.Title);

        // a card that starts locked but is in no Idea can never be unlocked: catch that here
        var orphans = new List<string>();
        foreach (var card in allCards.Rewards)
        {
            if (card.StartsUnlocked) continue;
            bool inIdea = false;
            foreach (var idea in ideas.Ideas) if (idea.Cards.Contains(card)) inIdea = true;
            if (!inIdea) orphans.Add(card.Title);
        }

        Debug.Log($"Thoughts: {Progress.Thoughts} / next recall {Progress.RecallCost} | can recall: {Progress.CanRecall(ideas)} | " +
                  $"locked Ideas ({lockedIdeas.Count}): {string.Join(", ", lockedIdeas)}" +
                  (orphans.Count > 0 ? $"\nLOCKED CARDS IN NO IDEA (never unlockable): {string.Join(", ", orphans)}" : ""));
    }

    [ContextMenu("Reset progress (new player)")]
    void ResetProgress()
    {
        Progress.ResetAll();
        LogState();
    }
}

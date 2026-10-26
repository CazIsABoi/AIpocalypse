using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// End-of-run reward sequence, started by DeathStats once per run:
// Thoughts count up -> recall bar fills -> if full: pick 1 of 3 Ideas -> its cards fan out -> repeat if still full
// -> show the death screen buttons. Thoughts were already added and saved before this starts.
public class RecallSequence : MonoBehaviour
{
    [SerializeField] private IdeaSet ideas;
    [SerializeField] private IdeaPicker picker;
    [SerializeField] private UnlockScreen unlockScreen;

    [Header("Death screen UI")]
    [SerializeField] private TMP_Text thoughtsText;      // "THOUGHTS +26"
    [SerializeField] private Image recallFill;           // Image Type = Filled, Horizontal
    [SerializeField] private TMP_Text recallText;        // "RECALL  160 / 200"
    [SerializeField] private GameObject[] continueButtons; // GiveUp / Continue?: hidden until the sequence is done

    [Header("Timing (unscaled: the game is paused)")]
    [SerializeField] private float startDelay = 0.6f;    // let the death stats land first
    [SerializeField] private float countTime = 1.2f;
    [SerializeField] private float fillTime = 0.8f;
    [SerializeField] private float recallDelay = 0.5f;   // beat on a full bar before the screen dims

    public void Play(int earned)
    {
        StopAllCoroutines();
        StartCoroutine(Run(earned));
    }

    IEnumerator Run(int earned)
    {
        SetButtons(false);
        thoughtsText.text = "THOUGHTS +0";
        int before = Mathf.Max(0, Progress.Thoughts - earned);
        ShowBar(before);
        yield return Wait(startDelay);

        // count up, tick sound every few points
        int shown = 0;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / countTime)
        {
            int now = Mathf.RoundToInt(Mathf.Lerp(0, earned, 1f - (1f - t) * (1f - t)));
            if (now / 2 != shown / 2) AudioManager.Instance?.Play(Sfx.ThoughtTick);   // a tick every 2 Thoughts
            shown = now;
            thoughtsText.text = $"THOUGHTS +{shown}";
            yield return null;
        }
        thoughtsText.text = $"THOUGHTS +{earned}";

        yield return Fill(before, Progress.Thoughts);

        // as long as the bar is full (a long run can pay for more than one), recall
        while (Progress.CanRecall(ideas))
        {
            yield return Wait(recallDelay);

            var choices = Progress.RecallChoices(ideas, 3);
            if (choices.Count == 0) { Debug.LogWarning("CanRecall is true but RecallChoices is empty"); break; }

            IdeaData picked = null;
            picker.Show(choices, idea => picked = idea);
            while (picked == null) yield return null;
            int recallsBefore = Progress.Recalls, cost = Progress.RecallCost;
            Progress.Recall(picked);   // save first (spends Thoughts, unlocks the Idea + its cards)...
            if (Progress.Recalls <= recallsBefore) { Debug.LogWarning("Recall didn't go through"); break; }   // would loop forever

            var names = new System.Collections.Generic.List<string>();
            foreach (var idea in choices) names.Add(idea.Id);   // Ids: survive renames
            ScoreService.SubmitRecall(new ScoreService.RecallEvent
            {
                idea = picked.Id, choices = string.Join(",", names),
                recallNumber = Progress.Recalls, cost = cost, thoughtsLeft = Progress.Thoughts
            });

            bool closed = false;       // ...show second
            unlockScreen.Show(picked.Cards, () => closed = true, picked.Title.ToUpper());
            while (!closed) yield return null;

            ShowBar(0);
            yield return Fill(0, Progress.Thoughts);   // leftover Thoughts toward the next (pricier) recall
        }

        SetButtons(true);
    }

    void SetButtons(bool on)
    {
        foreach (var button in continueButtons) if (button != null) button.SetActive(on);
    }

    IEnumerator Fill(int from, int to)
    {
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / fillTime)
        {
            ShowBar(Mathf.RoundToInt(Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t))));
            yield return null;
        }
        ShowBar(to);
    }

    void ShowBar(int thoughts)
    {
        int cost = Mathf.Max(1, Progress.RecallCost);
        recallFill.fillAmount = Mathf.Clamp01(thoughts / (float)cost);
        recallText.text = $"RECALL  {Mathf.Min(thoughts, cost)} / {cost}";
    }

    static IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) yield return null;
    }
}

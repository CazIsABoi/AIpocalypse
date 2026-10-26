using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// "A memory resurfaces": the screen dims, the title drops in, up to 3 Ideas fly in, the player picks one.
// Picking does NOT unlock anything here: it only reports the choice (RecallSequence calls Progress.Recall).
public class IdeaPicker : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup fade;          // on the panel: fades the whole screen in
    [SerializeField] private RectTransform title;       // "A MEMORY RESURFACES", punches in
    [SerializeField] private IdeaCard[] cards;          // drag the 3 Idea cards in
    [SerializeField] private float fadeTime = 0.4f;
    [SerializeField] private float titleHold = 0.6f;    // beat between the title and the cards
    [SerializeField] private float flyStagger = 0.12f;
    [SerializeField] private float lingerTime = 0.6f;   // how long the picked card stays before closing

    bool choosing;

    void Awake() => panel.SetActive(false);

    public void Show(List<IdeaData> choices, System.Action<IdeaData> onPicked)
    {
        panel.SetActive(true);   // first: coroutines can't start on inactive cards
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(choices, onPicked));
    }

    IEnumerator ShowRoutine(List<IdeaData> choices, System.Action<IdeaData> onPicked)
    {
        choosing = false;
        foreach (var card in cards) card.gameObject.SetActive(false);

        // dim in, then the title punches in big and settles
        AudioManager.Instance?.Play(Sfx.LevelUp);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / fadeTime)
        {
            if (fade != null) fade.alpha = t;
            if (title != null) title.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, 1f - (1f - t) * (1f - t));
            yield return null;
        }
        if (fade != null) fade.alpha = 1f;
        if (title != null) title.localScale = Vector3.one;
        for (float h = 0; h < titleHold; h += Time.unscaledDeltaTime) yield return null;

        for (int i = 0; i < cards.Length; i++)
        {
            bool used = i < choices.Count;
            cards[i].gameObject.SetActive(used);
            if (!used) continue;
            cards[i].Setup(choices[i], idea => { if (!choosing) StartCoroutine(Pick(idea, onPicked)); });
            cards[i].FlyIn(i * flyStagger);
        }
    }

    IEnumerator Pick(IdeaData idea, System.Action<IdeaData> onPicked)
    {
        choosing = true;
        foreach (var card in cards)
        {
            if (!card.gameObject.activeSelf) continue;
            card.Lock();
            if (card.Idea == idea) card.PlayChosen();
            else card.PlayDismissed();
        }
        AudioManager.Instance?.Play(Sfx.CardPick);
        for (float t = 0; t < lingerTime; t += Time.unscaledDeltaTime) yield return null;
        panel.SetActive(false);
        onPicked(idea);
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "NEW IDEAS" screen: shows freshly unlocked cards face down, 3 per page, click to flip.
// The cards are already unlocked and saved before this opens (save first, show second),
// so closing early or quitting never loses an unlock. Flipping is only the reveal.
public class UnlockScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private RewardCard[] cards;          // drag the 3 cards in (same prefab as the level-up cards)
    [SerializeField] private TMP_Text titleText;          // "NEW IDEA" / "NEW IDEAS"
    [SerializeField] private TMP_Text pageText;           // optional "1 / 2"
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private float flyStagger = 0.08f;    // delay between each card flying in
    [SerializeField] private CanvasGroup fade;            // optional, on the panel: fades the screen in instead of popping
    [SerializeField] private float fadeTime = 0.3f;

    private readonly List<RewardData> unlocked = new List<RewardData>();
    private readonly HashSet<RewardData> revealed = new HashSet<RewardData>();   // stays flipped when paging back
    private int page;
    private System.Action closed;

    public bool IsOpen => panel.activeSelf;
    int PageCount => Mathf.Max(1, Mathf.CeilToInt(unlocked.Count / (float)cards.Length));

    void Awake()
    {
        foreach (var card in cards)
        {
            RewardCard c = card;   // own copy for the lambda (a foreach variable would be shared in old C#)
            c.Revealed += _ => revealed.Add(c.Reward);
        }
        previousButton.onClick.AddListener(Previous);
        nextButton.onClick.AddListener(Next);
        confirmButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    // onClosed: optional, e.g. show the death screen buttons after the player confirms
    // title: optional heading, e.g. the recalled Idea's name
    public void Show(List<RewardData> newCards, System.Action onClosed = null, string title = null)
    {
        unlocked.Clear();
        foreach (var card in newCards) if (card != null) unlocked.Add(card);
        if (unlocked.Count == 0) { onClosed?.Invoke(); return; }   // nothing to show

        revealed.Clear();
        closed = onClosed;
        page = 0;
        string count = unlocked.Count == 1 ? "NEW CARD" : $"{unlocked.Count} NEW CARDS";
        titleText.text = title == null ? count : $"{title}\n<size=50%><alpha=#B3>{count}  ·  THEY CAN NOW APPEAR IN YOUR RUNS";
        panel.SetActive(true);   // first: coroutines can't start on inactive cards
        if (fade != null) StartCoroutine(FadeIn());
        ShowPage();
    }

    IEnumerator FadeIn()
    {
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / fadeTime)
        {
            fade.alpha = t;
            yield return null;
        }
        fade.alpha = 1f;
    }

    public void Next() { if (page < PageCount - 1) { page++; ShowPage(); } }        // right arrow
    public void Previous() { if (page > 0) { page--; ShowPage(); } }               // left arrow

    void ShowPage()
    {
        int first = page * cards.Length;
        for (int i = 0; i < cards.Length; i++)
        {
            int index = first + i;
            bool used = index < unlocked.Count;
            cards[i].gameObject.SetActive(used);
            if (!used) continue;

            RewardData card = unlocked[index];
            // no stats outside a run; face down until flipped once, then stays face up
            cards[i].Setup(card, _ => { }, null, !revealed.Contains(card));
            cards[i].FlyIn(i * flyStagger);
        }

        bool paged = PageCount > 1;
        previousButton.gameObject.SetActive(paged);
        nextButton.gameObject.SetActive(paged);
        previousButton.interactable = page > 0;
        nextButton.interactable = page < PageCount - 1;
        if (pageText != null)
        {
            pageText.gameObject.SetActive(paged);
            pageText.text = $"{page + 1} / {PageCount}";
        }
    }

    void Close()
    {
        panel.SetActive(false);
        var then = closed;
        closed = null;
        then?.Invoke();
    }
}

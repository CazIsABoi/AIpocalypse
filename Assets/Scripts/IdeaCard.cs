using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One Idea in the "recall" choice. Same layout trick as RewardCard:
// Root (this object) only scales (hover / picked / dismissed), Visual (child) holds everything and flies in.
// Easiest setup: duplicate a RewardCard, remove RewardCard + the card back, add this, reuse its texts.
public class IdeaCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI cardList;    // "UNLOCKS 6 CARDS" + their names
    [SerializeField] private Image icon;
    [SerializeField] private Image glow;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform visual;
    [SerializeField] private Outline border;
    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private UIGradient gradient;

    [SerializeField] float flyTime = 0.35f;
    [SerializeField] float flyOffset = 900f;
    [SerializeField] AnimationCurve flyCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 3f), new Keyframe(1f, 1f, 0f, 0f));
    [SerializeField] float hoverScale = 1.06f;
    [SerializeField] float chosenScale = 1.15f;
    [SerializeField] float scaleSpeed = 14f;

    public IdeaData Idea { get; private set; }

    bool locked, hovered;
    float targetScale = 1f;
    Color glowColor;
    System.Action<IdeaData> chosen;

    public void Setup(IdeaData idea, System.Action<IdeaData> onChosen)
    {
        Idea = idea;
        chosen = onChosen;
        var style = raritySettings.Get(idea.Rarity);

        title.text = idea.Title;
        description.text = idea.Description;
        string names = "";
        foreach (var card in idea.Cards) if (card != null) names += $"\n{card.Title}";
        cardList.text = $"<size=80%><alpha=#B3>UNLOCKS {idea.Cards.Count} CARDS</size><alpha=#FF>{names}";

        Color top = style.gradient.Evaluate(0f);
        gradient.Set(top, style.gradient.Evaluate(1f));
        if (border != null) border.effectColor = Color.Lerp(top, Color.white, 0.6f);
        icon.enabled = idea.Icon != null;
        if (idea.Icon != null) icon.sprite = idea.Icon;

        // Ideas are always a big deal: every card glows, in its rarity colour (white if that has none)
        glowColor = style.glowColor.a > 0f ? style.glowColor : new Color(1f, 1f, 1f, 0.4f);
        glow.enabled = true;

        StopAllCoroutines();
        transform.localScale = Vector3.one;
        visual.anchoredPosition = Vector2.zero;
        locked = hovered = false;
        targetScale = 1f;
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => { if (!locked) chosen(Idea); });
    }

    public void FlyIn(float delay) => StartCoroutine(FlyRoutine(delay));

    IEnumerator FlyRoutine(float delay)
    {
        button.interactable = false;
        visual.anchoredPosition = new Vector2(0f, -flyOffset);
        for (float d = 0; d < delay; d += Time.unscaledDeltaTime) yield return null;
        AudioManager.Instance?.Play(Sfx.CardFlip);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / flyTime)
        {
            visual.anchoredPosition = new Vector2(0f, Mathf.Lerp(-flyOffset, 0f, flyCurve.Evaluate(t)));
            yield return null;
        }
        visual.anchoredPosition = Vector2.zero;
        button.interactable = !locked;
    }

    // ---------- picked / not picked (called by IdeaPicker) ----------
    public void Lock() { locked = true; button.interactable = false; }
    public void PlayChosen() => targetScale = chosenScale;
    public void PlayDismissed() => targetScale = 0f;

    public void OnPointerEnter(PointerEventData e)
    {
        if (locked) return;
        hovered = true;
        AudioManager.Instance?.Play(Sfx.CardHover);
    }

    public void OnPointerExit(PointerEventData e) => hovered = false;
    void OnDisable() => hovered = false;

    void Update()
    {
        bool focused = button != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == button.gameObject;   // controller: the selected card grows like a hovered one
        float target = locked ? targetScale : (hovered || focused ? hoverScale : 1f);
        float k = 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, k);

        Color c = glowColor;
        c.a *= 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 2f);
        glow.color = c;
    }
}

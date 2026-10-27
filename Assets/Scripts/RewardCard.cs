using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Root (this object): positioned by the HorizontalLayoutGroup, only its SCALE is animated (hover / picked / dismissed).
// Visual (child):     holds everything you see, so flip, pop, shake and fly-in can move it freely.
public class RewardCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI change;
    [SerializeField] private GameObject back;
    [SerializeField] private Image glow;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform visual;
    [SerializeField] private Outline border;

    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private UIGradient gradient;

    [Header("Flip")]
    [SerializeField] Rarity flipFromRarity = Rarity.Rare;   // cards below this start face up (no flip click)
    [SerializeField] float flipTime = 0.125f;
    [SerializeField] float shakeTime = 0.25f;
    [SerializeField]
    AnimationCurve popCurve = new AnimationCurve(
    new Keyframe(0f, 0f),
    new Keyframe(0.7f, 1.1f),
    new Keyframe(1f, 1f));

    [Header("Fly in")]
    [SerializeField] float flyTime = 0.35f;
    [SerializeField] float flyOffset = 900f;        // starts this many pixels below its slot
    [SerializeField]
    AnimationCurve flyCurve = new AnimationCurve(   // ease-out: fast start, soft landing
    new Keyframe(0f, 0f, 0f, 3f),
    new Keyframe(1f, 1f, 0f, 0f));

    [Header("Hover / pick")]
    [SerializeField] float hoverScale = 1.06f;
    [SerializeField] float chosenScale = 1.15f;
    [SerializeField] float scaleSpeed = 14f;        // how quickly the root eases to its target scale

    public RewardData Reward => reward;
    public event System.Action<RaritySettings.Style> Revealed;   // LevelUpUI listens (row shake for Legendary)

    bool revealed;
    bool locked;                // a card was picked: ignore hover and clicks
    bool hovered;
    float targetScale = 1f;     // used while locked (picked = big, dismissed = 0)
    Color glowColor;            // what the glow pulses around
    RaritySettings.Style style;
    RewardData reward;
    System.Action<RewardData> chosen;

    // stats = null outside a run (unlock screen): no "old -> new" preview
    // faceDown: null = by rarity (level-ups), true = always flip to reveal, false = already revealed (glowing)
    public void Setup(RewardData reward, System.Action<RewardData> onChosen, PlayerStats stats, bool? faceDown = null)
    {
        style = raritySettings.Get(reward.GetRarity());
        string tag = reward.GetPath() switch   // which build path this card feeds
        {
            RewardPath.Melee => "  <color=#FBC270>MELEE</color>",
            RewardPath.Gun => "  <color=#7090FF>GUN</color>",
            _ => ""
        };
        title.text = $"{reward.Title}\n<size=60%><alpha=#B3><cspace=0.15em>{reward.GetRarity().ToString().ToUpper()}</cspace><alpha=#FF>{tag}";
        description.text = ColorSigns(reward.Description);
        change.text = stats != null ? reward.GetPreview(stats) : "";

        Color top = style.gradient.Evaluate(0f);
        gradient.Set(top, style.gradient.Evaluate(1f));
        if (border != null) border.effectColor = Color.Lerp(top, Color.white, 0.6f);   // lighter rim of the card's own colour

        change.gameObject.SetActive(change.text != "");
        icon.enabled = reward.Icon != null;
        if (reward.Icon != null) icon.sprite = reward.Icon;

        // Clean state for a new hand: kill anything a previous flip/fly left half-done
        StopAllCoroutines();
        transform.localScale = Vector3.one;
        visual.localScale = Vector3.one;
        visual.localRotation = Quaternion.identity;
        visual.anchoredPosition = Vector2.zero;
        button.interactable = true;
        // Only Rare+ cards are worth a flip; lower ones arrive face up, so picking takes one click
        revealed = faceDown.HasValue ? !faceDown.Value : reward.GetRarity() < flipFromRarity;
        locked = false;
        hovered = false;
        targetScale = 1f;
        back.SetActive(!revealed);
        glow.enabled = false;
        if (faceDown == false)   // revealed earlier (unlock screen page flipped back): keep its glow
        {
            glowColor = style.glowColor;
            glow.enabled = glowColor.a > 0f;
        }
        this.reward = reward;
        chosen = onChosen;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    // "+40% attack speed, -20% damage" → green / red parts
    static string ColorSigns(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return Regex.Replace(text, @"(?<![\w>])([+-])(\d[^,\n]*)", m =>
            $"<color={(m.Groups[1].Value == "+" ? StatNames.Good : StatNames.Bad)}>{m.Value}</color>");
    }

    void OnClick()
    {
        if (locked) return;
        if (!revealed) StartCoroutine(Flip());
        else chosen(reward);
    }

    // ---------- fly in ----------
    public void FlyIn(float delay) => StartCoroutine(FlyRoutine(delay));

    IEnumerator FlyRoutine(float delay)
    {
        button.interactable = false;
        visual.anchoredPosition = new Vector2(0f, -flyOffset);   // hidden below before the delay
        for (float d = 0; d < delay; d += Time.unscaledDeltaTime) yield return null;
        AudioManager.Instance?.Play(style.flyInSfx);   // per rarity: Rare+ hint at what's coming

        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / flyTime)
        {
            visual.anchoredPosition = new Vector2(0f, Mathf.Lerp(-flyOffset, 0f, flyCurve.Evaluate(t)));
            yield return null;
        }
        visual.anchoredPosition = Vector2.zero;
        button.interactable = !locked;
    }

    // ---------- flip ----------
    IEnumerator Flip()
    {
        revealed = true;
        button.interactable = false;
        AudioManager.Instance?.Play(Sfx.CardFlip);
        float t;
        Vector3 scale = visual.localScale;

        for (t = 0; t < 1f; t += Time.unscaledDeltaTime / flipTime)
        {
            scale.x = Mathf.Lerp(1f, 0f, t);
            visual.localScale = scale;
            yield return null;
        }
        scale.x = 0f;
        visual.localScale = scale;

        // Anticipation: big rarities wait a moment while closed, so the hit lands harder
        for (float h = 0; h < style.revealHold; h += Time.unscaledDeltaTime) yield return null;

        back.SetActive(false);
        // Punch: jump big, Update's easing brings it back down by itself
        if (style.punchScale > 1f) transform.localScale = Vector3.one * style.punchScale;
        glowColor = style.glowColor;
        glow.enabled = glowColor.a > 0f;           // no glow for Common/Uncommon
        if (style.shakeStrength > 0f) StartCoroutine(Shake(style.shakeStrength));
        AudioManager.Instance?.Play(style.revealSfx);
        Revealed?.Invoke(style);

        for (t = 0; t < 1f; t += Time.unscaledDeltaTime / flipTime)
        {
            scale.x = popCurve.Evaluate(t);
            visual.localScale = scale;
            yield return null;
        }
        scale.x = popCurve.Evaluate(1f);
        visual.localScale = scale;
        button.interactable = !locked;
    }

    IEnumerator Shake(float strength)
    {
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / shakeTime)
        {
            float angle = Random.Range(-1f, 1f) * strength * (1f - t);
            visual.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        visual.localRotation = Quaternion.identity;
    }

    // ---------- picked / not picked (called by LevelUpUI) ----------
    public void Lock()
    {
        locked = true;
        button.interactable = false;
    }

    public void PlayChosen()
    {
        targetScale = chosenScale;
        // even a Common gets a short white glow so the pick is obvious
        glowColor = style.glowColor.a > 0f ? style.glowColor : new Color(1f, 1f, 1f, 0.6f);
        glow.enabled = true;
    }

    public void PlayDismissed() => targetScale = 0f;   // shrink away

    // ---------- hover ----------
    public void OnPointerEnter(PointerEventData e)
    {
        if (locked) return;
        hovered = true;
        AudioManager.Instance?.Play(Sfx.CardHover);
    }

    public void OnPointerExit(PointerEventData e) => hovered = false;

    void OnDisable() => hovered = false;   // panel closed under the mouse: no Exit event comes

    void Update()
    {
        // Root scale eases toward its target. 1 - Exp(-speed * dt) = same feel at any frame rate.
        bool focused = button != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == button.gameObject;   // controller: the selected card grows like a hovered one
        float target = locked ? targetScale : (hovered || focused ? hoverScale : 1f);
        float k = 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, k);

        if (!glow.enabled) return;
        Color c = glowColor;
        c.a *= 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 2f);   // breathe between 50% and 100% alpha
        glow.color = c;
    }
}

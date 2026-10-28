using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One Idea in the Run Picker's list (a prefab). Click to equip / unequip; locked Ideas show as "???".
public class IdeaSlot : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text detail;          // "6 cards · Hits heal you."
    [SerializeField] private Image equippedMark;       // on while equipped (blue), always on for AlwaysInPool Ideas (orange)
    [SerializeField] private Color alwaysOnColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private GameObject alwaysOnLabel; // your small "always on" text, only shown on the orange ones
    [SerializeField] private CanvasGroup group;        // greys out locked Ideas
    private Color equippedColor;
    private bool alwaysOn;

    void Awake() => equippedColor = equippedMark.color;

    public IdeaData Idea { get; private set; }

    public void Setup(IdeaData idea, bool unlocked, System.Action<IdeaSlot> onClicked)
    {
        Idea = idea;
        alwaysOn = unlocked && idea.AlwaysInPool;
        equippedMark.color = alwaysOn ? alwaysOnColor : equippedColor;
        alwaysOnLabel.SetActive(alwaysOn);

        if (alwaysOn)
        {
            // nothing to equip: its cards are already in every run, so it shows as permanently equipped (orange)
            title.text = idea.Title;
            detail.text = $"{idea.Cards.Count} cards · {idea.Description}";
            group.alpha = 1f;
            button.interactable = false;
        }
        else if (unlocked)
        {
            title.text = idea.Title;
            detail.text = $"{idea.Cards.Count} cards · {idea.Description}";
            group.alpha = 1f;
            button.interactable = true;
        }
        else
        {
            title.text = "???";
            detail.text = idea.Requires != null && !Progress.IsUnlocked(idea.Requires)
                ? $"Needs {idea.Requires.Title} first"
                : "Not yet recalled";
            group.alpha = 0.5f;
            button.interactable = false;
        }

        // hand the button a function to call on click (Setup can run again on the same slot, so clear first)
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked(this));
    }

    public void SetEquipped(bool equipped)
    {
        equippedMark.gameObject.SetActive(equipped || alwaysOn);
    }
}

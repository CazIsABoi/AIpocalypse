using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One Idea in the Run Picker's list (a prefab). Click to equip / unequip; locked Ideas show as "???".
public class IdeaSlot : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text detail;          // "6 cards · Hits heal you."
    [SerializeField] private GameObject equippedMark;  // on while equipped
    [SerializeField] private CanvasGroup group;        // greys out locked Ideas

    public IdeaData Idea { get; private set; }

    public void Setup(IdeaData idea, bool unlocked, System.Action<IdeaSlot> onClicked)
    {
        Idea = idea;
        if (unlocked && idea.AlwaysInPool)
        {
            // nothing to equip: its cards are already in every run
            title.text = idea.Title;
            detail.text = $"{idea.Cards.Count} cards · always in the pool · {idea.Description}";
            group.alpha = 0.8f;
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
            detail.text = "Not yet recalled";
            group.alpha = 0.5f; // make it see-through
            button.interactable = false;
        }

        // hand the button a function to call on click (Setup can run again on the same slot, so clear first)
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked(this));
    }

    public void SetEquipped(bool equipped)
    {
        equippedMark.SetActive(equipped);
    }
}

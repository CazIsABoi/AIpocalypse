using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdeaSlot : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text detail;         
    [SerializeField] private GameObject equippedMark;  
    [SerializeField] private CanvasGroup group;       

    public IdeaData Idea { get; private set; }

    public void Setup(IdeaData idea, bool unlocked, System.Action<IdeaSlot> onClicked)
    {
        Idea = idea;
        if (unlocked)
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

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked(this));
    }

    public void SetEquipped(bool equipped)
    {
        equippedMark.SetActive(equipped);
    }
}

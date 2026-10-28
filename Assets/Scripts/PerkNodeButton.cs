using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PerkNodeButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text cost;           // the point cost, hidden once bought
    [SerializeField] private GameObject activeRing;   // on while the node is active this run
    [SerializeField] private CanvasGroup group;       // optional: fades the whole node (text and all) while it's locked
    [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.45f;
    [SerializeField] private Color lockedColor = new Color(0.25f, 0.25f, 0.25f);   // can't buy yet
    [SerializeField] private Color buyableColor = new Color(0.9f, 0.75f, 0.3f);    // affordable, requirements met
    [SerializeField] private Color boughtColor = new Color(0.85f, 0.85f, 0.85f);   // owned (maybe not active)

    public PerkNode Node { get; private set; }

    // Fired when the mouse moves onto the node, or a gamepad/keyboard selects it, so the tree can show
    // what it does BEFORE it's bought. Works on locked nodes too: hover doesn't need the button to be clickable.
    public event System.Action<PerkNodeButton> Hovered;

    public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this);
    public void OnSelect(BaseEventData eventData) => Hovered?.Invoke(this);

    public void Setup(PerkNode node, System.Action<PerkNodeButton> onClicked)
    {
        Node = node;
        title.text = node.Title;
        cost.text = node.Cost.ToString();
        if (node.IsMajor)
        {
            transform.localScale = Vector3.one * 1.2f;   // majors stand out: they're the branch choices
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked(this));
    }

    // Called after every change in the tree
    public void SetState(bool bought, bool active, bool canBuy)
    {
        if (bought)
        {
            background.color = boughtColor;
            cost.gameObject.SetActive(false);
            button.interactable = Node.IsMajor;   // a bought major can be clicked to switch to its branch
        }
        else if (canBuy)
        {
            background.color = buyableColor;
            cost.gameObject.SetActive(true);
            button.interactable = true;           // click to buy
        }
        else
        {
            background.color = lockedColor;
            cost.gameObject.SetActive(true);
            button.interactable = false;          // not affordable, or the node below isn't bought
        }
        activeRing.SetActive(active);
        // locked nodes fade back so the ones you can act on stand out (hover still works: alpha doesn't block it)
        if (group != null) group.alpha = bought || canBuy ? 1f : lockedAlpha;
    }
}

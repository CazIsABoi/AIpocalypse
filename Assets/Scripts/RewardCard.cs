using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI change;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;

    [SerializeField] private RaritySettings raritySettings;
    [SerializeField] private UIGradient gradient;

    public void Setup(RewardData reward, System.Action<RewardData> onChosen, PlayerStats stats)
    {
        title.text = reward.Title;
        description.text = reward.Description;
        change.text = reward.GetPreview(stats);
        var style = raritySettings.Get(reward.GetRarity());
        gradient.Set(style.gradient.Evaluate(0f), style.gradient.Evaluate(1f));
        if (change.text == "")
        {
            change.gameObject.SetActive(false);
        }
        else
        {
            change.gameObject.SetActive(true);
        }
        if (reward.Icon != null)
        {
            icon.sprite = reward.Icon;
            icon.enabled = true;
        }
        else
        {
            icon.enabled = false;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onChosen(reward));
    }
}
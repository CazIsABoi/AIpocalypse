using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;

    public void Setup(RewardData reward, System.Action<RewardData> onChosen)
    {
        title.text = reward.Title;
        description.text = reward.Description;
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
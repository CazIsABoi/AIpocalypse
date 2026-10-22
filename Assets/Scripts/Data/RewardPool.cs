using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rewards/Reward Pool")]
public class RewardPool : ScriptableObject
{
    public List<RewardData> Rewards;
    public string sourceText;
}
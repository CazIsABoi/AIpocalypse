using System.Collections.Generic;
using UnityEngine;

// Every achievement in the game (Data/Achievements/AllAchievements)
[CreateAssetMenu(menuName = "Rewards/Achievement Set")]
public class AchievementSet : ScriptableObject
{
    public List<AchievementData> Achievements;
}

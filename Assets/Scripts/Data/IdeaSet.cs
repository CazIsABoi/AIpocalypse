using System.Collections.Generic;
using UnityEngine;

// Every Idea that can be recalled. One asset, like the card pools.
[CreateAssetMenu(menuName = "Rewards/Idea Set")]
public class IdeaSet : ScriptableObject
{
    public List<IdeaData> Ideas;
}

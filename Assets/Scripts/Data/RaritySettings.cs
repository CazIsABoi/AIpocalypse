using UnityEngine;

[CreateAssetMenu(menuName = "Rewards/Rarity Settings")]
public class RaritySettings : ScriptableObject
{
    [System.Serializable]
    public struct Style
    {
        public Rarity rarity;
        public Gradient gradient;           
        public GameObject revealEffect;     
    }

    public Style[] styles;

    public Style Get(Rarity r)
    {
        foreach (var style in styles)
        {
            if (style.rarity == r)
            {
                return style;
            }
        }
        return styles[0];
    }
}
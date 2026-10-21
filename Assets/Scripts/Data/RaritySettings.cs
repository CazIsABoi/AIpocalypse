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
        public Sfx revealSfx;
        public Color glowColor;
        public float shakeStrength;
        public float rowShake;          // pixels the whole card row shakes on reveal (0 = none, Legendary only)
        public float punchScale;        // card jumps to this size on reveal and eases back (0/1 = none)
        public float flashAlpha;        // white screen flash strength on reveal (0 = none, keep <= 0.8)
        public float revealHold;        // seconds the card stays closed before opening (anticipation, 0 = none)
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
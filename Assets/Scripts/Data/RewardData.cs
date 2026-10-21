using UnityEngine;

public abstract class RewardData : ScriptableObject
{
    public string Title;
    [TextArea] public string Description;
    public Sprite Icon;
    public int Weight = 10;          // how often it appears
    public Rarity Rarity;

    public abstract void Apply(PlayerStats stats);

    public virtual string GetPreview(PlayerStats stats) => "";
    public Rarity GetRarity()
    {
        if (Rarity != Rarity.Auto) return Rarity;
        switch (Weight)
        {
            // matches the weight scale used on the items: 10 / 7 / 4 / 2 / 1
            case >= 10: return Rarity.Common;
            case >= 7: return Rarity.Uncommon;
            case >= 4: return Rarity.Rare;
            case >= 2: return Rarity.Epic;
            default: return Rarity.Legendary;
        }
    }
}

// Assets store these as numbers: only ever add new values at the END, never in between
public enum StatType { MoveSpeed, MoveSpeedPercent, MaxHealth, Damage, DamagePercent, PickupRadius, BulletAmount, CritDamage, CritChance, BulletPierce, RegenAmount, AttackSpeed, Lifesteal}
public enum Rarity { Auto, Common, Uncommon, Rare, Epic, Legendary }

public static class StatNames
{
    public static string Get(StatType type)
    {
        switch (type)
        {
            case StatType.Damage:
            case StatType.DamagePercent:
                return "Damage";
            case StatType.MoveSpeed:
            case StatType.MoveSpeedPercent:
                return "Speed";
            case StatType.MaxHealth:
                return "Max HP";
            case StatType.PickupRadius:
                return "Pickup Range";
            case StatType.CritDamage:
                return "Crit Damage";
            case StatType.CritChance:
                return "Crit Chance";
            case StatType.BulletAmount:
                return "Bullets";
            case StatType.BulletPierce:
                return "Pierce";
            case StatType.RegenAmount:
                return "Health Regen";
            case StatType.AttackSpeed:
                return "Attack Speed";
            case StatType.Lifesteal:
                return "Lifesteal";
            default:
                return type.ToString();  
        }
    }
    public static string Format(StatType type, float value)
    {
        switch (type)
        {
            case StatType.CritChance:
            case StatType.CritDamage:
                return $"{value * 100f:0.#}%";      // 0.2 → "20%", 1.1 → "110%                                         
            case StatType.MaxHealth:
                return $"{value:0}";                // 100.5 → "100",
            case StatType.RegenAmount:
                return $"{value * 100f:0.#}%/s"; // 0.2 → "20%/s", 1.1 → "110%/s"
            case StatType.AttackSpeed:
                return $"{(1 + value) * 100f:0.#}%"; // 0.2 → "120%", 1.1 → "210%"
            case StatType.Lifesteal:
                return $"{value * 100f:0.#}%";      // 0.2 → "20%", 1.1 → "110%"
            default:
                return $"{value:0.#}";
        }
    }
}

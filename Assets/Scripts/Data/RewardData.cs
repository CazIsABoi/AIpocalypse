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

    // false = pointless right now (e.g. its stat is already capped), so it isn't offered
    public virtual bool CanOffer(PlayerStats stats) => true;

    // Which build path this card feeds (shown as a tag on the card)
    public virtual RewardPath GetPath() => RewardPath.None;
    public static RewardPath PathOf(StatType t)
    {
        switch (t)
        {
            case StatType.MeleeDamagePercent: case StatType.MeleeRange: case StatType.MeleeSpeedPercent:
                return RewardPath.Melee;
            case StatType.GunDamagePercent: case StatType.BulletAmount: case StatType.BulletPierce:
                return RewardPath.Gun;
            default: return RewardPath.None;
        }
    }
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
public enum RewardPath { None, Melee, Gun }

// Assets store StatType as numbers: only ever add new values at the END
public enum StatType { MoveSpeed, MoveSpeedPercent, MaxHealth, Damage, DamagePercent, PickupRadius, BulletAmount, CritDamage, CritChance, BulletPierce, RegenAmount, AttackSpeed, Lifesteal,
    MeleeDamagePercent, MeleeRange, MeleeSpeedPercent, GunDamagePercent}   // path stats (added at the end on purpose)
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
            case StatType.MeleeDamagePercent:
                return "Melee Damage";
            case StatType.MeleeRange:
                return "Melee Range";
            case StatType.MeleeSpeedPercent:
                return "Melee Speed";
            case StatType.GunDamagePercent:
                return "Gun Damage";
            default:
                return type.ToString();  
        }
    }
    // 950 -> "950", 12400 -> "12.4k", 3500000 -> "3.5M": late-game damage gets huge
    public static string Compact(float v)
    {
        float a = Mathf.Abs(v);
        if (a >= 1e9f) return $"{v / 1e9f:0.#}B";
        if (a >= 1e6f) return $"{v / 1e6f:0.#}M";
        if (a >= 1e4f) return $"{v / 1e3f:0.#}k";
        return $"{v:0.#}";
    }

    public const string Good = "#6CFF8A";     // green for gains
    public const string Bad = "#FF6464";      // red for losses

    // "old -> new" with the new value coloured by whether it went up or down
    public static string Change(StatType type, float before, float after)
    {
        string to = Format(type, after);
        if (after > before) to = $"<color={Good}>{to}</color>";
        else if (after < before) to = $"<color={Bad}>{to}</color>";
        return $"{Format(type, before)} -> {to}";
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
            case StatType.MeleeDamagePercent:
            case StatType.MeleeSpeedPercent:
            case StatType.GunDamagePercent:
                return $"{(1 + value) * 100f:0.#}%"; // 0.2 → "120%", 1.1 → "210%"
            case StatType.Lifesteal:
                return $"{value * 100f:0.#}%";      // 0.2 → "20%", 1.1 → "110%"
            default:
                return $"{value:0.#}";
        }
    }
}

using UnityEngine;

public abstract class RewardData : ScriptableObject
{
    public string Title;
    [TextArea] public string Description;
    public Sprite Icon;
    public int Weight = 10;          // how often it appears

    public abstract void Apply(PlayerStats stats);

    public virtual string GetPreview(PlayerStats stats) => "";
}
public enum StatType { MoveSpeed, MoveSpeedPercent, MaxHealth, Damage, DamagePercent, PickupRadius, CritDamage, CritChance }

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
            default:
                return $"{value:0.#}";
        }
    }

}

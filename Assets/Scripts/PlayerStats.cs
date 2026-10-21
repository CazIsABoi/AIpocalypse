using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float MaxHealth = 100f;
    public float BaseSpeed = 8f;
    public float flatSpeed;
    public float speedPercent;
    public float Speed => CalcSpeed(flatSpeed, speedPercent);
    public float baseDamage = 10f;
    private float flatDamage;        // sum of all "+X damage" upgrades
    private float damagePercent;     // sum of all "+X% damage" upgrades (0.1 = 10%)
    public float Damage => CalcDamage(flatDamage, damagePercent);

    public float CritDamage;    // 2f is 2x damage, a 100% damage increase.
    public float CritChance;    // 0 -> 1 is 0% -> 100% crit chance
    public float CritHitDamage => Damage * CritDamage;

    public float pickupRange = 3;
    public event System.Action<StatType> StatsChanged;

    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }
    public int XpToNext => XpForLevel(Level);
    public event System.Action<int, int> XpChanged;   // (current, needed)
    public event System.Action<int> LeveledUp;        // new level

    public void AddXp(int amount)
    {
        // Debug.Log($"AddXp({amount}) → {Xp + amount}/{XpToNext}");
        Xp += amount;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            LeveledUp?.Invoke(Level);
        }
        XpChanged?.Invoke(Xp, XpToNext);
    }

    int XpForLevel(int level) { return 100 + (level - 1) * 25; }

    public void Add(StatType type, float amount)
    {
        switch (type)
        {
            case (StatType.MoveSpeed):
                flatSpeed += amount;
                break;
            case StatType.MoveSpeedPercent:
                speedPercent += amount;
                break;
            case StatType.MaxHealth:
                MaxHealth = Mathf.Max(1f, MaxHealth + amount);
                break;
            case StatType.Damage:
                flatDamage += amount;
                break;
            case StatType.DamagePercent:
                damagePercent += amount;
                break;
            case StatType.PickupRadius:
                pickupRange += amount;
                break;
            case StatType.CritDamage:
                CritDamage += amount;
                break;
            case StatType.CritChance:
                CritChance += amount;
                break;
            default:
                Debug.LogError($"Unknown stat type: {type}");
                break;
        }
        StatsChanged?.Invoke(type);
    }

    public float Get(StatType type)
    {
        switch(type)
        {
            case StatType.MoveSpeed:
                return CalcSpeed(flatSpeed, speedPercent);
            case StatType.MoveSpeedPercent:
                return CalcSpeed(flatSpeed, speedPercent);
            case StatType.MaxHealth:
                return MaxHealth;
            case StatType.Damage:
                return CalcDamage(flatDamage, damagePercent);
            case StatType.DamagePercent:
                return CalcDamage(flatDamage, damagePercent);
            case StatType.PickupRadius:
                return pickupRange;
            case StatType.CritDamage:
                return CritDamage;
            case StatType.CritChance:
                return CritChance;
            default:
                Debug.LogError($"Unknown stat type: {type}");
                return 0f;
        }
    }

    public float Preview(StatType type, float amount)
    {
        switch (type)
        {
            case StatType.MoveSpeed:
                return CalcSpeed(flatSpeed + amount, speedPercent);
            case StatType.MoveSpeedPercent:
                return CalcSpeed(flatSpeed, speedPercent + amount);
            case StatType.MaxHealth:
                return MaxHealth + amount;
            case StatType.Damage:
                return CalcDamage(flatDamage + amount, damagePercent);
            case StatType.DamagePercent:
                return CalcDamage(flatDamage, damagePercent + amount);
            case StatType.PickupRadius:
                return pickupRange + amount;
            case StatType.CritDamage:
                return CritDamage + amount;
            case StatType.CritChance:
                return CritChance + amount;
            default:
                Debug.LogError($"Unknown stat type: {type}");
                return 0f;
        }
    }
    float CalcDamage(float flat, float percent) => (baseDamage + flat) * (1f + percent);
    float CalcSpeed(float flat, float percent) => (BaseSpeed + flat) * (1f + percent);
}

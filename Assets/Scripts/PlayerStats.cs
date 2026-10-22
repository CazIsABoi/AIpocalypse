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

    public int BulletAmount;
    public int BulletPierce;

    public float pickupRange = 3;
    public float regenAmount = 0.01f; // fraction of max HP per second (0.01 = 1%/s)
    public event System.Action<StatType> StatsChanged;
    public float AttackSpeed;
    public float Lifesteal; // fraction of damage dealt that is returned as health (0.01 = 1% of damage dealt)
    [SerializeField] private float lifestealCap = 0.3f; // cap at 30% lifesteal

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

    int XpForLevel(int level) { return 30 + (level - 1) * 15; }

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
            case StatType.BulletAmount:
                BulletAmount += (int) amount;
                break;
            case StatType.CritDamage:
                CritDamage += amount;
                break;
            case StatType.CritChance:
                CritChance += amount;
                break;
            case StatType.BulletPierce:
                BulletPierce += (int) amount;
                break;
            case StatType.RegenAmount:
                regenAmount += amount;
                break;
            case StatType.AttackSpeed:
                AttackSpeed += amount;
                break;
            case StatType.Lifesteal:
                Lifesteal = Mathf.Min(lifestealCap, Lifesteal + amount);
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
            case StatType.BulletAmount:
                return BulletAmount;
            case StatType.BulletPierce:
                return BulletPierce;
            case StatType.RegenAmount:
                return regenAmount;
            case StatType.AttackSpeed:
                return AttackSpeed;
            case StatType.Lifesteal:
                return Lifesteal;
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
            case StatType.BulletAmount:
                return BulletAmount + amount;
            case StatType.BulletPierce:
                return BulletPierce + amount;
            case StatType.RegenAmount:
                return regenAmount + amount;
            case StatType.AttackSpeed:
                return AttackSpeed + amount;
            case StatType.Lifesteal:
                return Mathf.Min(lifestealCap, Lifesteal + amount);
            default:
                Debug.LogError($"Unknown stat type: {type}");
                return 0f;
        }
    }
    // A stat that can't usefully go higher: cards that only raise it are left out of the picks
    public bool IsCapped(StatType type)
    {
        switch (type)
        {
            case StatType.CritChance: return CritChance >= 1f;                // 100% crit
            case StatType.Lifesteal: return Lifesteal >= lifestealCap - 0.0001f;
            default: return false;
        }
    }

    float CalcDamage(float flat, float percent) => (baseDamage + flat) * (1f + percent);
    float CalcSpeed(float flat, float percent) => (BaseSpeed + flat) * (1f + percent);
}

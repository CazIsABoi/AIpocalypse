using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float MaxHealth = 100f;
    public float BaseSpeed = 8f;
    public float flatSpeed;
    public float speedPercent;
    public float Speed => (BaseSpeed + flatSpeed) * (1f + speedPercent);
    public float baseDamage = 10f;
    private float flatDamage;        // sum of all "+X damage" upgrades
    private float damagePercent;     // sum of all "+X% damage" upgrades (0.1 = 10%)
    public float Damage => (baseDamage + flatDamage) * (1f + damagePercent);

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
                MaxHealth += amount;
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
            default:
                Debug.LogError($"Unknown stat type: {type}");
                break;
        }
        StatsChanged?.Invoke(type);
    }
}

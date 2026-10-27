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
    public float CritChance;    // 0 -> 1 is 0% -> 100% crit chance (never above critChanceCap)
    [SerializeField] private float critChanceCap = 0.75f;   // raised by CritChanceCap cards (e.g. the Intuition Idea), max 100%
    private float critGathered = -1f;   // all crit chance picked up, even above the cap: raising the cap releases it
    public float CritChanceCap => critChanceCap;
    public float CritHitDamage => Damage * CritDamage;

    public int BulletAmount;
    public int BulletPierce;
    public float ProjectileSpeed;   // bonus: 0 = normal speed, 0.2 = +20%, 1 = double

    public float pickupRange = 3;
    public float regenAmount = 0.01f; // fraction of max HP per second (0.01 = 1%/s)
    public event System.Action<StatType> StatsChanged;
    public float AttackSpeed;
    public float Lifesteal; // fraction of damage dealt that is returned as health (0.01 = 1% of damage dealt)
    [SerializeField] private float lifestealCap = 0.15f; // cap at 15% lifesteal
    public float MeleeLifesteal;                               // extra lifesteal on melee hits only (adds to Lifesteal)
    [SerializeField] private float meleeLifestealCap = 0.1f;
    [SerializeField] private float regenCap = 0.04f;     // cap at 4% max HP per second

    [Header("Build paths")]
    public float meleeDamagePercent;     // melee only (0.15 = +15%)
    public float meleeSpeedPercent;      // melee only, stacks with AttackSpeed
    public float gunDamagePercent;       // gun only
    public float meleeRange;             // extra swing radius
    [SerializeField] private float meleeBaseRadius = 1.5f;
    [SerializeField] private float meleeRangeCap = 2f;  // at most +2 radius (3.5 total)
    public float MeleeRadius => meleeBaseRadius + meleeRange;
    public float MeleeDamageMult => Mathf.Max(0.1f, 1f + meleeDamagePercent);   // never below 10%
    public float GunDamageMult => Mathf.Max(0.1f, 1f + gunDamagePercent); 
    [SerializeField] private float meleeDamageFactor = 4f;   // a swing hits this many times harder than a bullet
    // What one hit actually deals (shown in the stats panels)
    public float GunHit => Damage * GunDamageMult;
    public float MeleeHit => Damage * meleeDamageFactor * MeleeDamageMult;
    public float GunCritHit => GunHit * CritDamage;
    public float MeleeCritHit => MeleeHit * CritDamage;

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

    // Slightly curved: spawns ramp up a lot, so a linear curve made late levels come faster and faster
    int XpForLevel(int level)
    {
        int l = level - 1;
        return 30 + l * 15 + Mathf.RoundToInt(1.5f * l * l);
    }

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
                if (critGathered < 0f) critGathered = CritChance;   // first change: start from the base value
                critGathered += amount;
                CritChance = Mathf.Min(critChanceCap, critGathered);
                break;
            case StatType.BulletPierce:
                BulletPierce = Mathf.Max(1, BulletPierce + (int) amount);   // pierce 0 = bullets never hit anything
                break;
            case StatType.RegenAmount:
                regenAmount = Mathf.Min(regenCap, regenAmount + amount);
                break;
            case StatType.AttackSpeed:
                AttackSpeed += amount;
                break;
            case StatType.Lifesteal:
                Lifesteal = Mathf.Min(lifestealCap, Lifesteal + amount);
                break;
            case StatType.MeleeDamagePercent: meleeDamagePercent += amount; break;
            case StatType.MeleeSpeedPercent: meleeSpeedPercent += amount; break;
            case StatType.GunDamagePercent: gunDamagePercent += amount; break;
            case StatType.MeleeRange: meleeRange = Mathf.Min(meleeRangeCap, meleeRange + amount); break;
            case StatType.ProjectileSpeed: ProjectileSpeed += amount; break;
            case StatType.MeleeLifesteal: MeleeLifesteal = Mathf.Min(meleeLifestealCap, MeleeLifesteal + amount); break;
            case StatType.CritChanceCap:
                critChanceCap = Mathf.Min(1f, critChanceCap + amount);
                if (critGathered < 0f) critGathered = CritChance;
                CritChance = Mathf.Min(critChanceCap, critGathered);   // crit chance gathered above the old cap counts now
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
            case StatType.MeleeDamagePercent: return meleeDamagePercent;
            case StatType.MeleeSpeedPercent: return meleeSpeedPercent;
            case StatType.GunDamagePercent: return gunDamagePercent;
            case StatType.MeleeRange: return MeleeRadius;
            case StatType.ProjectileSpeed: return ProjectileSpeed;
            case StatType.CritChanceCap: return critChanceCap;
            case StatType.MeleeLifesteal: return MeleeLifesteal;
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
                return Mathf.Min(critChanceCap, (critGathered < 0f ? CritChance : critGathered) + amount);
            case StatType.CritChanceCap:
                return Mathf.Min(1f, critChanceCap + amount);
            case StatType.BulletAmount:
                return BulletAmount + amount;
            case StatType.BulletPierce:
                return Mathf.Max(1, BulletPierce + amount);
            case StatType.RegenAmount:
                return Mathf.Min(regenCap, regenAmount + amount);
            case StatType.AttackSpeed:
                return AttackSpeed + amount;
            case StatType.Lifesteal:
                return Mathf.Min(lifestealCap, Lifesteal + amount);
            case StatType.MeleeDamagePercent: return meleeDamagePercent + amount;
            case StatType.MeleeSpeedPercent: return meleeSpeedPercent + amount;
            case StatType.GunDamagePercent: return gunDamagePercent + amount;
            case StatType.MeleeRange: return meleeBaseRadius + Mathf.Min(meleeRangeCap, meleeRange + amount);
            case StatType.ProjectileSpeed: return ProjectileSpeed + amount;
            case StatType.MeleeLifesteal: return Mathf.Min(meleeLifestealCap, MeleeLifesteal + amount);
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
            case StatType.CritChance: return CritChance >= critChanceCap - 0.0001f;   // 75% unless the cap was raised
            case StatType.CritChanceCap: return critChanceCap >= 1f - 0.0001f;
            case StatType.Lifesteal: return Lifesteal >= lifestealCap - 0.0001f;
            case StatType.MeleeLifesteal: return MeleeLifesteal >= meleeLifestealCap - 0.0001f;
            case StatType.RegenAmount: return regenAmount >= regenCap - 0.0001f;
            case StatType.MeleeRange: return meleeRange >= meleeRangeCap - 0.0001f;
            default: return false;
        }
    }

    float CalcDamage(float flat, float percent) => (baseDamage + flat) * (1f + percent);
    float CalcSpeed(float flat, float percent) => (BaseSpeed + flat) * (1f + percent);
}

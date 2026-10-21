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

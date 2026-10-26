using UnityEngine;

public class XpOrb : Pickup
{
    public int Amount;

    protected override void Collect()
    {
        player.AddXp(Amount);
        AudioManager.Instance.Play(Sfx.XpPickup);
    }
}

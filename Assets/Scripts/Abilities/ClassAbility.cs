using UnityEngine;

public abstract class ClassAbility : ScriptableObject
{
    public string Title;
    [TextArea] public string Description;
    public Sprite Icon;
    public float Cooldown = 10f;

    public abstract void Activate(Transform player);
}

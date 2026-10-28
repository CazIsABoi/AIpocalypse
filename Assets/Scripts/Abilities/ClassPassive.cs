using UnityEngine;
public abstract class ClassPassive : ScriptableObject
{
    public string Title;
    [TextArea] public string Description;
    public Sprite Icon;
    public abstract void Attach(GameObject player);
}

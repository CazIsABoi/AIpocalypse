using UnityEngine;

public abstract class GameEvent : ScriptableObject
{
    public string Title;        // show this on screen when it starts
    public int Weight = 10;
    public float Duration;      // 0 = Wait until IsDone

    public abstract void Begin(EventContext ctx);
    public virtual void End(EventContext ctx) { }
    public virtual bool IsDone(EventContext ctx) => true;
    public virtual void Tick(EventContext ctx) { }
    public virtual float TimeLeft(float elapsed) => Duration - elapsed;
}
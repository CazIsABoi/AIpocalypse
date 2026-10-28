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
    public virtual bool Started => true;   // false = Begin couldn't set it up, so the director skips it silently
    public virtual string Status => "";    // optional progress next to the title, e.g. "12 / 34"
    public virtual string Outcome => "";   // telemetry: how it ended, e.g. "broke 34/40"
}
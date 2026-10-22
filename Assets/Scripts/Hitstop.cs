using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[AutoStaticsCleanup]
partial class Hitstop : MonoBehaviour
{
    public static Hitstop Instance;
    [SerializeField] float scale = 0.05f;   // almost frozen;
    float until;
    void Awake() => Instance = this;
    public void Stop(float duration)
    {
        until = Mathf.Max(until, Time.unscaledTime + duration);   // overlapping hits EXTEND, they don't add up
        GamePause.SetSlow(scale);
    }

    void Update()
    {
        if (until > 0 && Time.unscaledTime > until)
        {
            until = 0;
            GamePause.SetSlow(1f);
        }
    }
}
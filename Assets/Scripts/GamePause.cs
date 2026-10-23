using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum PauseReason { LevelUp, FloorChange, PauseMenu, Death, Cutscene }

[AutoStaticsCleanup]
partial class GamePause
{
    static readonly HashSet<PauseReason> reasons = new HashSet<PauseReason>();

    public static bool IsPaused => reasons.Count > 0;
    static float slowScale = 1f;
    public static void SetSlow(float scale) { slowScale = scale; Apply(); }
    public static bool Has(PauseReason reason) => reasons.Contains(reason);

    public static void Add(PauseReason reason) { reasons.Add(reason); Apply(); }
    public static void Remove(PauseReason reason) { reasons.Remove(reason); Apply(); }
    public static void Clear() { reasons.Clear(); slowScale = 1f; Apply(); }

    static void Apply() => Time.timeScale = reasons.Count > 0 ? 0f : slowScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        reasons.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) Clear(); }
}

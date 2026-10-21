using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum PauseReason { LevelUp, FloorChange, PauseMenu, Death }

[AutoStaticsCleanup]
partial class GamePause
{
    static readonly HashSet<PauseReason> reasons = new HashSet<PauseReason>();

    public static bool IsPaused => reasons.Count > 0;
    public static bool Has(PauseReason reason) => reasons.Contains(reason);

    public static void Add(PauseReason reason) { reasons.Add(reason); Apply(); }
    public static void Remove(PauseReason reason) { reasons.Remove(reason); Apply(); }
    public static void Clear() { reasons.Clear(); Apply(); }   // new scene / restart / quit

    static void Apply() => Time.timeScale = reasons.Count > 0 ? 0f : 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        reasons.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) Clear(); }
}

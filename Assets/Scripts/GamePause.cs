using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Why this exists: level-up, floor change, the pause menu and death all pause the game.
// When each one set Time.timeScale itself, closing one could unpause another
// (e.g. a floor change finishing after you died → alive at 0 HP, then a freeze).
//
// Rule: NEVER write Time.timeScale directly. Add your reason when you pause,
// remove ONLY your own reason when you're done. The game runs again only when no reasons are left.
//
//   GamePause.Add(PauseReason.LevelUp);      // open
//   GamePause.Remove(PauseReason.LevelUp);   // close (still paused if e.g. Death is in there)
//
// Reading Time.timeScale (e.g. "if (Time.timeScale == 0f) return;") is still fine.
// Need a new kind of pause? Add a value to the enum.
public enum PauseReason { LevelUp, FloorChange, PauseMenu, Death }

public static class GamePause
{
    static readonly HashSet<PauseReason> reasons = new HashSet<PauseReason>();

    public static bool IsPaused => reasons.Count > 0;
    public static bool Has(PauseReason reason) => reasons.Contains(reason);

    public static void Add(PauseReason reason) { reasons.Add(reason); Apply(); }
    public static void Remove(PauseReason reason) { reasons.Remove(reason); Apply(); }
    public static void Clear() { reasons.Clear(); Apply(); }   // new scene / restart / quit

    static void Apply() => Time.timeScale = reasons.Count > 0 ? 0f : 1f;

    // Statics survive between play sessions when domain reload is off, and between scene loads always.
    // So: start clean every time you press Play, and every time a scene loads (Restart, Menu).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        reasons.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) Clear(); }
}

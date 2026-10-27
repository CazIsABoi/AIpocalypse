#if STEAMWORKS_NET
using Steamworks;
#endif
using UnityEngine;

// Mirrors our own achievements to Steam. SWITCHED OFF until the game has a Steam App ID:
//   1. Install Steamworks.NET (Package Manager > Add package from git URL:
//      https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net)
//   2. Put the App ID in steam_appid.txt next to the Assets folder (for testing in the editor)
//   3. Create the achievements on the Steamworks site with the SAME API names as AchievementData.Id
//   4. Player Settings > Other Settings > Scripting Define Symbols: add STEAMWORKS_NET
// Until then every call here does nothing, and the game needs no Steam package to build.
public class SteamAchievements : MonoBehaviour
{
#if STEAMWORKS_NET
    static bool ready;

    // Runs once when the game starts, before the first scene: connects to Steam and keeps callbacks running
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        try { ready = SteamAPI.Init(); }
        catch (System.DllNotFoundException e) { Debug.LogWarning($"Steam: steam_api64.dll missing ({e.Message})"); }
        if (!ready) { Debug.LogWarning("Steam: not running or no App ID, achievements stay local"); return; }

        var go = new GameObject("Steam");
        DontDestroyOnLoad(go);
        go.AddComponent<SteamAchievements>();

        // achievements unlocked before Steam was hooked up (or while offline) reach Steam now
        foreach (var id in AchievementProgress.Unlocked) SteamUserStats.SetAchievement(id);
        SteamUserStats.StoreStats();
    }

    void Update() => SteamAPI.RunCallbacks();
    void OnApplicationQuit() { if (ready) SteamAPI.Shutdown(); ready = false; }
#endif

    public static void Unlock(string id)
    {
#if STEAMWORKS_NET
        if (!ready) return;
        SteamUserStats.SetAchievement(id);
        SteamUserStats.StoreStats();   // without this Steam only shows it after the game closes
#endif
    }
}

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

// Writes what the game was doing into Player.log, so a crash or an exception can be traced afterwards:
// a system header at start, a line for every scene load / unload, context after every error, and
// "Quit normally" on a clean exit (a log that ends without it = the game crashed or was killed).
// Player.log: %USERPROFILE%\AppData\LocalLow\CLBFLRMS\Wrench It_ The AIpocalypse\Player.log
// Hard crashes also leave a folder with a dump in %TEMP%\CLBFLRMS\Wrench It_ The AIpocalypse\Crashes
[AutoStaticsCleanup]
public static partial class CrashLog
{
    static string scene = "?";
    static float sceneLoadedAt;
    static string lastError;
    static GameController game;
    static EntityHandler spawner;

    // Warning, not Log: release builds only keep warnings and up (BuildPerformance)
    public static void Note(string message) => Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "[Diag] {0}", message);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Application.logMessageReceived -= OnLog;   // -= first: never subscribed twice
        Application.logMessageReceived += OnLog;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        Application.lowMemory -= OnLowMemory;
        Application.lowMemory += OnLowMemory;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
        // exceptions outside Unity's own loop (threads, async code) would otherwise vanish without a trace
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandled;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException -= OnUnobservedTask;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTask;

        Note($"{Application.productName} {Application.version}{(Debug.isDebugBuild ? " (development)" : "")} | {Application.platform} | {SystemInfo.operatingSystem}");
        Note($"CPU {SystemInfo.processorType} x{SystemInfo.processorCount} | RAM {SystemInfo.systemMemorySize} MB | " +
             $"GPU {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsMemorySize} MB)");
        Note($"Screen {Screen.width}x{Screen.height} {Screen.fullScreenMode} @ {Screen.currentResolution.refreshRateRatio.value:0} Hz | " +
             $"quality {QualitySettings.names[QualitySettings.GetQualityLevel()]} | vsync {QualitySettings.vSyncCount} | fps cap {Application.targetFrameRate}");
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        scene = s.name;
        sceneLoadedAt = Time.realtimeSinceStartup;
        game = UnityEngine.Object.FindAnyObjectByType<GameController>();
        spawner = UnityEngine.Object.FindAnyObjectByType<EntityHandler>();
        Note($"Loaded {s.name} ({mode}) | {Memory()}");
    }

    static void OnSceneUnloaded(Scene s) => Note($"Unloaded {s.name} | {Memory()}");
    static void OnLowMemory() => Note($"LOW MEMORY warning from the OS | {Memory()}");
    static void OnQuitting() => Note($"Quit normally after {Time.realtimeSinceStartup / 60f:0.0} min");
    static void OnUnhandled(object sender, UnhandledExceptionEventArgs e) => Note($"Unhandled exception (terminating: {e.IsTerminating}): {e.ExceptionObject}");
    static void OnUnobservedTask(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e) => Note($"Exception in a task: {e.Exception}");

    // Unity already prints the error and its stack trace; this adds where the game was when it happened.
    // Only once per different message, so an error that repeats every frame doesn't flood the log.
    static void OnLog(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
        if (message == lastError) return;
        lastError = message;
        try { Note($"^ in {scene}, {Time.realtimeSinceStartup - sceneLoadedAt:0.0}s after it loaded, frame {Time.frameCount}{Context()}"); }
        catch { }   // never let the logger itself throw
    }

    // " | floor 12, threat x23.1" during a run, "" in the menus
    public static string Context()
    {
        if (game == null) return "";
        string threat = spawner != null ? $", threat x{spawner.Threat:0.0}" : "";
        return $" | floor {game.Floor}{threat}, paused {GamePause.IsPaused}";
    }

    public static string Memory() =>
        $"memory {Profiler.GetTotalAllocatedMemoryLong() / 1048576} MB, managed {GC.GetTotalMemory(false) / 1048576} MB";
}

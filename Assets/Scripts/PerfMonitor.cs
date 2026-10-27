using System;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Watches performance in every build: a summary line in Player.log every few seconds, a line for every
// hitch, and an F3 overlay (FPS, CPU / GPU time, draw calls, memory, enemy / bullet counts, frame graph).
// Reading it: GPU time higher than CPU main = rendering is the limit (shadows, overdraw, resolution, lights);
// CPU main higher = scripts, physics or too many objects. CPU / GPU times need Player Settings >
// Other Settings > Frame Timing Stats on (0 otherwise). Spawns itself, nothing to add to the scenes.
[AutoStaticsCleanup]
public partial class PerfMonitor : MonoBehaviour
{
    public static PerfMonitor Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        if (Instance != null) return;
        var go = new GameObject("PerfMonitor");
        DontDestroyOnLoad(go);
        go.AddComponent<PerfMonitor>();
    }

    const float logInterval = 10f;      // seconds between summary lines
    const float hitchMs = 50f;          // a frame slower than this is logged on its own
    const float overlayRefresh = 0.5f;  // overlay numbers update this often, so they're readable
    const int graphLength = 240;

    ProfilerRecorder drawCalls, setPassCalls, triangles, gcAllocated, gcUsed;
    readonly FrameTiming[] timing = new FrameTiming[1];
    double mainMs, renderMs, gpuMs;     // latest frame

    // this interval (summary line)
    readonly float[] frameMs = new float[4096];
    int frames;
    float intervalTime;
    double mainSum, renderSum, gpuSum;
    long drawSum, allocSum;
    int hitches, lastGcCount;
    float lastHitchLog = -10f;

    // this run (telemetry, reset by RunTelemetry)
    long runFrames, runDraws;
    double runMain, runGpu;
    int runHitches;
    public float RunCpuMs => runFrames > 0 ? (float)(runMain / runFrames) : 0f;
    public float RunGpuMs => runFrames > 0 ? (float)(runGpu / runFrames) : 0f;
    public float RunDrawCalls => runFrames > 0 ? (float)runDraws / runFrames : 0f;
    public int RunHitches => runHitches;
    public void ResetRun() { runFrames = runDraws = 0; runMain = runGpu = 0; runHitches = 0; }

    // overlay
    bool overlay;
    readonly float[] graph = new float[graphLength];
    int graphHead;
    float overlayTimer;
    string overlayText = "";
    GUIStyle style;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        gcUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
        lastGcCount = GC.CollectionCount(0);
    }

    void OnDisable()
    {
        drawCalls.Dispose();
        setPassCalls.Dispose();
        triangles.Dispose();
        gcAllocated.Dispose();
        gcUsed.Dispose();
    }

    void Update()
    {
        float ms = Time.unscaledDeltaTime * 1000f;

        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, timing) > 0)
        {
            mainMs = timing[0].cpuMainThreadFrameTime;
            renderMs = timing[0].cpuRenderThreadFrameTime;
            gpuMs = timing[0].gpuFrameTime;
        }
        long draws = Last(drawCalls);

        if (frames < frameMs.Length) frameMs[frames] = ms;
        frames++;
        intervalTime += Time.unscaledDeltaTime;
        mainSum += mainMs; renderSum += renderMs; gpuSum += gpuMs;
        drawSum += draws;
        allocSum += Last(gcAllocated);

        runFrames++; runMain += mainMs; runGpu += gpuMs; runDraws += draws;

        int gcCount = GC.CollectionCount(0);
        bool collected = gcCount != lastGcCount;
        lastGcCount = gcCount;

        if (ms > hitchMs && Time.frameCount > 10)
        {
            hitches++;
            runHitches++;
            if (Time.realtimeSinceStartup - lastHitchLog > 1f)   // a burst of slow frames: one line per second
            {
                lastHitchLog = Time.realtimeSinceStartup;
                CrashLog.Note($"[Perf] hitch {ms:0} ms (main {mainMs:0.0}, render {renderMs:0.0}, gpu {gpuMs:0.0}){(collected ? " + garbage collection" : "")} | {Counts()}{CrashLog.Context()}");
            }
        }

        if (intervalTime >= logInterval) LogSummary();

        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) overlay = !overlay;
        if (overlay)
        {
            graph[graphHead] = ms;
            graphHead = (graphHead + 1) % graphLength;
            overlayTimer -= Time.unscaledDeltaTime;
            if (overlayTimer <= 0f) { overlayTimer = overlayRefresh; overlayText = OverlayText(ms); }
        }
    }

    void LogSummary()
    {
        int n = Mathf.Min(frames, frameMs.Length);
        Array.Sort(frameMs, 0, n);
        float worst1 = frameMs[Mathf.Clamp(n - 1 - n / 100, 0, n - 1)];   // the slowest 1% of frames
        CrashLog.Note($"[Perf] {intervalTime:0}s: fps {frames / intervalTime:0} (1% low {1000f / Mathf.Max(0.01f, worst1):0}, worst {frameMs[n - 1]:0} ms, hitches {hitches}) | " +
                      $"main {mainSum / frames:0.0} ms, render {renderSum / frames:0.0}, gpu {gpuSum / frames:0.0} | draws {drawSum / frames} | " +
                      $"garbage {allocSum / intervalTime / 1024f:0} KB/s, managed {Last(gcUsed) / 1048576} MB | {Counts()}{CrashLog.Context()}");
        frames = 0; intervalTime = 0f; hitches = 0;
        mainSum = renderSum = gpuSum = 0; drawSum = allocSum = 0;
    }

    string OverlayText(float ms) =>
        $"FPS {1000f / Mathf.Max(0.01f, ms):0}  ({ms:0.0} ms)\n" +
        $"CPU main {mainMs:0.0}  render {renderMs:0.0}  GPU {gpuMs:0.0} ms\n" +
        $"Draws {Last(drawCalls)}  SetPass {Last(setPassCalls)}  Tris {StatNames.Compact(Last(triangles))}\n" +
        $"Garbage {Last(gcAllocated) / 1024f:0.0} KB/frame  Managed {Last(gcUsed) / 1048576} MB\n" +
        Counts();

    // What's alive right now: the usual suspects when a late run gets slow
    static string Counts() =>
        $"enemies {FindObjectsByType<EnemyController>().Length}, " +
        $"bullets {FindObjectsByType<ProjectileController>().Length}, " +
        $"popups {FindObjectsByType<DamagePopup>().Length}";

    static long Last(ProfilerRecorder r) => r.Valid ? r.LastValue : 0;

    void OnGUI()
    {
        if (!overlay) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };

        var box = new Rect(10f, Screen.height - 230f, 470f, 220f);
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);

        // frame-time graph: one bar per frame, green under 60 fps, yellow under 30, red slower
        const float graphHeight = 70f, scaleMs = 50f;
        for (int i = 0; i < graphLength; i++)
        {
            float v = graph[(graphHead + i) % graphLength];
            float h = Mathf.Min(1f, v / scaleMs) * graphHeight;
            GUI.color = v <= 16.7f ? Color.green : v <= 33.3f ? Color.yellow : Color.red;
            GUI.DrawTexture(new Rect(box.x + 10f + i * 1.8f, box.yMax - 10f - h, 1.8f, h), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(box.x + 10f, box.y + 6f, box.width - 20f, 130f), overlayText, style);
    }
}

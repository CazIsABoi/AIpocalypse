using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Runs once before the first scene loads, in every build (no object in the scene needed).
public static class BuildPerformance
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        // Debug.Log in a release build costs time and garbage (in the browser every line goes to the console),
        // so release builds drop plain logs. Warnings, errors and exceptions stay: they're what Player.log is for.
        if (!Debug.isDebugBuild) Debug.unityLogger.filterLogType = LogType.Warning;

#if UNITY_WEBGL && !UNITY_EDITOR
        // The web build uses the PC quality level; a browser has less headroom, so render a bit lighter.
        // Only in the built game: in the editor this would permanently change the URP asset.
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.renderScale = 0.85f;     // 85% resolution, upscaled: hard to notice, much cheaper
            urp.shadowDistance = 30f;    // was 50: the top-down camera never sees that far anyway
        }
#endif
    }
}

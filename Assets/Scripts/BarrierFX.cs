using System.Collections;
using UnityEngine;

// Fades a HexBarrier in when it spawns and out when it's removed (drives the shader's _Intensity).
// MaterialPropertyBlock = per-barrier value without creating a material copy for each one.
public class BarrierFX : MonoBehaviour
{
    [SerializeField] private float fadeInTime = 0.35f;
    [SerializeField] private float fadeOutTime = 0.4f;
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private Renderer rend;
    private MaterialPropertyBlock block;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    void OnEnable() => StartCoroutine(Fade(0f, 1f, fadeInTime, false));

    // Called instead of Destroy: stops blocking right away, fades, then removes itself
    public void FadeOut()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        StopAllCoroutines();
        StartCoroutine(Fade(1f, 0f, fadeOutTime, true));
    }

    IEnumerator Fade(float from, float to, float time, bool destroyAfter)
    {
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / time)   // unscaled: also works while paused (floor change)
        {
            Set(Mathf.Lerp(from, to, t));
            yield return null;
        }
        Set(to);
        if (destroyAfter) Destroy(gameObject);
    }

    void Set(float value)
    {
        rend.GetPropertyBlock(block);
        block.SetFloat(IntensityId, value);
        rend.SetPropertyBlock(block);
    }
}

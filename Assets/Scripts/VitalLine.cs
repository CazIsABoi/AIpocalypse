using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class VitalLine : MaskableGraphic
{
    [SerializeField] private int points = 200;
    [SerializeField] private float thickness = 3f;     // core line, pixels
    [SerializeField] private float glow = 10f;         // extra soft width on each side
    [SerializeField] private float repeats = 3f;       // periods across the bar
    [SerializeField] private float baseSpeed = 0.4f;
    [SerializeField] private float morphTime = 1f;

    int fromTier, toTier;
    float blend = 1f, scroll, speed;

    // RunTimerUI calls this every frame
    public void Show(int tier, Color tint, float threat)
    {
        if (tier != toTier) { fromTier = toTier; toTier = tier; blend = 0f; }
        color = tint;
        speed = baseSpeed * (1f + Mathf.Log(Mathf.Max(1f, threat)));   // threat 1 → 1x, 20 → 4x, 6000 → ~10x
    }

    void Update()
    {
        scroll = (scroll + Time.deltaTime * speed) % 1f;   // one period = 1, so wrapping changes nothing on screen
        blend = Mathf.Min(1f, blend + Time.deltaTime / morphTime);
        SetVerticesDirty();
    }

    Vector2[] pts, normals;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points < 2) return;
        if (pts == null || pts.Length != points) { pts = new Vector2[points]; normals = new Vector2[points]; }

        Rect r = rectTransform.rect;
        float half = thickness * 0.5f;
        float amplitude = Mathf.Max(0f, r.height * 0.5f - half - glow * 0.5f);   // the wave + most of its glow fit inside the bar

        // One extra period past each side of the bar: the line's square ends sit outside, where the RectMask2D cuts them off
        float periodWidth = r.width / Mathf.Max(0.01f, repeats);
        for (int i = 0; i < points; i++)
        {
            float t = i / (points - 1f);
            float x = t * (repeats + 2f) - 1f + scroll;
            float y = Mathf.Lerp(Wave(fromTier, x), Wave(toTier, x), blend);
            pts[i] = new Vector2(Mathf.Lerp(r.xMin - periodWidth, r.xMax + periodWidth, t), r.center.y + y * amplitude);
        }

        // One normal per point (averaged over both neighbours), so neighbouring quads share their edges: no gaps or "hairs"
        for (int i = 0; i < points; i++)
        {
            Vector2 dir = pts[Mathf.Min(i + 1, points - 1)] - pts[Mathf.Max(i - 1, 0)];
            normals[i] = new Vector2(-dir.y, dir.x).normalized;
        }

        for (int i = 0; i < points - 1; i++)
        {
            AddStrip(vh, i, -half, half, 1f, 1f);               // core
            AddStrip(vh, i, half, half + glow, 0.5f, 0f);       // glow above
            AddStrip(vh, i, -half, -half - glow, 0.5f, 0f);     // glow below
        }
    }

    // A quad from point i to i + 1, offset 'inner'..'outer' pixels along the normals, alpha fading from aIn to aOut
    void AddStrip(VertexHelper vh, int i, float inner, float outer, float aIn, float aOut)
    {
        Vector2 p0 = pts[i], p1 = pts[i + 1], n0 = normals[i], n1 = normals[i + 1];
        Color cIn = color; cIn.a *= aIn;
        Color cOut = color; cOut.a *= aOut;
        int v = vh.currentVertCount;
        vh.AddVert(p0 + n0 * inner, cIn, Vector2.zero);
        vh.AddVert(p0 + n0 * outer, cOut, Vector2.zero);
        vh.AddVert(p1 + n1 * outer, cOut, Vector2.zero);
        vh.AddVert(p1 + n1 * inner, cIn, Vector2.zero);
        vh.AddTriangle(v, v + 1, v + 2);
        vh.AddTriangle(v + 2, v + 3, v);
    }

    // x: any value, one period per 1.0 → y in −1..1. Organic (smooth) at tier 0, digital (hard steps) at the top.
    float Wave(int tier, float x)
    {
        float f = x - Mathf.Floor(x);   // 0..1 inside the period
        float sine = Mathf.Sin(f * Mathf.PI * 2f);
        switch (tier)
        {
            case 0: return sine;                                                    // HUMAN: calm sine
            case 1: return f > 0.55f && f < 0.7f ? Mathf.Round(sine * 3f) / 3f : sine;   // GLITCHING: a short stepped glitch each period
            case 2: return sine * 0.75f + Mathf.Sin(f * Mathf.PI * 10f) * 0.25f;   // UNSTABLE: jittery ripples on top
            case 3: return Mathf.Round(sine * 3f) / 3f;                             // CORRUPTED: the whole sine in steps
            case 4: return f * 2f - 1f;                                             // SYNTHETIC: sawtooth
            case 5: return f < 0.5f ? 1f : -1f;                                     // MACHINE: square wave
            case 6: return Hash(Mathf.Floor(f * 12f)) < 0.5f ? 1f : -1f;           // AI: 12 random bits per period (f, not x: scroll wraps at 1)
            default: return 0f;
        }
    }

    // Same input → same "random" 0..1, so the bits scroll instead of flickering
    static float Hash(float n)
    {
        float h = Mathf.Sin(n * 12.9898f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }
}

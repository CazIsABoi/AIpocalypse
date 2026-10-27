using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class VitalLine : MaskableGraphic
{
    [SerializeField] private int pointsPerPeriod = 120;   // hard edges (square, steps, bits) need a dense line
    [SerializeField] private float thickness = 3f;     // core line, pixels
    [SerializeField] private float glow = 10f;         // extra soft width on each side
    [SerializeField] private float repeats = 3f;       // periods across the bar
    [SerializeField] private float baseSpeed = 0.4f;
    [SerializeField] private float morphTime = 1f;

    [Header("Jolts (higher tiers)")]
    [SerializeField] private int joltFromTier = 2;          // UNSTABLE and up
    [SerializeField] private float joltsPerSecond = 0.25f;  // per tier above joltFromTier - 1
    [SerializeField] private float joltTime = 0.12f;
    [SerializeField] private float joltWidth = 0.04f;       // of the bar's width
    [SerializeField] private int shakeFromTier = 6;         // AI: the whole line trembles
    [SerializeField] private float shake = 1.5f;            // pixels

    int fromTier, toTier;
    float blend = 1f, scroll, speed;
    float joltTimer, joltAt, joltSize;

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

        joltTimer -= Time.deltaTime;
        float rate = joltsPerSecond * (toTier - joltFromTier + 1);
        if (rate > 0f && joltTimer <= 0f && Random.value < rate * Time.deltaTime)
        {
            joltTimer = joltTime;
            joltAt = Random.value;
            joltSize = Random.Range(0.6f, 1f) * (Random.value < 0.5f ? -1f : 1f);
            if (Random.value < 0.5f) scroll = (scroll + Random.Range(0.1f, 0.3f)) % 1f;   // sometimes the signal skips too
        }
        SetVerticesDirty();
    }

    Vector2[] pts, offsets;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int points = Mathf.Max(2, Mathf.RoundToInt(pointsPerPeriod * (repeats + 2f)));
        if (pts == null || pts.Length != points) { pts = new Vector2[points]; offsets = new Vector2[points]; }

        Rect r = rectTransform.rect;
        float half = thickness * 0.5f;
        float amplitude = Mathf.Max(0f, r.height * 0.5f - half - glow);   // the wave and all of its glow fit inside the bar
        Vector2 trembling = toTier >= shakeFromTier ? Random.insideUnitCircle * shake : Vector2.zero;

        // One extra period past each side of the bar: the line's square ends sit outside, where the RectMask2D cuts them off
        float periodWidth = r.width / Mathf.Max(0.01f, repeats);
        float extra = 1f / (repeats + 2f);   // share of the line outside the bar on each side
        for (int i = 0; i < points; i++)
        {
            float t = i / (points - 1f);
            float x = t * (repeats + 2f) - 1f + scroll;
            float y = Mathf.Lerp(Wave(fromTier, x), Wave(toTier, x), blend);
            if (joltTimer > 0f)
            {
                float d = (Mathf.InverseLerp(extra, 1f - extra, t) - joltAt) / joltWidth;
                y += joltSize * 1.5f * Mathf.Exp(-d * d);
            }
            y = Mathf.Clamp(y, -1f, 1f);
            pts[i] = new Vector2(Mathf.Lerp(r.xMin - periodWidth, r.xMax + periodWidth, t), r.center.y + y * amplitude) + trembling;
        }

        // Mitered offsets: the line keeps its width through sharp corners instead of pinching thin.
        // Neighbouring quads share them, so there are no gaps or "hairs".
        for (int i = 0; i < points; i++)
        {
            Vector2 a = i > 0 ? SegmentNormal(i - 1) : SegmentNormal(i);
            Vector2 b = i < points - 1 ? SegmentNormal(i) : a;
            Vector2 miter = (a + b).normalized;
            if (miter == Vector2.zero) miter = a;
            offsets[i] = miter / Mathf.Max(0.35f, Vector2.Dot(miter, a));   // at most ~3x wider in the sharpest corners
        }

        for (int i = 0; i < points - 1; i++)
        {
            AddStrip(vh, i, -half, half, 1f, 1f);               // core
            AddStrip(vh, i, half, half + glow, 0.5f, 0f);       // glow above
            AddStrip(vh, i, -half, -half - glow, 0.5f, 0f);     // glow below
        }
    }

    Vector2 SegmentNormal(int i)
    {
        Vector2 dir = (pts[i + 1] - pts[i]).normalized;
        return new Vector2(-dir.y, dir.x);
    }

    // A quad from point i to i + 1, offset 'inner'..'outer' pixels along the offsets, alpha fading from aIn to aOut
    void AddStrip(VertexHelper vh, int i, float inner, float outer, float aIn, float aOut)
    {
        Vector2 p0 = pts[i], p1 = pts[i + 1], n0 = offsets[i], n1 = offsets[i + 1];
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

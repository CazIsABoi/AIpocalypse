using UnityEngine;

// A rectangle of floor cells with its own wear texture.
// Rooms and corridors each have one, so the wear system treats them the same.
public class WearSurface
{
    public Vector2Int Origin;     // global cell of the bottom-left corner
    public int Width, Height;
    public Texture2D Texture;     // 1 pixel per cell, red channel = wear
    public float[,] Wear;         // 0 = untouched, 1 = fully worn

    public bool Contains(Vector2Int globalCell)
    {
        Vector2Int local = globalCell - Origin;
        return local.x >= 0 && local.x < Width && local.y >= 0 && local.y < Height;
    }

    // Returns true if the texture changed (so the generator knows to upload it)
    public bool AddWear(Vector2Int globalCell, float amount)
    {
        Vector2Int local = globalCell - Origin;
        float old = Wear[local.x, local.y];
        float now = Mathf.Min(1f, old + amount);
        if (now == old) return false;

        Wear[local.x, local.y] = now;
        Texture.SetPixel(local.x, local.y, new Color(now, 0, 0, 1));
        return true;
    }
}

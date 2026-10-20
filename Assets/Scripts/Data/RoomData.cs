using System.Collections.Generic;
using UnityEngine;

class RoomData
{
    public Vector2Int slot;
    public int width;
    public int height;
    public Transform root;
    public bool[] doors = new bool[4]; // 0 = N, 1 = E, 2 = S, 3 = W
    public bool explored;
    public HashSet<Vector2Int> blocked;
    public HashSet<Vector2Int> solid;     // cells nothing can walk through (walls + props)
    public HashSet<Vector2Int> lastFill;  // debug: cells the last flood fill reached

    public WearSurface surface;           // the room's floor + wear texture
    public Renderer fog;
    public Renderer mapQuad;              // the room's rectangle on the minimap
}
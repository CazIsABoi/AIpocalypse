using System.Collections.Generic;
using UnityEngine;

class RoomData
{
    public Vector2Int slot;
    public int width;
    public int height;
    public Transform root;
    public bool[] doors = new bool[4]; // 0 = N, 1 = E, 2 = S, 3 = W
    public HashSet<Vector2Int> blocked;
    public HashSet<Vector2Int> solid;     // cells nothing can walk through (walls + props)
    public HashSet<Vector2Int> lastFill;  // debug: cells the last flood fill reached

    public Renderer floor;                // the room's single floor object
    public Texture2D trailTex;            // 1 pixel per cell, red channel = wear
    public float[,] wear;                 // 0 = untouched, 1 = fully worn
}
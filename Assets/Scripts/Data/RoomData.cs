using System.Collections.Generic;
using UnityEngine;

class RoomData
{
    public Vector2Int slot;
    public int width;
    public int height;
    public RoomShape shape;
    public Transform root;
    public bool[] doors = new bool[4]; // 0 = N, 1 = E, 2 = S, 3 = W
    public int distance;
    public RoomRole role;
    public RoomContent content;           // what goes in this room (decided in AssignRoles, null = empty)
    public bool explored;
    public HashSet<Vector2Int> blocked;
    public HashSet<Vector2Int> solid;     // cells nothing can walk through (walls + props)
    public HashSet<Vector2Int> lastFill;  // debug: cells the last flood fill reached

    public WearSurface surface;           // the room's floor + wear texture
    public Renderer fog;
    public Renderer mapQuad;              // the room's rectangle on the minimap
}
public enum RoomRole { Normal, Start, Core, Treasure }
public enum RoomShape { Rectangle, L, Half, Cross, Diagonal, Donut }
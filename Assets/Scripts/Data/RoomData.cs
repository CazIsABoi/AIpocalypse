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
}

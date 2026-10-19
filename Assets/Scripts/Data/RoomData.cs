using UnityEngine;
class RoomData 
{ 
    public Vector2Int slot;
    public bool[] doors = new bool[4]; // 0 = N, 1 = E, 2 = S, 3 = W
}

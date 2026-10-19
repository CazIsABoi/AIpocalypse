using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Room Content")]
public class RoomContent : ScriptableObject
{
    public int weight = 1;          // how often this set is picked
    public int minRoomSize = 11;    // smallest room it fits in (use min of width/height)
    public PlacementPattern pattern;
    public GameObject[] props;      // random pick per placed prop
    public float density = 0.05f;   // props per interior cell (10e)
    public int spacing = 4;
}

public enum PlacementPattern { Scatter, PillarGrid }
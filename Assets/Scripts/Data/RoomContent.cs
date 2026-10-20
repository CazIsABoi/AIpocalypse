using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Room Content")]
public class RoomContent : ScriptableObject
{
    public int weight = 1;          // how often this set is picked
    public int minRoomSize = 11;    // smallest room it fits in
    public PlacementPattern pattern;
    public GameObject[] props;      // random pick per placed prop
    public bool walkable;   // e.g. floor hazards: placed on cells, but don't block movement
    public float density = 0.05f;   // props per interior cell (10e)
    public int spacing = 4;
    public Material mapMaterial;      // minimap color of each prop (empty = normal obstacle color)
    public Material mapRoomMaterial;  // minimap color of the whole room (empty = normal room color)

    [Header("Rotation")]
    public PropRotation rotation;   // how placed props are turned
    [Header("Scale")]
    public bool randomScale;          // off = keep the prefab's normal scale

    [Header("Floor")]
    public bool tintFloor;                     // off = keep the floor material's normal color
    public Color floorColor = Color.white;     // the room's clean floor color when tintFloor is on
}

public enum PlacementPattern { Scatter, PillarGrid, Center, Rows, Perimeter, Ring, Clusters }
public enum PropRotation { Random90, None, Random180, RandomAny }
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private InputActionReference devResetKey;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorTile;
    [SerializeField] private GameObject wallTile;
    [SerializeField] private float wallTileYOffset;

    [Header("Layout Settings")]
    [SerializeField] private int maxRooms = 10; // Maximum number of rooms to generate
    [SerializeField] private int minRooms = 5; // Minimum number of rooms to generate
    [SerializeField] private float branchChance = 0.5f; // Chance to create a branch from the current room
    [SerializeField] private float maxNeighbors = 1; // Maximum number of neighboring rooms allowed for a new room
    [SerializeField] private float loopChance = 0.3f; // Chance to create a loop between rooms

    [Header("Room Settings")]
    [SerializeField] private int width = 11; // Room width in tiles
    [SerializeField] private int height = 11; // Room height in tiles
    [SerializeField] private float cellSize = 1f; // Size of each tile in cells
    [SerializeField] private int doorWidth = 3; // Big enough for navmesh agents
    [SerializeField] private int slotSize = 15; // Size of the room slot in cells

    Dictionary<Vector2Int, RoomData> rooms = new Dictionary<Vector2Int, RoomData>();

    static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    private void Start()
    {
        Generate();
    }

    void GenerateLayout()
    {
        rooms.Clear();
        var queue = new Queue<RoomData>();

        // create start room at (0,0), add to rooms, enqueue it
        var startRoom = new RoomData { slot = new Vector2Int(0, 0), doors = new bool[4] { false, false, false, false } };
        rooms[startRoom.slot] = startRoom;
        queue.Enqueue(startRoom);

        while (queue.Count > 0)
        {
            RoomData current = queue.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                Vector2Int next = current.slot + Dirs[i];

                // if (should skip) continue;
                if (rooms.ContainsKey(next)) continue;
                if (rooms.Count >= maxRooms) continue;
                if (Random.value > branchChance) continue;
                if (CountFilledNeighbours(next) > maxNeighbors) continue;
                foreach (RoomData room in rooms.Values) // check for loops
                {
                    if (room.slot == next) continue;
                    if (CountFilledNeighbours(room.slot) < 2) continue;
                    if (Random.value > loopChance) continue;
                    current.doors[i] = true;
                    room.doors[(i + 2) % 4] = true;
                }
                var neighbourRoom = new RoomData { slot = next, doors = new bool[4] { false, false, false, false } };
                rooms[next] = neighbourRoom;
                current.doors[i] = true;
                neighbourRoom.doors[(i + 2) % 4] = true;
                queue.Enqueue(neighbourRoom);
            } 
        }
    }

    private void Generate()
    {  
        // Clear existing rooms
        foreach (Transform child in transform) Destroy(child.gameObject);

        int attempts = 0;
        do { GenerateLayout(); attempts++; }
        while (rooms.Count < minRooms && attempts < 100);
        Debug.Log($"Generated {rooms.Count} rooms in {attempts} attempts.");

        foreach (var room in rooms.Values)
        {
            BuildRoom(room);
        }
        BuildCorridors();

    }
    
    private void BuildRoom(RoomData room)
    {
        Vector2Int origin = room.slot * slotSize;
        GameObject roomObj = new GameObject($"Room {room.slot}");
        roomObj.transform.SetParent(transform);

        // Generate floor tiles for rooms
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = CellToWorld(origin, x, y);
                Instantiate(floorTile, position, Quaternion.identity, roomObj.transform);

                // Generate walls
                if (x == width - 1 || y == height - 1 || x == 0 || y == 0)
                {
                    if ((x == 0 && room.doors[3] && InDoorGap(y, height)) ||
                        (x == width - 1 && room.doors[1] && InDoorGap(y, height)) ||
                        (y == 0 && room.doors[2] && InDoorGap(x, width)) ||
                        (y == height - 1 && room.doors[0] && InDoorGap(x, width)))
                    {
                        // Skip wall generation for doors
                        continue;
                    }
                    else
                    {
                        Vector3 wallPosition = CellToWorld(origin, x, y);
                        wallPosition.y += wallTileYOffset;
                        Instantiate(wallTile, wallPosition, Quaternion.identity, roomObj.transform);
                    }
                }
            }
        }
    }

    void BuildCorridors()
    {
        foreach (var room in rooms.Values)
        {
            if (room.doors[1]) BuildCorridor(room, horizontal: true);   // East
            if (room.doors[0]) BuildCorridor(room, horizontal: false);  // North
        }
    }

    void BuildCorridor(RoomData room, bool horizontal)
    {
        Vector2Int origin = room.slot * slotSize;
        GameObject roomObj = new GameObject($"Corridor {room.slot} { (horizontal ? "E" : "N") }");
        roomObj.transform.SetParent(transform);

        int roomLength = horizontal ? width : height;
        int crossLength = horizontal ? height : width;
        int gapStart = DoorStart(crossLength);
        int gapEnd = (gapStart + doorWidth);

        for (int along = roomLength; along < slotSize; along++)        
        {
            for (int across = gapStart - 1; across <= gapEnd; across++) 
            {
                int x = horizontal ? along : across;
                int y = horizontal ? across : along;
                Vector3 position = CellToWorld(origin, x, y);
                Instantiate(floorTile, position, Quaternion.identity, roomObj.transform);
                // Generate walls
                if (across == gapStart - 1 || across == gapEnd)
                {
                    Vector3 wallPosition = CellToWorld(origin, x, y);
                    wallPosition.y += wallTileYOffset;
                    Instantiate(wallTile, wallPosition, Quaternion.identity, roomObj.transform);     
                }
            }
        }
    }

    void Update()
    {
    #if UNITY_EDITOR || DEBUG
        if (devResetKey.action.triggered) { Generate(); }
    #endif
    }

    int CountFilledNeighbours(Vector2Int slot) { 
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (rooms.ContainsKey(slot + Dirs[i])) count++;
        }
        return count;
    }

    private bool InDoorGap(int pos, int length)
    {
        return pos >= DoorStart(length) && pos < DoorStart(length) + doorWidth;
    }

    private int DoorStart(int length)
    {
        return (length - doorWidth) / 2;
    }

    Vector3 CellToWorld(Vector2Int origin, int x, int y)
        => new Vector3((origin.x + x + 0.5f) * cellSize, 0, (origin.y + y + 0.5f) * cellSize);

    private void OnDrawGizmos()
    {
        if (rooms == null) return;
        foreach (var room in rooms.Values)
        {
            Vector3 center = new Vector3((room.slot.x * slotSize + width / 2f) * cellSize, 0, (room.slot.y * slotSize + height / 2f) * cellSize);
            Vector3 size = new Vector3(width * cellSize, 0, height * cellSize);

            Gizmos.DrawWireCube(center, size);

            // Draw line from center to neighbors center if there is an open door
            for (int i = 0; i < 4; i++)
            {
                if (room.doors[i])
                {
                    Gizmos.color = Color.green;
                    Vector2Int neighborSlot = room.slot + Dirs[i];
                    if (rooms.ContainsKey(neighborSlot))
                    {
                        Vector3 neighborCenter = new Vector3((neighborSlot.x * slotSize + width / 2f) * cellSize, 0, (neighborSlot.y * slotSize + height / 2f) * cellSize);
                        Gizmos.DrawLine(center, neighborCenter);
                    }
                    Gizmos.color = Color.lightCyan;
                }
            }
        }
    }
}

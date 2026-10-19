using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private InputActionReference devResetKey;

    [Header("References")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorTile;
    [SerializeField] private GameObject wallTile;

    [Header("Layout Settings")]
    [SerializeField] private int maxRooms = 10; // Maximum number of rooms to generate
    [SerializeField] private int minRooms = 5; // Minimum number of rooms to generate
    [SerializeField] private float branchChance = 0.5f; // Chance to create a branch from the current room
    [SerializeField] private float maxNeighbors = 1; // Maximum number of neighboring rooms allowed for a new room
    [SerializeField] private float loopChance = 0.3f; // Chance to create a loop between rooms
    public int seed = 0; // Seed for random number generation
    [SerializeField] private bool useRandomSeed = true; // Whether to use a random seed or the specified seed

    [Header("Room Settings")]
    [SerializeField] private int minWidth = 11; // Room width in tiles
    [SerializeField] private int minHeight = 11; // Room height in tiles
    [SerializeField] private int maxWidth = 21; // Room width in tiles
    [SerializeField] private int maxHeight = 21; // Room height in tiles
    [SerializeField] private float cellSize = 1f; // Size of each tile in cells
    [SerializeField] private int doorWidth = 3; // Big enough for navmesh agents
    [SerializeField] private int slotSize = 23; // Size of the room slot in cells
    [SerializeField] private int laneDepth = 4; // Depth of the corridor lane in cells
    [SerializeField] List<RoomContent> contentSets;

    [Header("Debug")]
    [SerializeField] private bool showBlocked = true;
    [SerializeField] private bool showSolid = true;
    [SerializeField] private bool showFill = true;
    [SerializeField] private Vector2Int debugRoomSlot;          // which room to animate
    [SerializeField] private float fillStepDelay = 0.02f;      // seconds per step

    Dictionary<Vector2Int, RoomData> rooms = new Dictionary<Vector2Int, RoomData>();

    static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    private void Start()
    {
        StartCoroutine(GenerateRoutine());
    }

    void GenerateLayout()
    {
        rooms.Clear();
        var queue = new Queue<RoomData>();

        // create start room at (0,0), add to rooms, enqueue it
        var startRoom = CreateRoom(Vector2Int.zero);
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
                var neighbourRoom = CreateRoom(next);
                rooms[next] = neighbourRoom;
                current.doors[i] = true;
                neighbourRoom.doors[(i + 2) % 4] = true;
                queue.Enqueue(neighbourRoom);
            } 
        }
        AddLoops();
    }
    RoomData CreateRoom(Vector2Int slot)
    {
        return new RoomData { slot = slot, width = Random.Range(minWidth / 2, maxWidth / 2 + 1) * 2 + 1, height = Random.Range(minHeight / 2, maxHeight / 2 + 1) * 2 + 1, doors = new bool[4] { false, false, false, false } };
    }

    void AddLoops()
    {
        foreach (RoomData room in rooms.Values)
        {
            for (int i = 0; i < 2; i++)   
            {
                Vector2Int neighbourSlot = room.slot + Dirs[i];

                if (!rooms.TryGetValue(neighbourSlot, out RoomData neighbour)) continue;
                if (room.doors[i]) continue;
                if (Random.value > loopChance) continue;

                room.doors[i] = true;
                neighbour.doors[(i + 2) % 4] = true;
            }
        }
    }

    public IEnumerator GenerateRoutine()
    {
        if (useRandomSeed) seed = System.Environment.TickCount;
        Random.InitState(seed);
        Debug.Log($"Generating dungeon with seed: {seed}");
        // Clear existing rooms
        foreach (Transform child in transform) Destroy(child.gameObject);
        yield return null;          

        int attempts = 0;
        do { GenerateLayout(); attempts++; }
        while (rooms.Count < minRooms && attempts < 100);
        Debug.Log($"Generated {rooms.Count} rooms in {attempts} attempts.");

        foreach (var room in rooms.Values)
        {
            BuildRoom(room);
        }
        BuildCorridors();
        foreach (var room in rooms.Values) PopulateRoom(room);
        navMeshSurface.BuildNavMesh();
    }
    
    private void BuildRoom(RoomData room)
    {
        Vector2Int origin = room.slot * slotSize + RoomOffset(room);
        GameObject roomObj = new GameObject($"Room {room.slot}");
        roomObj.transform.SetParent(transform);

        // Generate floor tiles for rooms
        for (int x = 0; x < room.width; x++)
        {
            for (int y = 0; y < room.height; y++)
            {
                Vector3 position = CellToWorld(origin, x, y);
                Instantiate(floorTile, position, Quaternion.identity, roomObj.transform);

                // Generate walls
                if (x == room.width - 1 || y == room.height - 1 || x == 0 || y == 0)
                {
                    if ((x == 0 && room.doors[3] && InDoorGap(y, room.height)) ||
                        (x == room.width - 1 && room.doors[1] && InDoorGap(y, room.height)) ||
                        (y == 0 && room.doors[2] && InDoorGap(x, room.width)) ||
                        (y == room.height - 1 && room.doors[0] && InDoorGap(x, room.width)))
                    {
                        // Skip wall generation for doors
                        continue;
                    }
                    else
                    {
                        Vector3 wallPosition = CellToWorld(origin, x, y);
                        Instantiate(wallTile, wallPosition, Quaternion.identity, roomObj.transform);
                    }
                }
                room.root = roomObj.transform;  // Store the root transform for later use
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
        int dir = horizontal ? 1 : 0;                              // 1 = East, 0 = North
        RoomData neighbour = rooms[room.slot + Dirs[dir]];         // room B

        Vector2Int origin = room.slot * slotSize;                 // corner of A's SLOT (no offset!)
        Vector2Int offsetA = RoomOffset(room);
        Vector2Int offsetB = RoomOffset(neighbour);

        int start = horizontal ? offsetA.x + room.width : offsetA.y + room.height;
        int end = horizontal ? slotSize + offsetB.x : slotSize + offsetB.y;

        int gapStart = DoorStart(slotSize);
        int gapEnd = gapStart + doorWidth;

        GameObject corridorObj = new GameObject($"Corridor {room.slot} {(horizontal ? "E" : "N")}");
        corridorObj.transform.SetParent(transform);
        for (int along = start; along < end; along++)
        {
            for (int across = gapStart - 1; across <= gapEnd; across++) 
            {
                int x = horizontal ? along : across;
                int y = horizontal ? across : along;
                Vector3 position = CellToWorld(origin, x, y);
                Instantiate(floorTile, position, Quaternion.identity, corridorObj.transform);
                // Generate walls
                if (across == gapStart - 1 || across == gapEnd)
                {
                    Vector3 wallPosition = CellToWorld(origin, x, y);
                    Instantiate(wallTile, wallPosition, Quaternion.identity, corridorObj.transform);     
                }
            }
        }
    }

    private void PopulateRoom(RoomData room)
    {
        HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();   // no props allowed here
        HashSet<Vector2Int> solid = new HashSet<Vector2Int>();     // nothing can walk here
        room.blocked = blocked;
        room.solid = solid;

        // Wall ring: reserved AND solid
        foreach (var set in new[] { blocked, solid })
        {
            BlockRect(set, 0, 0, room.width, 1);                 // bottom
            BlockRect(set, 0, room.height - 1, room.width, 1);   // top
            BlockRect(set, 0, 0, 1, room.height);                // left
            BlockRect(set, room.width - 1, 0, 1, room.height);   // right
        }

        // Door lanes: reserved only (you can walk on them)
        if (room.doors[0]) BlockRect(blocked, DoorStart(room.width), room.height - 1 - laneDepth, doorWidth, laneDepth); // north
        if (room.doors[1]) BlockRect(blocked, room.width - 1 - laneDepth, DoorStart(room.height), laneDepth, doorWidth); // east
        if (room.doors[2]) BlockRect(blocked, DoorStart(room.width), 1, doorWidth, laneDepth);                          // south
        if (room.doors[3]) BlockRect(blocked, 1, DoorStart(room.height), laneDepth, doorWidth);                         // west

        if (room.slot != Vector2Int.zero)   // start room stays empty
        {
            RoomContent content = PickContent(room);
            if (content != null && content.props.Length > 0)
            {
                switch (content.pattern)
                {
                    case PlacementPattern.Scatter: PlaceScatter(room, content, blocked, solid); break;
                    case PlacementPattern.PillarGrid: PlacePillarGrid(room, content, blocked, solid); break;
                }
            }
        }

        if (!IsConnected(room, solid))
            Debug.LogWarning($"Room {room.slot} is not fully connected!");
    }
    RoomContent PickContent(RoomData room)
    {
        // 1. Make a temporary list of the sets that fit this room
        int roomSize = Mathf.Min(room.width, room.height);
        List<RoomContent> fitting = new List<RoomContent>();
        foreach (RoomContent set in contentSets)
        {
            if (roomSize >= set.minRoomSize) fitting.Add(set);
        }

        // 2. Nothing fits -> no content
        if (fitting.Count == 0) return null;

        // 3. Add up the weights
        int total = 0;
        foreach (RoomContent set in fitting) total += set.weight;

        // 4. Roll a number
        int roll = Random.Range(0, total);

        // 5. Walk the list, subtracting weights, until the roll goes below 0
        foreach (RoomContent set in fitting)
        {
            roll -= set.weight;
            if (roll < 0) return set;
        }

        return null;
    }
    void BlockRect(HashSet<Vector2Int> set, int x0, int y0, int w, int h)
    {
        for (int x = x0; x < x0 + w; x++)
            for (int y = y0; y < y0 + h; y++)
                set.Add(new Vector2Int(x, y));
    }
    bool IsConnected(RoomData room, HashSet<Vector2Int> solid)
    {
        // 1. Count positions where the 2×2 brush fits, and remember one to start from
        int total = 0;
        Vector2Int start = new Vector2Int(-1, -1);
        for (int x = 1; x < room.width - 1; x++)
            for (int y = 1; y < room.height - 1; y++)
            {
                Vector2Int c = new Vector2Int(x, y);
                if (!Fits(room, solid, c)) continue;
                total++;
                start = c;
            }

        if (total == 0) return false;

        // 2. Flood fill from start
        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int next = current + d;
                if (!Fits(room, solid, next)) continue;
                if (!visited.Add(next)) continue;
                queue.Enqueue(next);
            }
        }

        room.lastFill = visited;   // debug: remember what we reached

        // 3. Did we reach everything?
        return visited.Count == total;
    }

    void PlaceProp(RoomData room, RoomContent content, Vector2Int cell)
    {
        GameObject prefab = content.props[Random.Range(0, content.props.Length)];
        Vector3 pos = CellToWorld(RoomOrigin(room), cell.x, cell.y);
        Quaternion rot = Quaternion.Euler(0, 90 * Random.Range(0, 4), 0);
        Instantiate(prefab, pos, rot, room.root);
    }
    void PlaceScatter(RoomData room, RoomContent content, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        int interior = (room.width - 2) * (room.height - 2);
        int count = Mathf.RoundToInt(interior * content.density);

        int placed = 0;
        int attempts = 0;
        while (placed < count && attempts < count * 20)
        {
            attempts++;
            Vector2Int cell = new Vector2Int(Random.Range(1, room.width - 1),
                                             Random.Range(1, room.height - 1));

            if (blocked.Contains(cell) || solid.Contains(cell)) continue;  // reserved or taken
            if (!TryAddSolid(room, solid, cell)) continue;                 // would seal something off

            PlaceProp(room, content, cell);
            placed++;
        }
    }
    void PlacePillarGrid(RoomData room, RoomContent content, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        int cx = room.width / 2;
        int cy = room.height / 2;

        for (int x = 1; x < room.width - 1; x++)
        {
            for (int y = 1; y < room.height - 1; y++)
            {
                bool onGridX = Mathf.Abs(x - cx) % content.spacing == 0;
                bool onGridY = Mathf.Abs(y - cy) % content.spacing == 0;
                if (!onGridX || !onGridY) continue;

                Vector2Int cell = new Vector2Int(x, y);
                if (blocked.Contains(cell) || solid.Contains(cell)) continue;
                if (!TryAddSolid(room, solid, cell)) continue;

                PlaceProp(room, content, cell);
            }
        }
    }

    void Update()
    {
    #if UNITY_EDITOR || DEBUG
        if (devResetKey.action.triggered) { StartCoroutine(GenerateRoutine()); }
    #endif
    }

    // --------- Helpers ---------

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
    Vector2Int RoomOffset(RoomData room)
    => new Vector2Int((slotSize - room.width) / 2, (slotSize - room.height) / 2);
    Vector2Int RoomOrigin(RoomData room) => room.slot * slotSize + RoomOffset(room);
    Vector3 SlotCenter(Vector2Int slot)
    => new Vector3((slot.x * slotSize + slotSize / 2f) * cellSize, 0,
                   (slot.y * slotSize + slotSize / 2f) * cellSize);

    Vector3 CellToWorld(Vector2Int origin, int x, int y)
        => new Vector3((origin.x + x + 0.5f) * cellSize, 0, (origin.y + y + 0.5f) * cellSize);
    bool IsWalkable(RoomData room, HashSet<Vector2Int> solid, Vector2Int c)
    {
        bool inside = c.x >= 1 && c.x < room.width - 1 && c.y >= 1 && c.y < room.height - 1;
        return inside && !solid.Contains(c);
    }
    bool Fits(RoomData room, HashSet<Vector2Int> solid, Vector2Int c)
    {
        return IsWalkable(room, solid, c)
            && IsWalkable(room, solid, c + Vector2Int.right)
            && IsWalkable(room, solid, c + Vector2Int.up)
            && IsWalkable(room, solid, c + Vector2Int.one);
    }
    bool TryAddSolid(RoomData room, HashSet<Vector2Int> solid, Vector2Int cell)
    {
        solid.Add(cell);                          // pretend the prop is there
        if (IsConnected(room, solid)) return true;

        solid.Remove(cell);                       // it sealed something off, so undo
        return false;
    }

    // -------- Debug --------
    private void OnDrawGizmos()
    {
        if (rooms == null) return;
        foreach (var room in rooms.Values)
        {
            Vector3 center = SlotCenter(room.slot);
            Vector3 size = new Vector3(room.width * cellSize, 0, room.height * cellSize);

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
                        Vector3 neighborCenter = SlotCenter(neighborSlot);
                        Gizmos.DrawLine(center, neighborCenter);
                    }
                    Gizmos.color = Color.lightCyan;
                }
            }

            Vector2Int origin = RoomOrigin(room);

            if (showBlocked && room.blocked != null)
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f);    // red: no props allowed
                foreach (Vector2Int c in room.blocked)
                    Gizmos.DrawCube(CellToWorld(origin, c.x, c.y), Vector3.one * cellSize * 0.8f);
            }

            if (showSolid && room.solid != null)
            {
                Gizmos.color = new Color(0, 0, 0, 0.6f);    // black: can't walk
                foreach (Vector2Int c in room.solid)
                    Gizmos.DrawCube(CellToWorld(origin, c.x, c.y) + Vector3.up * 0.3f, Vector3.one * cellSize * 0.5f);
            }

            if (showFill && room.lastFill != null)
            {
                Gizmos.color = new Color(0, 0.6f, 1, 0.5f); // blue: reached by the flood fill
                foreach (Vector2Int c in room.lastFill)
                    Gizmos.DrawCube(CellToWorld(origin, c.x, c.y) + Vector3.up * 0.6f, Vector3.one * cellSize * 0.4f);
            }
        }
    }

    [ContextMenu("Animate Flood Fill")]
    void AnimateFillMenu()
    {
        if (!rooms.TryGetValue(debugRoomSlot, out RoomData room) || room.solid == null)
        {
            Debug.LogWarning($"No populated room at {debugRoomSlot}");
            return;
        }
        StartCoroutine(AnimateFill(room));
    }

    IEnumerator AnimateFill(RoomData room)
    {
        // find a start position, the same way IsConnected does
        Vector2Int start = new Vector2Int(-1, -1);
        for (int x = 1; x < room.width - 1 && start.x < 0; x++)
            for (int y = 1; y < room.height - 1; y++)
                if (Fits(room, room.solid, new Vector2Int(x, y))) { start = new Vector2Int(x, y); break; }

        if (start.x < 0) yield break;

        var visited = new HashSet<Vector2Int> { start };
        room.lastFill = visited;          // the gizmos draw this set as it grows
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int next = current + d;
                if (!Fits(room, room.solid, next)) continue;
                if (!visited.Add(next)) continue;
                queue.Enqueue(next);
            }
            yield return new WaitForSeconds(fillStepDelay);   // pause so you can watch
        }
    }
}

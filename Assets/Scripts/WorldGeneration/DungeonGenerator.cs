using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private InputActionReference devResetKey;

    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorTile;
    [SerializeField] private GameObject wallTile;
    [SerializeField] private Material roomFloorMaterial;   // uses the RoomFloor shader graph
    [SerializeField] GameObject fogPrefab;

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
    [SerializeField] private float cellSize = 1f; // Size of each tile in world units
    [SerializeField] private int doorWidth = 3; // Big enough for navmesh agents
    [SerializeField] private int slotSize = 23; // Size of the room slot in cells
    [SerializeField] private int laneDepth = 4; // Depth of the door lane in cells
    [SerializeField] List<RoomContent> contentSets;
    Vector2Int lastPlayerSlot = new Vector2Int(int.MinValue, int.MinValue);

    [Header("Floor Path")]
    [SerializeField] private bool smoothPath = true; // true = soft blend between cells, false = sharp squares

    [Header("Fog")]
    [SerializeField] float fogHeight = 3f;
    [SerializeField] float fogFadeTime = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showBlocked = true;
    [SerializeField] private bool showSolid = true;
    [SerializeField] private bool showFill = true;
    [SerializeField] private Vector2Int debugRoomSlot;          // which room to animate
    [SerializeField] private float fillStepDelay = 0.02f;      // seconds per step

    Dictionary<Vector2Int, RoomData> rooms = new Dictionary<Vector2Int, RoomData>();
    HashSet<RoomData> dirtyRooms = new HashSet<RoomData>();    // rooms whose floor texture changed this frame
    public event System.Action Generated;
    public bool IsReady { get; private set; }
    public Vector3 StartPosition => SlotCenter(Vector2Int.zero) + Vector3.up * 1f;

    static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    private void Start()
    {
        StartCoroutine(GenerateRoutine());
    }

    void Update()
    {
        TrackPlayer();
#if UNITY_EDITOR || DEBUG
        if (devResetKey.action.triggered) { StartCoroutine(GenerateRoutine()); }
    #endif
    }

    void LateUpdate()
    {
        // Upload changed floor textures once per frame, however many cells changed
        if (dirtyRooms.Count == 0) return;
        foreach (RoomData room in dirtyRooms)
            if (room.trailTex != null) room.trailTex.Apply();
        dirtyRooms.Clear();
    }

    void OnDestroy()
    {
        FreeFloorTextures();
    }
    public bool TryGetSpawnPosition(out Vector3 position)
    {
        position = default;

        var candidates = new List<RoomData>();

        // 1. frontier: unexplored rooms with a door to an explored room
        foreach (RoomData room in rooms.Values)
            if (!room.explored && HasExploredNeighbour(room)) candidates.Add(room);

        // 2. fallback: explored rooms the player isn't in
        if (candidates.Count == 0)
            foreach (RoomData room in rooms.Values)
                if (room.explored && room.slot != lastPlayerSlot) candidates.Add(room);

        // still nothing? then there's nowhere to spawn this time
        if (candidates.Count == 0) return false;

        // 3. try a few random spots
        for (int attempt = 0; attempt < 20; attempt++)
        {
            RoomData room = candidates[Random.Range(0, candidates.Count)];
            Vector2Int cell = new Vector2Int(Random.Range(1, room.width - 1),
                                             Random.Range(1, room.height - 1));
            if (room.solid != null && room.solid.Contains(cell)) continue;   

            Vector3 world = CellToWorld(RoomOrigin(room), cell.x, cell.y);
            if (NavMesh.SamplePosition(world, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }
        }
        return false;
    }

    // -------- Generation --------

    public IEnumerator GenerateRoutine()
    {
        IsReady = false;
        lastPlayerSlot = new Vector2Int(int.MinValue, int.MinValue);
        // Clear the old dungeon (textures are NOT destroyed with GameObjects, so free them ourselves)
        FreeFloorTextures();
        foreach (Transform child in transform) Destroy(child.gameObject);
        yield return null;

        if (useRandomSeed) seed = System.Environment.TickCount;
        Random.InitState(seed);
        Debug.Log($"Generating dungeon with seed: {seed}");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        int attempts = 0;
        do { GenerateLayout(); attempts++; }
        while (rooms.Count < minRooms && attempts < 100);
        Debug.Log($"Layout: {sw.ElapsedMilliseconds} ms ({rooms.Count} rooms in {attempts} attempts)"); sw.Restart();

        foreach (var room in rooms.Values) BuildRoom(room);
        Debug.Log($"Build rooms: {sw.ElapsedMilliseconds} ms"); sw.Restart();

        BuildCorridors();
        Debug.Log($"Build corridors: {sw.ElapsedMilliseconds} ms"); sw.Restart();

        foreach (var room in rooms.Values) PopulateRoom(room);
        Debug.Log($"Populate rooms: {sw.ElapsedMilliseconds} ms"); sw.Restart();

        navMeshSurface.BuildNavMesh();
        Debug.Log($"Build nav mesh: {sw.ElapsedMilliseconds} ms");

        IsReady = true;
        Generated?.Invoke();
    }

    void GenerateLayout()
    {
        rooms.Clear();
        var queue = new Queue<RoomData>();

        // create start room at (0,0), add to rooms, enqueue it
        var startRoom = CreateRoom(Vector2Int.zero);
        rooms[startRoom.slot] = startRoom;
        startRoom.explored = true;
        queue.Enqueue(startRoom);

        int passes = 0;
        do
        {
            while (queue.Count > 0)
            {
                RoomData current = queue.Dequeue();

                for (int i = 0; i < 4; i++)
                {
                    Vector2Int next = current.slot + Dirs[i];

                    if (rooms.ContainsKey(next)) continue;
                    if (rooms.Count >= maxRooms) continue;
                    if (Random.value > branchChance) continue;
                    if (CountFilledNeighbours(next) > maxNeighbors) continue;

                    var neighbourRoom = CreateRoom(next);
                    rooms[next] = neighbourRoom;
                    current.doors[i] = true;
                    neighbourRoom.doors[(i + 2) % 4] = true;
                    queue.Enqueue(neighbourRoom);
                } // end for (directions)
            } // end while (queue)

            // queue is empty: this pass is done
            if (rooms.Count >= minRooms) break;                            // enough rooms, stop
            foreach (RoomData room in rooms.Values) queue.Enqueue(room);   // give every room another chance
            passes++;
        }
        while (passes < 20); // end do

        AddLoops();
    }

    RoomData CreateRoom(Vector2Int slot)
    {
        return new RoomData
        {
            slot = slot,
            width = Random.Range(minWidth / 2, maxWidth / 2 + 1) * 2 + 1,
            height = Random.Range(minHeight / 2, maxHeight / 2 + 1) * 2 + 1,
            doors = new bool[4]
        };
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

    // -------- Building --------

    private void BuildRoom(RoomData room)
    {
        Vector2Int origin = RoomOrigin(room);
        GameObject roomObj = new GameObject($"Room {room.slot}");
        roomObj.transform.SetParent(transform);
        room.root = roomObj.transform;

        BuildRoomFloor(room);

        // Walls only (the floor is one object now)
        for (int x = 0; x < room.width; x++)
        {
            for (int y = 0; y < room.height; y++)
            {
                bool border = x == 0 || y == 0 || x == room.width - 1 || y == room.height - 1;
                if (!border) continue;

                bool door = (x == 0 && room.doors[3] && InDoorGap(y, room.height)) ||
                            (x == room.width - 1 && room.doors[1] && InDoorGap(y, room.height)) ||
                            (y == 0 && room.doors[2] && InDoorGap(x, room.width)) ||
                            (y == room.height - 1 && room.doors[0] && InDoorGap(x, room.width));
                if (door) continue;

                Instantiate(wallTile, CellToWorld(origin, x, y), Quaternion.identity, room.root);
            }
        }
        if (!room.explored) CreateFog(room);
    }
    void CreateFog(RoomData room)
    {
        Vector3 position = SlotCenter(room.slot) + Vector3.up * fogHeight;
        Quaternion rotation = Quaternion.Euler(90, 0, 0);

        GameObject fog = Instantiate(fogPrefab, position, rotation, room.root);
        fog.transform.localScale = Vector3.one * slotSize * cellSize * 1.05f;

        room.fog = fog.GetComponent<Renderer>();
    }
    IEnumerator FadeFog(Renderer fog)
    {
        Material mat = fog.material;     // this room's own copy, so only this fog fades
        Color c = mat.color;

        for (float t = 0; t < 1; t += Time.deltaTime / fogFadeTime)
        {
            if (fog == null) yield break;   // dungeon regenerated mid-fade
            c.a = 1 - t;
            mat.color = c;
            yield return null;              // wait one frame
        }

        Destroy(fog.gameObject);
        Destroy(mat);                       // .material copies aren't cleaned up automatically
    }

    void BuildRoomFloor(RoomData room)
    {
        Vector2Int origin = RoomOrigin(room);
        Vector3 corner = new Vector3(origin.x * cellSize, 0, origin.y * cellSize);    // bottom-left corner, world
        Vector3 size = new Vector3(room.width * cellSize, 0, room.height * cellSize);

        // One floor object, scaled to cover the whole room
        GameObject floor = Instantiate(floorTile, corner + size / 2f, Quaternion.identity, room.root);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(size.x, floorTile.transform.localScale.y, size.z);
        room.floor = floor.GetComponentInChildren<Renderer>();
        room.floor.sharedMaterial = roomFloorMaterial;

        // Tiny texture: pixel (x, y) = cell (x, y). Starts all black = no wear.
        room.trailTex = new Texture2D(room.width, room.height, TextureFormat.RGBA32, false)
        {
            filterMode = smoothPath ? FilterMode.Bilinear : FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        room.trailTex.SetPixels32(new Color32[room.width * room.height]);
        room.trailTex.Apply();
        room.wear = new float[room.width, room.height];

        // Give THIS renderer its own texture and position, without copying the material
        var block = new MaterialPropertyBlock();
        block.SetTexture("_TrailTex", room.trailTex);
        block.SetVector("_RoomRect", new Vector4(corner.x, corner.z, size.x, size.z));
        room.floor.SetPropertyBlock(block);
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

        Vector2Int origin = room.slot * slotSize;                  // corner of A's SLOT (no offset!)
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

                if (across == gapStart - 1 || across == gapEnd)
                    Instantiate(wallTile, position, Quaternion.identity, corridorObj.transform);
            }
        }
    }

    // -------- Room contents --------

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

    // -------- Connectivity (flood fill) --------

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

    // -------- Floor path (public, used by FloorTrail) --------

    /// World position -> global cell (the same grid everything is built on).
    public Vector2Int WorldToGlobalCell(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x / cellSize),
                              Mathf.FloorToInt(worldPos.z / cellSize));
    }

    /// Wears down the floor cell under worldPos by 'amount' (0..1). Does nothing outside rooms.
    public void AddWear(Vector3 worldPos, float amount)
    {
        if (!TryGetRoomCell(worldPos, out RoomData room, out Vector2Int cell)) return;
        if (room.trailTex == null) return;

        float old = room.wear[cell.x, cell.y];
        float now = Mathf.Min(1f, old + amount);
        if (now == old) return;                   // already fully worn

        room.wear[cell.x, cell.y] = now;
        room.trailTex.SetPixel(cell.x, cell.y, new Color(now, 0, 0, 1));
        dirtyRooms.Add(room);                     // uploaded in LateUpdate
    }

    bool TryGetRoomCell(Vector3 worldPos, out RoomData room, out Vector2Int cell)
    {
        Vector2Int global = WorldToGlobalCell(worldPos);
        Vector2Int slot = WorldToSlot(worldPos);
        cell = default;
        if (!rooms.TryGetValue(slot, out room)) return false;

        cell = global - RoomOrigin(room);
        return cell.x >= 0 && cell.x < room.width && cell.y >= 0 && cell.y < room.height;
    }
    Vector2Int WorldToSlot(Vector3 worldPos)
    {
        Vector2Int g = WorldToGlobalCell(worldPos);
        return new Vector2Int(Mathf.FloorToInt((float)g.x / slotSize),
                              Mathf.FloorToInt((float)g.y / slotSize));
    }

    void FreeFloorTextures()
    {
        foreach (var room in rooms.Values)
            if (room.trailTex != null) Destroy(room.trailTex);
        dirtyRooms.Clear();
    }
    void TrackPlayer()
    {
        if (player == null || !IsReady) return;
        Vector2Int slot = WorldToSlot(player.position);
        if (slot == lastPlayerSlot) return;
        lastPlayerSlot = slot;
        if (rooms.TryGetValue(slot, out RoomData room) && !room.explored)
            Explore(room);
    }

    void Explore(RoomData room)
    {
        room.explored = true;
        Debug.Log($"Explored {room.slot}");

        if (room.fog != null) StartCoroutine(FadeFog(room.fog));
    }

    // --------- Helpers ---------

    int CountFilledNeighbours(Vector2Int slot)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (rooms.ContainsKey(slot + Dirs[i])) count++;
        }
        return count;
    }

    void BlockRect(HashSet<Vector2Int> set, int x0, int y0, int w, int h)
    {
        for (int x = x0; x < x0 + w; x++)
            for (int y = y0; y < y0 + h; y++)
                set.Add(new Vector2Int(x, y));
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
    bool HasExploredNeighbour(RoomData room)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2Int neighbourSlot = room.slot + Dirs[i];
            if (room.doors[i] && rooms.TryGetValue(neighbourSlot, out RoomData neighbour) && neighbour.explored)
                return true;
        }
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

            Gizmos.color = Color.lightCyan;
            Gizmos.DrawWireCube(center, size);

            // Draw line from center to neighbors center if there is an open door
            Gizmos.color = Color.green;
            for (int i = 0; i < 4; i++)
            {
                if (!room.doors[i]) continue;
                Vector2Int neighborSlot = room.slot + Dirs[i];
                if (rooms.ContainsKey(neighborSlot))
                    Gizmos.DrawLine(center, SlotCenter(neighborSlot));
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
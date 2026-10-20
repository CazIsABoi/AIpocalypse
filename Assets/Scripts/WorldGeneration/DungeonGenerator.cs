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
    [SerializeField] GameObject mapQuadPrefab;
    [SerializeField] Material mapRoomMaterial;
    [SerializeField] Material mapStartMaterial;
    [SerializeField] Material mapCorridorMaterial;
    [SerializeField] Material mapObstacleMaterial;
    [SerializeField] Material mapCutoutMaterial;

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

    [Header("Room Roles")]
    [SerializeField] private RoomContent coreContent;      
    [SerializeField] private RoomContent treasureContent;   
    [SerializeField] private int treasureRooms = 2;

    [Header("Fill")]
    [SerializeField] private bool fillBetweenRooms = true; // solid wall mass in all space outside rooms and corridors
    [SerializeField] private int fillMargin = 1;           // extra ring of solid slots around the whole map

    [Header("Floor Path")]
    [SerializeField] private bool smoothPath = true; 

    [Header("Fog")]
    [SerializeField] float fogHeight = 3f;
    [SerializeField] float fogFadeTime = 0.5f;
    [SerializeField] float mapHeight = 2.5f;   // must be lower than fogHeight, so the fog covers it
    [SerializeField] float obstacleMapOffset = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool showBlocked = true;
    [SerializeField] private bool showSolid = true;
    [SerializeField] private bool showFill = true;
    [SerializeField] private Vector2Int debugRoomSlot;          // which room to animate
    [SerializeField] private float fillStepDelay = 0.02f;      // seconds per step

    Dictionary<Vector2Int, RoomData> rooms = new Dictionary<Vector2Int, RoomData>();
    List<WearSurface> corridorSurfaces = new List<WearSurface>();
    HashSet<WearSurface> dirtySurfaces = new HashSet<WearSurface>();   // floors whose wear texture changed this frame
    public event System.Action Generated;
    public bool IsReady { get; private set; }
    public Vector3 StartPosition => SlotCenter(Vector2Int.zero) + Vector3.up * 1f;

    static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    private void Start()
    {
        StartCoroutine(GenerateRoutine());
    }
    public void Regenerate()
    {
        if (!IsReady) return;   // already generating
        StartCoroutine(GenerateRoutine());
    }

    void Update()
    {
        TrackPlayer();
#if UNITY_EDITOR || DEBUG
        if (devResetKey.action.triggered) { Regenerate(); }
    #endif
    }

    void LateUpdate()
    {
        // Upload changed floor textures once per frame, however many cells changed
        if (dirtySurfaces.Count == 0) return;
        foreach (WearSurface surface in dirtySurfaces)
            if (surface.Texture != null) surface.Texture.Apply();
        dirtySurfaces.Clear();
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
        ComputeDistances();
        AssignRoles();

        foreach (var room in rooms.Values) BuildRoom(room);
        Debug.Log($"Build rooms: {sw.ElapsedMilliseconds} ms"); sw.Restart();

        BuildCorridors();
        Debug.Log($"Build corridors: {sw.ElapsedMilliseconds} ms"); sw.Restart();

        if (fillBetweenRooms)
        {
            BuildFill();
            Debug.Log($"Build fill: {sw.ElapsedMilliseconds} ms"); sw.Restart();
        }

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
        Vector2Int offset = RoomOffset(room);
        Material mat = room.slot == Vector2Int.zero ? mapStartMaterial : mapRoomMaterial;
        room.mapQuad = CreateMapQuad(room.slot, offset.x, offset.y, room.width, room.height, mat, room.root);

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
        room.fog.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // fog shouldn't darken anything below it
        room.fog.receiveShadows = false;                                           // or get shadows drawn onto it
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
        // Room types can tint their floor; null = keep the material's normal clean color
        Color? tint = room.content != null && room.content.tintFloor ? room.content.floorColor : (Color?)null;
        room.surface = CreateSurface(RoomOrigin(room), room.width, room.height, room.root, "Floor", tint);
    }

    // One floor object covering a rectangle of cells, with its own wear texture.
    // Used for both rooms and corridors.
    WearSurface CreateSurface(Vector2Int origin, int width, int height, Transform parent, string name, Color? cleanColor = null)
    {
        Vector3 corner = new Vector3(origin.x * cellSize, 0, origin.y * cellSize);    // bottom-left corner, world
        Vector3 size = new Vector3(width * cellSize, 0, height * cellSize);

        // One floor object, scaled to cover the whole rectangle
        GameObject floor = Instantiate(floorTile, corner + size / 2f, Quaternion.identity, parent);
        floor.name = name;
        floor.transform.localScale = new Vector3(size.x, floorTile.transform.localScale.y, size.z);
        Renderer floorRenderer = floor.GetComponentInChildren<Renderer>();
        floorRenderer.sharedMaterial = roomFloorMaterial;

        // Tiny texture: pixel (x, y) = cell (x, y). Starts all black = no wear.
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = smoothPath ? FilterMode.Bilinear : FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(new Color32[width * height]);
        texture.Apply();

        // Give THIS renderer its own texture and position, without copying the material
        var block = new MaterialPropertyBlock();
        block.SetTexture("_TrailTex", texture);
        block.SetVector("_RoomRect", new Vector4(corner.x, corner.z, size.x, size.z));
        if (cleanColor.HasValue) block.SetColor("_CleanColor", cleanColor.Value);   // per-room floor color
        floorRenderer.SetPropertyBlock(block);

        return new WearSurface
        {
            Origin = origin,
            Width = width,
            Height = height,
            Texture = texture,
            Wear = new float[width, height]
        };
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

        // The walkable part of the corridor as a rectangle (slot coordinates of room A)
        Vector2Int localCorner = horizontal ? new Vector2Int(start, gapStart) : new Vector2Int(gapStart, start);
        int w = horizontal ? end - start : doorWidth;
        int h = horizontal ? doorWidth : end - start;

        CreateMapQuad(room.slot, localCorner.x, localCorner.y, w, h, mapCorridorMaterial, corridorObj.transform);

        // One floor with its own wear texture, like a room
        corridorSurfaces.Add(CreateSurface(origin + localCorner, w, h, corridorObj.transform, "CorridorFloor"));

        // With fill on, the fill boxes already form the corridor's side walls
        if (fillBetweenRooms) return;

        for (int along = start; along < end; along++)
        {
            foreach (int across in new[] { gapStart - 1, gapEnd })
            {
                int x = horizontal ? along : across;
                int y = horizontal ? across : along;
                Instantiate(wallTile, CellToWorld(origin, x, y), Quaternion.identity, corridorObj.transform);
            }
        }
    }

    // -------- Fill (solid mass between rooms) --------

    // Fills every cell that isn't room or corridor with a few big scaled wall boxes.
    // Each slot is split into rectangles around its room:
    //
    //   +----+--------+----+
    //   | NW | north  | NE |    corners: always solid
    //   +----+--------+----+    sides:   solid, or two pieces with a gap
    //   |west|  ROOM  |east|             where a corridor leaves the room
    //   +----+--------+----+
    //   | SW | south  | SE |    slots without a room: one box for the whole slot
    //   +----+--------+----+
    void BuildFill()
    {
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(transform);

        // Big flat box tops would otherwise get baked as (unreachable) walkable navmesh
        var modifier = fillObj.AddComponent<NavMeshModifier>();
        modifier.overrideArea = true;
        modifier.area = NavMesh.GetAreaFromName("Not Walkable");
        modifier.applyToChildren = true;

        // Bounds of the map in slots, plus a margin
        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
        foreach (RoomData room in rooms.Values)
        {
            min = Vector2Int.Min(min, room.slot);
            max = Vector2Int.Max(max, room.slot);
        }
        min -= Vector2Int.one * fillMargin;
        max += Vector2Int.one * fillMargin;

        for (int sx = min.x; sx <= max.x; sx++)
        {
            for (int sy = min.y; sy <= max.y; sy++)
            {
                Vector2Int slot = new Vector2Int(sx, sy);
                if (rooms.TryGetValue(slot, out RoomData room))
                    FillAroundRoom(room, fillObj.transform);
                else
                    FillBox(slot, 0, 0, slotSize, slotSize, fillObj.transform);   // empty slot: solid
            }
        }
    }

    void FillAroundRoom(RoomData room, Transform parent)
    {
        Vector2Int slot = room.slot;
        Vector2Int offset = RoomOffset(room);
        int x0 = offset.x, x1 = offset.x + room.width;    // room covers x0..x1-1 (slot coordinates)
        int y0 = offset.y, y1 = offset.y + room.height;   // room covers y0..y1-1
        int s = slotSize;

        // Corners
        FillBox(slot, 0,  0,  x0,     y0,     parent);   // SW
        FillBox(slot, x1, 0,  s - x1, y0,     parent);   // SE
        FillBox(slot, 0,  y1, x0,     s - y1, parent);   // NW
        FillBox(slot, x1, y1, s - x1, s - y1, parent);   // NE

        // Sides (split around the corridor if that side has a door)
        FillBand(slot, 0,  y0, x0,      y1 - y0, room.doors[3], splitAlongY: true,  parent);   // west
        FillBand(slot, x1, y0, s - x1,  y1 - y0, room.doors[1], splitAlongY: true,  parent);   // east
        FillBand(slot, x0, 0,  x1 - x0, y0,      room.doors[2], splitAlongY: false, parent);   // south
        FillBand(slot, x0, y1, x1 - x0, s - y1,  room.doors[0], splitAlongY: false, parent);   // north
    }

    // A side band: one box, or two boxes leaving the corridor's door rows/columns open
    void FillBand(Vector2Int slot, int x, int y, int w, int h, bool door, bool splitAlongY, Transform parent)
    {
        if (!door)
        {
            FillBox(slot, x, y, w, h, parent);
            return;
        }

        // Rooms are centered in their slots, so every corridor runs along the slot's middle
        int gapStart = DoorStart(slotSize);
        int gapEnd = gapStart + doorWidth;

        if (splitAlongY)
        {
            FillBox(slot, x, y,      w, gapStart - y,     parent);   // below the corridor
            FillBox(slot, x, gapEnd, w, y + h - gapEnd,   parent);   // above the corridor
        }
        else
        {
            FillBox(slot, x,      y, gapStart - x,   h, parent);     // left of the corridor
            FillBox(slot, gapEnd, y, x + w - gapEnd, h, parent);     // right of the corridor
        }
    }

    // One wall box covering cells x..x+w-1, y..y+h-1 of a slot
    void FillBox(Vector2Int slot, int x, int y, int w, int h, Transform parent)
    {
        if (w <= 0 || h <= 0) return;   // e.g. a room that touches its slot edge

        Vector2Int origin = slot * slotSize;
        Vector3 center = new Vector3((origin.x + x + w / 2f) * cellSize, 0,
                                     (origin.y + y + h / 2f) * cellSize);

        GameObject box = Instantiate(wallTile, center, Quaternion.identity, parent);
        box.GetComponentInChildren<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Vector3 scale = wallTile.transform.localScale;
        box.transform.localScale = new Vector3(scale.x * w, scale.y, scale.z * h);
    }
    void ComputeDistances()
    {
        foreach (RoomData room in rooms.Values) room.distance = -1;   // -1 = not reached yet

        RoomData start = rooms[Vector2Int.zero];
        start.distance = 0;
        var queue = new Queue<RoomData>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            RoomData current = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                if (!current.doors[i]) continue;
                RoomData neighbor = rooms[current.slot + Dirs[i]];
                if (neighbor.distance != -1) continue;
                neighbor.distance = current.distance + 1;
                queue.Enqueue(neighbor);
            }
        }
    }

    // -------- Room contents --------
    void AssignRoles()
    {
        // 1. everything starts as Normal; the room at (0,0) becomes Start
        foreach (RoomData room in rooms.Values) room.role = RoomRole.Normal;
        rooms[Vector2Int.zero].role = RoomRole.Start;


        // 2. Core = the room with the highest distance
        List<RoomData> farthest = new List<RoomData>();
        foreach (RoomData room in rooms.Values)
        {
            if (room.distance == -1) continue;  // not reached
            if (farthest.Count == 0 || room.distance > farthest[0].distance)
            {
                farthest.Clear();
                farthest.Add(room);
            }
            else if (room.distance == farthest[0].distance)
            {
                farthest.Add(room);
            }
        }
        if (farthest.Count > 0) { farthest[Random.Range(0, farthest.Count)].role = RoomRole.Core; }


        // 3. Treasure = the farthest dead ends
        List<RoomData> candidates = new List<RoomData>();
        foreach (RoomData room in rooms.Values)
        {
            if (room.role != RoomRole.Normal) continue;
            if (DoorCount(room) == 1) candidates.Add(room);
        }

        // Not enough dead ends? Add the other Normal rooms too (they'll sort in by distance)
        if (candidates.Count < treasureRooms)
        {
            foreach (RoomData room in rooms.Values)
                if (room.role == RoomRole.Normal && !candidates.Contains(room)) candidates.Add(room);
        }

        candidates.Sort((a, b) => b.distance.CompareTo(a.distance));   // farthest first

        for (int i = 0; i < treasureRooms && i < candidates.Count; i++)
            candidates[i].role = RoomRole.Treasure;

        // 4. Decide every room's content now, before anything is built,
        //    so the floor (built in BuildRoom) already knows which room type it is
        foreach (RoomData room in rooms.Values)
        {
            if (room.role == RoomRole.Start) { room.content = null; room.shape = RoomShape.Rectangle; }
            else if (room.role == RoomRole.Core) { room.content = coreContent; room.shape = RoomShape.Rectangle; }
            else if (room.role == RoomRole.Treasure) { room.content = treasureContent; room.shape = RoomShape.Rectangle; }
            else { room.content = PickContent(room); room.shape = PickShape(room); }
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

        ApplyShape(room, blocked, solid); // shape can block more cells, e.g. L-shaped rooms

        RoomContent content = room.content;   // decided in AssignRoles

        // Special rooms (e.g. treasure) get their own color on the minimap
        if (content != null && content.mapRoomMaterial != null)
            room.mapQuad.sharedMaterial = content.mapRoomMaterial;

        if (content != null && content.props.Length > 0)
        {
            switch (content.pattern)
            {
                case PlacementPattern.Scatter: PlaceScatter(room, content, blocked, solid); break;
                case PlacementPattern.PillarGrid: PlacePillarGrid(room, content, blocked, solid); break;
                case PlacementPattern.Center: PlaceCenter(room, content, blocked, solid); break;
                case PlacementPattern.Rows: PlaceRows(room, content, blocked, solid); break;
                case PlacementPattern.Perimeter: PlacePerimeter(room, content, blocked, solid); break;
                case PlacementPattern.Ring: PlaceRing(room, content, blocked, solid); break;
                case PlacementPattern.Clusters: PlaceClusters(room, content, blocked, solid); break;
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

    RoomShape PickShape(RoomData room)
    {
        if (room.content != null && room.content.pattern == PlacementPattern.Center)
            return RoomShape.Rectangle;

        RoomShape[] shapes = { RoomShape.Rectangle, RoomShape.L, RoomShape.Half, RoomShape.Cross, RoomShape.Diagonal, RoomShape.Donut };
        int[] weights = { 10, 3, 3, 2, 2, 2 };

        int total = 0;
        foreach (int w in weights) total += w;

        int roll = Random.Range(0, total);
        for (int i = 0; i < shapes.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0) return shapes[i];
        }
        return RoomShape.Rectangle;
    }

    void PlaceProp(RoomData room, RoomContent content, Vector2Int cell)
    {
        GameObject prefab = content.props[Random.Range(0, content.props.Length)];
        Vector3 pos = CellToWorld(RoomOrigin(room), cell.x, cell.y);
        float angle = 0f;
        switch (content.rotation)
        {
            case PropRotation.Random90: angle = 90f * Random.Range(0, 4); break;
            case PropRotation.Random180: angle = 180f * Random.Range(0, 2); break;   // facing one way or the opposite
            case PropRotation.RandomAny: angle = Random.Range(0f, 360f); break;      // any angle, good for debris
                                                                                     // None: stays 0
        }
        Quaternion rot = Quaternion.Euler(0, angle, 0);
        GameObject prop = Instantiate(prefab, pos, rot, room.root);
        if (content.randomScale)
            prop.transform.localScale *= Random.Range(0.85f, 1.15f);
        Vector2Int offset = RoomOffset(room);
        Material mapMat = content.mapMaterial != null ? content.mapMaterial : mapObstacleMaterial;
        Renderer mapIcon = CreateMapQuad(room.slot, offset.x + cell.x, offset.y + cell.y, 1, 1, mapMat, room.root, obstacleMapOffset);

        // Chests recolor their map icon and room when looted, so tell them which quads are theirs
        if (prop.TryGetComponent(out Chest chest))
            chest.SetMapMarkers(mapIcon, room.mapQuad);
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

            if (!TryClaimCell(room, content, blocked, solid, cell)) continue;  
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
                if (!TryClaimCell(room, content, blocked, solid, cell)) continue;   
                PlaceProp(room, content, cell);
            }
        }
    }
    void PlaceCenter(RoomData room, RoomContent content, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        Vector2Int cell = new Vector2Int(room.width / 2, room.height / 2);

        if (!TryClaimCell(room, content, blocked, solid, cell)) return;  
        PlaceProp(room, content, cell);
    }
    void PlaceRows(RoomData room, RoomContent content, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        bool horizontal = room.width >= room.height;            // rows run along the room's longer side
        int lengthAlong = horizontal ? room.width : room.height;
        int lengthAcross = horizontal ? room.height : room.width;

        for (int across = 2; across < lengthAcross - 2; across += content.spacing)   // one row every 'spacing'
        {
            for (int along = 2; along < lengthAlong - 2; along++)
            {
                Vector2Int cell = horizontal ? new Vector2Int(along, across) : new Vector2Int(across, along);
                if (!TryClaimCell(room, content, blocked, solid, cell)) continue;
                PlaceProp(room, content, cell);
            }
        }
    }
    void PlacePerimeter(RoomData room, RoomContent content, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        for (int x = 1; x < room.width - 1; x++)
        {
            for (int y = 1; y < room.height - 1; y++)
            {
                bool nextToWall = x == 1 || y == 1 || x == room.width - 2 || y == room.height - 2;
                if (!nextToWall) continue;
                if (Random.value > content.density) continue;       // here: chance per cell (1 = solid line)

                Vector2Int cell = new Vector2Int(x, y);
                if (!TryClaimCell(room, content, blocked, solid, cell)) continue;
                PlaceProp(room, content, cell);
            }
        }
    }
    void PlaceRing(RoomData room, RoomContent content, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        Vector2 center = new Vector2(room.width / 2, room.height / 2);
        float radius = content.spacing;                              // here: the ring's radius in cells

        for (int x = 1; x < room.width - 1; x++)
        {
            for (int y = 1; y < room.height - 1; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (Mathf.Abs(distance - radius) > 0.5f) continue;   // only cells ON the circle

                Vector2Int cell = new Vector2Int(x, y);
                if (!TryClaimCell(room, content, blocked, solid, cell)) continue;
                PlaceProp(room, content, cell);
            }
        }
    }
    void PlaceClusters(RoomData room, RoomContent content, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        int clusterCount = 3;                // or a field on RoomContent
        int radius = content.spacing;        // here: cluster size in cells

        for (int c = 0; c < clusterCount; c++)
        {
            Vector2Int center = new Vector2Int(Random.Range(2, room.width - 2), Random.Range(2, room.height - 2));

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    if (x < 1 || x >= room.width - 1 || y < 1 || y >= room.height - 1) continue;
                    if (Vector2.Distance(new Vector2(x, y), center) > radius) continue;   // outside the cluster radius
                    if (Random.value > 0.7f) continue;   // skip randomly, denser near the centre

                    Vector2Int cell = new Vector2Int(x, y);
                    if (!TryClaimCell(room, content, blocked, solid, cell)) continue;

                    PlaceProp(room, content, cell);
                }
            }
        }
    }
    void ApplyShape(RoomData room, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        if (room.shape == RoomShape.Rectangle) return;

        // ---- Donut: one block in the middle ----
        if (room.shape == RoomShape.Donut)
        {
            int margin = laneDepth + 2;                    // keep door lanes + a walkway around the block free
            int w = room.width - 2 * margin;
            int h = room.height - 2 * margin;
            if (w >= 1 && h >= 1) CutBlock(room, margin, margin, w, h, blocked, solid);
            return;
        }

        // ---- Corner shapes: how big may a cut be? (step 2) ----
        int maxCutX = DoorStart(room.width) - 1;
        int maxCutY = DoorStart(room.height) - 1;
        if (maxCutX < 3 || maxCutY < 3) return;            // too small, stays a rectangle

        int cutX = Random.Range(3, maxCutX + 1);
        int cutY = Random.Range(3, maxCutY + 1);

        List<int> corners = new List<int>();
        int first = Random.Range(0, 4);
        switch (room.shape)
        {
            case RoomShape.L: corners.Add(first); break;
            case RoomShape.Half: corners.Add(first); corners.Add((first + 1) % 4); break;   // neighbours = one side
            case RoomShape.Diagonal: corners.Add(first); corners.Add((first + 2) % 4); break;   // opposite corners
            case RoomShape.Cross: corners.AddRange(new[] { 0, 1, 2, 3 }); break;
        }

        // ---- Cut each chosen corner ----
        foreach (int corner in corners)
        {
            bool left = corner == 0 || corner == 3;      // SW and NW
            bool bottom = corner == 0 || corner == 1;      // SW and SE
            int x = left ? 0 : room.width - cutX;
            int y = bottom ? 0 : room.height - cutY;
            CutBlock(room, x, y, cutX, cutY, blocked, solid);
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
    void CutBlock(RoomData room, int x, int y, int w, int h, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid)
    {
        BlockRect(blocked, x, y, w, h);       // no props here
        BlockRect(solid, x, y, w, h);         // nobody walks here

        Vector2Int origin = RoomOrigin(room);
        Vector3 center = new Vector3((origin.x + x + w / 2f) * cellSize, 0,
                                     (origin.y + y + h / 2f) * cellSize);

        GameObject box = Instantiate(wallTile, center, Quaternion.identity, room.root);
        Vector3 scale = wallTile.transform.localScale;
        box.transform.localScale = new Vector3(scale.x * w, scale.y, scale.z * h);
        box.GetComponentInChildren<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var modifier = box.AddComponent<NavMeshModifier>();
        modifier.overrideArea = true;
        modifier.area = NavMesh.GetAreaFromName("Not Walkable");
        modifier.applyToChildren = true;

        Vector2Int offset = RoomOffset(room);
        CreateMapQuad(room.slot, offset.x + x, offset.y + y, w, h, mapCutoutMaterial, room.root, obstacleMapOffset);
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

    /// Wears down the floor cell under worldPos by 'amount' (0..1). Works in rooms and corridors.
    public void AddWear(Vector3 worldPos, float amount)
    {
        Vector2Int cell = WorldToGlobalCell(worldPos);
        WearSurface surface = FindSurface(worldPos, cell);
        if (surface == null) return;

        if (surface.AddWear(cell, amount)) dirtySurfaces.Add(surface);   // uploaded in LateUpdate
    }

    WearSurface FindSurface(Vector3 worldPos, Vector2Int cell)
    {
        // Rooms first: a quick dictionary lookup by slot
        if (rooms.TryGetValue(WorldToSlot(worldPos), out RoomData room)
            && room.surface != null && room.surface.Contains(cell))
            return room.surface;

        // Otherwise check the corridors (only runs when stepping onto a new cell, so a loop is fine)
        foreach (WearSurface corridor in corridorSurfaces)
            if (corridor.Contains(cell)) return corridor;

        return null;
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
            if (room.surface?.Texture != null) Destroy(room.surface.Texture);
        foreach (WearSurface corridor in corridorSurfaces)
            if (corridor.Texture != null) Destroy(corridor.Texture);

        corridorSurfaces.Clear();
        dirtySurfaces.Clear();
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

    int DoorCount(RoomData room)
    {
        int count = 0;
        foreach (bool hasDoor in room.doors) if (hasDoor) count++;
        return count;
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
    // Claims a cell for a prop. Returns false if the cell can't be used.
    bool TryClaimCell(RoomData room, RoomContent content, HashSet<Vector2Int> blocked, HashSet<Vector2Int> solid, Vector2Int cell)
    {
        if (blocked.Contains(cell) || solid.Contains(cell)) return false;

        if (content.walkable)
        {
            blocked.Add(cell);    // used, so nothing else goes here, but still walkable
            return true;
        }

        return TryAddSolid(room, solid, cell);
    }
    // Returns the quad's renderer, so its color can be changed later (treasure rooms, looted chests)
    Renderer CreateMapQuad(Vector2Int slot, float x, float y, float w, float h, Material mat, Transform parent, float heightOffset = 0f)
    {
        Vector3 center = new Vector3((slot.x * slotSize + x + w / 2f) * cellSize, mapHeight + heightOffset, (slot.y * slotSize + y + h / 2f) * cellSize);
        GameObject quad = Instantiate(mapQuadPrefab, center, Quaternion.Euler(90, 0, 0), parent);
        quad.transform.localScale = new Vector3(w * cellSize, h * cellSize, 1);
        Renderer quadRenderer = quad.GetComponent<Renderer>();
        quadRenderer.sharedMaterial = mat;
        return quadRenderer;
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
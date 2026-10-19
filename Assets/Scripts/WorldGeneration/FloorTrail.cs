using UnityEngine;

public class FloorTrail : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField, Range(0f, 1f)] private float wearPerStep = 0.1f;   // how much one step wears the floor

    private Vector2Int lastCell = new Vector2Int(int.MinValue, int.MinValue);

    void Awake()
    {
        // Lets spawned objects (e.g. enemies) find the generator without an Inspector reference
        if (dungeon == null) dungeon = FindAnyObjectByType<DungeonGenerator>();
    }

    void Update()
    {
        Vector2Int cell = dungeon.WorldToGlobalCell(transform.position);
        if (cell == lastCell) return;      // still on the same cell, so no new step
        lastCell = cell;

        dungeon.AddWear(transform.position, wearPerStep);
    }
}
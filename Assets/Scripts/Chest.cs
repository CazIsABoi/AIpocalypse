using UnityEngine;

public class Chest : MonoBehaviour, IMapMarked
{
    [SerializeField] private RewardPool chestPool;
    [SerializeField] private float openRange = 1.5f;
    [SerializeField] private Material openedMaterial;

    [Header("Minimap (after looting)")]
    [SerializeField] private Material lootedMapIconMaterial;   // the chest's dot on the minimap
    [SerializeField] private Material lootedMapRoomMaterial;   // the whole room on the minimap

    private LevelUpUI levelUpUI;
    private Transform player;
    private bool opened;

    private Renderer mapIcon;
    private Renderer mapRoom;

    void Awake()
    {
        levelUpUI = FindAnyObjectByType<LevelUpUI>();
        player = FindAnyObjectByType<PlayerStats>().transform;
    }

    // Called by the DungeonGenerator right after placing this chest
    public void SetMapMarkers(Renderer icon, Renderer room)
    {
        mapIcon = icon;
        mapRoom = room;
    }

    void Update()
    {
        if (opened) return;

        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0; // Ignore vertical distance
        float distance = offset.magnitude;
        if (distance < openRange && levelUpUI.ShowChest(chestPool))
        {
            opened = true;
            RunTelemetry.Instance?.ChestOpened();
            AudioManager.Instance?.Play(Sfx.ChestOpen);
            GetComponentInChildren<Renderer>().sharedMaterial = openedMaterial;

            if (mapIcon != null && lootedMapIconMaterial != null) mapIcon.sharedMaterial = lootedMapIconMaterial;
            if (mapRoom != null && lootedMapRoomMaterial != null) mapRoom.sharedMaterial = lootedMapRoomMaterial;
        }
    }
}

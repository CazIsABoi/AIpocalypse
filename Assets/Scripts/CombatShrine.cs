using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CombatShrine : MonoBehaviour, IMapMarked
{
    enum State { Idle, Active, Done }
    State state = State.Idle;

    private LevelUpUI levelUpUI;
    private Transform player;
    private EntityHandler spawner;
    private GameController game;

    [SerializeField] float activateRadius = 2f, activateTime = 1f, spawnRadius = 4f;
    [SerializeField] int baseWave = 3;                 // + current floor
    [SerializeField] RewardPool rewardPool;
    [SerializeField] Material usedMaterial;            // palette without emission: runes/crystal go dark
    [SerializeField] Material usedMapIconMaterial;     // minimap dot after completion (e.g. the chest's looted one)
    private Renderer mapIcon;

    List<EnemyController> wave = new List<EnemyController>();
    float charge;

    private void Awake()
    {
        levelUpUI = FindAnyObjectByType<LevelUpUI>();
        player = FindAnyObjectByType<PlayerHealth>().transform;
        spawner = FindAnyObjectByType<EntityHandler>();
        game = FindAnyObjectByType<GameController>();
    }

    void Update()
    {
        switch (state)
        {
            case State.Idle:
                Vector3 offset = player.position - transform.position;
                offset.y = 0f;                                   // flat distance, height doesn't matter
                if (offset.magnitude < activateRadius)
                {
                    charge += Time.deltaTime;
                    if (charge >= activateTime) Activate();
                }
                else
                {
                    charge -= Time.deltaTime;
                    if (charge < 0) charge = 0;
                }
                break;
            case State.Active:
                wave.RemoveAll(e => e == null);        
                if (wave.Count == 0) StartCoroutine(Complete());
                break;
        }
    }

    void Activate()
    {
        state = State.Active;
        for (int i = 0; i < baseWave + game.Floor; i++)
        {
            // on a ring 2..spawnRadius away (never inside the shrine), snapped to walkable floor
            Vector2 dir = Random.insideUnitCircle.normalized;
            Vector3 point = transform.position + new Vector3(dir.x, 0f, dir.y) * Random.Range(2f, spawnRadius);
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                wave.Add(spawner.SpawnEnemy(spawner.RollEnemyWeightAndIndex(), hit.position));
        }
        AudioManager.Instance?.Play(Sfx.EventStart);
        CameraShake.Instance?.Add(0.5f);
    }

    IEnumerator Complete()
    {
        state = State.Done;
        // the renderers are on the model/crystal children, and each can have several material slots
        if (usedMaterial != null)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = usedMaterial;
                r.sharedMaterials = mats;
            }
        }
        if (mapIcon != null && usedMapIconMaterial != null) mapIcon.sharedMaterial = usedMapIconMaterial;
        AudioManager.Instance?.Play(Sfx.BountyKilled);
        yield return new WaitUntil(() => levelUpUI.ShowChest(rewardPool));   
    }
    public void SetMapMarkers(Renderer icon, Renderer room) => mapIcon = icon;   // called by the DungeonGenerator
}
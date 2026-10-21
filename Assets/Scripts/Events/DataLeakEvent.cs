using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Events/Data Leak")]
public class DataLeakEvent : GameEvent
{
    public GameObject orbPrefab;    
    public int count = 15;
    public float radius = 6f;
    public int xpPerOrb = 5;
    private float timer;

    private List<GameObject> spawned = new List<GameObject>();   

    public override void Begin(EventContext ctx)
    {
        timer = 0f;
    }

    public override void Tick(EventContext ctx)
    {
        timer += Time.deltaTime;         // count up a little each frame
        float interval = Duration / count;
        while (timer >= interval)        
        {
            timer -= interval;
            if (!TryGetOrbSpot(ctx.Player.position, out Vector3 spawnPos))
            {
                // If we can't find a good spot, just skip this orb
                continue;
            }
            GameObject orb = Instantiate(orbPrefab, spawnPos + Vector3.up * 0.5f, Quaternion.identity);
            orb.GetComponent<XpOrb>().Amount = xpPerOrb;
            spawned.Add(orb);
        }
    }

    public override void End(EventContext ctx)
    {
        foreach (var orb in spawned)
        {
            if (orb != null) Destroy(orb);
        }
        spawned.Clear();
    }
    bool TryGetOrbSpot(Vector3 center, out Vector3 spot)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            if (!NavMesh.SamplePosition(center, out NavMeshHit ground, 2f, NavMesh.AllAreas))
            { spot = default; return false; }
            center = ground.position;            
            if (NavMesh.SamplePosition(center + new Vector3(offset.x, 0, offset.y), out NavMeshHit hit, 1.0f, NavMesh.AllAreas) == false)
                continue;  // not on the navmesh
            if (NavMesh.Raycast(center, hit.position, out NavMeshHit ohit, NavMesh.AllAreas) != true)
            {
                spot = hit.position;
                return true;
            }
            else continue;
        }
        spot = default; return false;      // no good spot this time: just skip this orb
    }
}
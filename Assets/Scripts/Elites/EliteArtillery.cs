using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EliteArtillery : EliteBehaviour
{
    protected override float Interval => 4f;
    float range = 25f;
    float blastRadius = 2.5f;
    float fuse = 1f;

    protected override void Tick()
    {
        if (Vector3.Distance(player.position, transform.position) < range)
            StartCoroutine(Strike(player.position));
    }

    IEnumerator Strike(Vector3 spot)
    {
        Vector3 ground = spot;
        if (NavMesh.SamplePosition(spot, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            ground = hit.position;

        GameObject marker = Instantiate(enemy.handler.ArtilleryMarker, ground, Quaternion.identity);
        Destroy(marker, fuse);
        yield return new WaitForSeconds(fuse);
        Instantiate(enemy.handler.ArtilleryBlast, ground, Quaternion.identity);
        if (Vector3.Distance(player.position, spot) < blastRadius)
            playerHealth.TakeDamage(enemy.Damage, "Artillery Elite");
    }
}
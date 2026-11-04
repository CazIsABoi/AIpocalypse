using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EliteBlink : EliteBehaviour
{
    protected override float Interval => 4f;
    float tooFar = 12f;
    float landDistance = 4f;
    float warning = 0.5f;

    protected override void Tick()
    {
        if (Vector3.Distance(player.position, transform.position) > tooFar)
            StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        Vector2 circle = Random.insideUnitCircle.normalized * landDistance;
        Vector3 blinkPosition = player.position + new Vector3(circle.x, 0f, circle.y);
        NavMesh.SamplePosition(blinkPosition, out NavMeshHit hit, 1f, NavMesh.AllAreas);
        if (!hit.hit)
            yield break;   // stop if no walkable spot found
        Instantiate(enemy.handler.BlinkEffect, hit.position, Quaternion.identity);
        yield return new WaitForSeconds(warning);
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent.isActiveAndEnabled)
            agent.Warp(hit.position);
    }
}
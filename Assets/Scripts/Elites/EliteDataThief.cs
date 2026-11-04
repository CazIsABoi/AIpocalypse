using UnityEngine;

public class EliteDataThief : EliteBehaviour
{
    protected override float Interval => 0.5f;
    float grabRadius = 6f;
    float bonus = 1.5f;

    protected override void Tick()
    {
        foreach (XpOrb orb in FindObjectsByType<XpOrb>())
        {
            if (Vector3.Distance(orb.transform.position, transform.position) < grabRadius)
            {
                enemy.Xp += Mathf.RoundToInt(orb.Amount * bonus);
                Destroy(orb.gameObject);
            }
        }
    }
}
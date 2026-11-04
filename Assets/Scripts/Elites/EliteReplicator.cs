using UnityEngine;

public class EliteReplicator : EliteBehaviour
{
    public int EnemyIndex;
    protected override float Interval => 5f;
    int maxCopies = 4;
    float copyHealth = 0.4f;
    float copyXp = 0.25f;
    int made;

    protected override void Tick()
    {
        if (made >= maxCopies) return;
        var copy = enemy.handler.SpawnEnemy(EnemyIndex, transform.position);
        copy.MaxHealth *= copyHealth;
        copy.Xp = Mathf.RoundToInt(copy.Xp * copyXp);
        made++;
    }
}
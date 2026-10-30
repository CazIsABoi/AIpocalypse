using UnityEngine;

// Bulwark's grenade: a cloud on the ground that gives the player armor while they stand in it.
public class SmokeCloud : MonoBehaviour
{
    float radius, armor, timeLeft;
    PlayerStats player;
    bool applied;

    public static void Spawn(Vector3 at, float radius, float duration, float armor, PlayerStats player, GameObject effect)
    {
        var cloud = new GameObject("SmokeCloud").AddComponent<SmokeCloud>();
        cloud.transform.position = at;
        cloud.radius = radius;
        cloud.armor = armor;
        cloud.timeLeft = duration;
        cloud.player = player;
        if (effect != null) Instantiate(effect, at, Quaternion.identity, cloud.transform);
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f || player == null) { Destroy(gameObject); return; }

        Vector3 d = player.transform.position - transform.position;
        bool inside = d.x * d.x + d.z * d.z <= radius * radius;
        if (inside != applied) SetArmor(inside);
    }

    void SetArmor(bool on)
    {
        applied = on;
        player.Add(StatType.Armor, on ? armor : -armor);
    }

    void OnDestroy()
    {
        if (applied && player != null) SetArmor(false);   // never leave the bonus behind
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

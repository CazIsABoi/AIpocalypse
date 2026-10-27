using UnityEngine;

// Anything the player walks into to collect (XP, health). Once in range it's pulled toward the player,
// starting at magnetSpeed and speeding up, so it never trails behind a player who keeps running.
public abstract class Pickup : MonoBehaviour
{
    protected PlayerStats player;
    [SerializeField] private float magnetSpeed = 12f;     // speed when the pull starts
    [SerializeField] private float maxSpeed = 35f;
    [SerializeField] private float acceleration = 60f;    // units per second, per second
    [SerializeField] private float collectRadius = 0.5f;
    private bool attracted;
    private float speed;

    protected virtual void Awake()
    {
        player = FindAnyObjectByType<PlayerStats>();
    }

    protected virtual float AttractRange => player.PickupRange;
    protected virtual bool CanAttract => true;            // e.g. health: only when the player is hurt
    protected abstract void Collect();

    void Update()
    {
        if (player == null) return;

        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0;
        float distance = offset.sqrMagnitude;
        if (!attracted && CanAttract && distance < AttractRange * AttractRange)
        {
            attracted = true;
            speed = magnetSpeed;
        }

        if (attracted)
        {
            speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
        }

        if (distance < collectRadius * collectRadius && CanAttract)
        {
            Collect();
            Destroy(gameObject);
        }
    }
}

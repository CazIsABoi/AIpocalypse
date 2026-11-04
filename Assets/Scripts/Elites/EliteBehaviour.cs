using UnityEngine;

public abstract class EliteBehaviour : MonoBehaviour
{
    protected EnemyController enemy;
    protected Transform player;
    protected PlayerHealth playerHealth;
    protected abstract float Interval { get; }
    float timer;

    protected virtual void Start()
    {
        enemy = GetComponent<EnemyController>();
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        player = playerHealth.transform;
        timer = Interval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Interval;
        Tick();
    }

    protected abstract void Tick();
}
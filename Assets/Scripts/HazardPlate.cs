using System.Collections.Generic;
using UnityEngine;

public class HazardPlate : MonoBehaviour
{
    [Header("Timing (seconds)")]
    [SerializeField] private float offTime = 2f;
    [SerializeField] private float warnTime = 0.75f;
    [SerializeField] private float onTime = 1f;

    [Header("Damage")]
    [SerializeField] private float playerDamage = 10f;
    [SerializeField] private float enemyDamage = 15f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Look")]
    [SerializeField] private Renderer model;
    [SerializeField] private Material offMaterial, warnMaterial, onMaterial;

    private float timer;
    private PlayerHealth player;
    private readonly HashSet<IDamageable> hitThisPhase = new HashSet<IDamageable>();

    float Cycle => offTime + warnTime + onTime;

    void Awake()
    {
        player = FindAnyObjectByType<PlayerHealth>();

        // Checkerboard: plates on "odd" cells start half a cycle later
        int cx = Mathf.FloorToInt(transform.position.x);
        int cz = Mathf.FloorToInt(transform.position.z);
        timer = ((cx + cz) % 2 == 0) ? 0f : Cycle / 2f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= Cycle) timer -= Cycle;

        // 2. which phase?
        if (timer < offTime)
        {
            model.sharedMaterial = offMaterial;
            hitThisPhase.Clear();
        }
        else if (timer < offTime + warnTime)
        {
            // flicker between offMaterial and warnMaterial
            model.sharedMaterial = Mathf.Repeat(timer, 0.2f) < 0.1f ? offMaterial : warnMaterial;
        }
        else
        {
            model.sharedMaterial = onMaterial;
            DealDamage();
        }
    }

    void DealDamage()
    {
        if (player != null) 
        {
            Vector3 diff = player.transform.position - transform.position;
            float dx = diff.x;
            float dz = diff.z;
            if (Mathf.Abs(dx) < 0.6f && Mathf.Abs(dz) < 0.6f)
            {
                player.TakeDamage(playerDamage);
            }
        }
        if (enemyLayer != 0)
        {
            Vector3 center = transform.position + Vector3.up * 1.5f;     // box from 0 to 3 high
            Vector3 halfSize = new Vector3(0.5f, 1.5f, 0.5f);
            Collider[] hits = Physics.OverlapBox(center, halfSize, Quaternion.identity, enemyLayer);
            foreach (Collider hit in hits)
            {
                if (hit.TryGetComponent(out IDamageable target))
                {
                    if (hitThisPhase.Add(target))
                    {
                        target.TakeDamage(enemyDamage);
                    }
                }
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, new Vector3(1f, 3f, 1f)); 
    }
}
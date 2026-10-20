using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private GameObject PlayerTarget;   // Always gonna be the player
    [SerializeField] private PlayerHealth playerHealth; // Always gonna be the player health script
    [SerializeField] private Vector3 TargetPosition;    // Might be set to something else depending on Ai Quirks.
    [SerializeField] private int AiType = -1;   // 0 == Walker.
    [SerializeField] private NavMeshAgent agent;

    // Assigned Upon Spawn Based on assigned base stats
    public float Health;
    public float MaxHealth;
    public float Damage;
    // Assigned on prefab
    [SerializeField] private float AttackRange;
    [SerializeField] private float AttackCD;
    [SerializeField] private float AttackInterval;
    [SerializeField] private float Speed = 4;

    private bool IsAttacking;
    private float AttackWindow;

    [SerializeField] private GameObject DamagePopup;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Gradient HpColor;

    [SerializeField] private bool TEMP;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Health = MaxHealth;
        agent = GetComponent<NavMeshAgent>();
        PlayerTarget = GameObject.Find("Player");
        playerHealth = PlayerTarget.GetComponent<PlayerHealth>();
    }

    // Update is called once per frame
    void Update()
    {
        if (TEMP) TakeDamage(10);

        if (Health <= 0) { Destroy(gameObject); return; } // Enemy died, destroy the gameobject and exit the update loop.
        if (AttackInterval > 0) AttackInterval -= Time.deltaTime;
        if (AttackCD > 0) AttackCD -= Time.deltaTime;

        hpSlider.value = Health;
        hpSlider.maxValue = MaxHealth;
        hpSlider.fillRect.GetComponent<Image>().color = HpColor.Evaluate(Health / MaxHealth);

        if (AiType == 0)
        {
            
            if(!IsAttacking && AttackInterval <= 0 && AttackCD <= 0)
            {
                AttackInterval = 0.1f;
                Vector3 offset = PlayerTarget.transform.position - transform.position;
                Debug.DrawLine(transform.position + new Vector3(0, 0.5f, 0), transform.position + (offset.normalized * AttackRange), Color.blue);
                // Debug.Log("Distance: " + offset.sqrMagnitude + " range being: " + AttackRange * AttackRange);

                if (offset.sqrMagnitude <= AttackRange * AttackRange)
                {
                    IsAttacking = true;
                    AttackCD = 1.5f;
                    AttackWindow = 0.5f;
                    Debug.Log("attacked");
                }
            }
            if(!IsAttacking)
            {
                TargetPosition = PlayerTarget.transform.position;
                if(agent.isActiveAndEnabled) agent.SetDestination(TargetPosition);
            }
            else if(IsAttacking)
            {
                AttackWindow -= Time.deltaTime;
                agent.speed = 0;

                if (AttackWindow <= 0)
                {
                    Debug.Log("Attack Finished");
                    Vector3 offset = PlayerTarget.transform.position - transform.position;
                    if (offset.sqrMagnitude <= AttackRange * AttackRange) // If the player is still in range, apply damage
                    {
                        playerHealth.TakeDamage(Damage);
                        Debug.Log($"Player hit for {Damage}");
                    }

                    agent.speed = Speed;
                    IsAttacking = false;
                }
            }
        }
    }

    private void LateUpdate()
    {
        hpSlider.transform.parent.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }

    public void TakeDamage(float Amount)
    {
        TEMP = false;
        Health -= Amount;
        GameObject spawnedPopup = Instantiate(DamagePopup, transform.position, Quaternion.identity);
        spawnedPopup.GetComponent<DamagePopup>().TargetPosition = transform.position + new Vector3(0, 1.5f, 0);
        spawnedPopup.GetComponent<DamagePopup>().PopupDisplayContent = ((int)Amount).ToString();
    }
}

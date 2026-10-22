using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private GameObject PlayerTarget;   // Always gonna be the player
    [SerializeField] private PlayerHealth playerHealth; // Always gonna be the player health script
    [SerializeField] private Vector3 TargetPosition;    // Might be set to something else depending on Ai Quirks.
    [SerializeField] private int AiType = -1;   // 0 == Walker. 1 == Tank, 2 == Drone, 3 == Froggie, 4 == Tripod 
    [SerializeField] private NavMeshAgent agent;

    // Assigned Upon Spawn Based on assigned base stats
    public float Health;
    public float MaxHealth;
    public float Damage;
    public int Xp;
    // Assigned on prefab
    [SerializeField] private float AttackRange;
    [SerializeField] private float AttackCD;
    [SerializeField] private float AttackInterval;
    [SerializeField] private float Speed = 4;

    private bool IsAttacking;
    private float AttackWindow;
    [SerializeField] private float DamageWindow;      // An interval in which the attack deals any damage, can only deal one instance of damage.

    [SerializeField] private GameObject DamagePopup;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Gradient HpColor;
    [SerializeField] private GameObject xpPrefab;

    public EntityHandler handler;
    public bool SpeedBoostIncrease;     // if cap is reached 
    private float SpeedMax;
   
    private bool died;

    [SerializeField] private LayerMask PlayerLayer;
    [SerializeField] private GameObject TripodBulletPrefab;
    [SerializeField] private GameObject TripodShootPivot;
    [SerializeField] private GameObject DroneExplosionPrefab;
    private HitFlash flash;

    [SerializeField] private float popupSumWindow = 0.5f;   // hits closer together than this add onto the same number
    // Normal hits and crits sum up on separate numbers, so a crit never recolors the normal number
    private DamagePopup normalPopup, critPopup;
    private float normalTotal, critTotal, lastNormalTime, lastCritTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Health = MaxHealth;
        agent = GetComponent<NavMeshAgent>();
        flash = GetComponent<HitFlash>();
        PlayerTarget = GameObject.Find("Player");
        playerHealth = PlayerTarget.GetComponent<PlayerHealth>();
        SpeedMax = Speed * 3;
    }

    // Update is called once per frame
    void Update()
    {
        

        if (Health <= 0 && !died)
        {
            died = true;
        }

        if (died) {
            GameObject spawnedXp = Instantiate(xpPrefab, transform.position, Quaternion.identity, transform.parent);
            spawnedXp.GetComponent<XpOrb>().Amount = Xp;
            AudioManager.Instance.PlayAt(AiType == 2 ? Sfx.DroneDeath : Sfx.EnemyDeath, transform.position);
            handler?.RegisterKill();
            Destroy(gameObject); return;  
            } // Enemy died, destroy the gameobject and exit the update loop.
        if (AttackInterval > 0) AttackInterval -= Time.deltaTime;
        if (AttackCD > 0) AttackCD -= Time.deltaTime;
        if (DamageWindow > 0) DamageWindow -= Time.deltaTime;

        hpSlider.value = Health;
        hpSlider.maxValue = MaxHealth;
        hpSlider.fillRect.GetComponent<Image>().color = HpColor.Evaluate(Health / MaxHealth);

        if(SpeedBoostIncrease)
        {
            if(Speed < SpeedMax)
            {
                Speed += SpeedMax * 0.04f * Time.deltaTime;
            }
        }

        if (AiType == 0) // Walker
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
                    AudioManager.Instance?.PlayAt(Sfx.EnemyAttack, transform.position);
                    DamageWindow = 0.5f;
                    // Debug.Log("attacked");
                }
            }
            if(!IsAttacking)
            {
                
                TargetPosition = PlayerTarget.transform.position;
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(TargetPosition);
                }
            }
            else if(IsAttacking)
            {
                AttackWindow -= Time.deltaTime;
                agent.speed = 0;

                if (agent.isActiveAndEnabled) agent.isStopped = true;

                if(DamageWindow > 0.1f && DamageWindow < 0.2f)
                {
                    Debug.DrawLine(transform.position, transform.position + transform.forward * 1.25f, Color.purple);
                    Debug.DrawLine(transform.position + transform.forward * 1.25f, transform.position + transform.forward * 1.25f + new Vector3(0, 2, 0), Color.green);
                    Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1.15f, 0.8f, PlayerLayer);
                    foreach (Collider hit in hits)
                    {
                        if(hit.gameObject.GetComponent<PlayerHealth>())
                        {
                            hit.gameObject.GetComponent<PlayerHealth>().TakeDamage(Damage);
                            DamageWindow = 5;
                        }
                    }
                }
                if (AttackWindow <= 0)
                {
                   
                    agent.speed = Speed;
                    IsAttacking = false;
                }
            }
        }
        else if (AiType == 1) // Tank
        {

            if (!IsAttacking && AttackInterval <= 0 && AttackCD <= 0)
            {
                AttackInterval = 0.1f;
                Vector3 offset = PlayerTarget.transform.position - transform.position;
                Debug.DrawLine(transform.position + new Vector3(0, 0.5f, 0), transform.position + (offset.normalized * AttackRange), Color.blue);
                // Debug.Log("Distance: " + offset.sqrMagnitude + " range being: " + AttackRange * AttackRange);

                if (offset.sqrMagnitude <= AttackRange * AttackRange)
                {
                    IsAttacking = true;
                    AttackCD = 9f;
                    AttackWindow = 6f;
                    DamageWindow = 0.7f;
                    AudioManager.Instance?.PlayAt(Sfx.EnemyAttack, transform.position);
                    Debug.Log("attacked");
                    GetComponentInChildren<Animator>().SetBool("IsAttacking", true);
                }
            }
            if (!IsAttacking)
            {

                TargetPosition = PlayerTarget.transform.position;
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(TargetPosition);
                }
            }
            else if (IsAttacking)
            {
                float spinSpeed = 1;
                if (AttackWindow <= 6f && AttackWindow > 4.5f) spinSpeed = Mathf.InverseLerp(6f, 4.5f, AttackWindow);
                else if (AttackWindow <= 4.5f && AttackWindow > 1.5f) spinSpeed = 1;
                else spinSpeed = Mathf.InverseLerp(0f, 1.5f, AttackWindow);
                GetComponentInChildren<Animator>().SetFloat("SpinSpeed", spinSpeed); 

                if(DamageWindow < 0.1f)
                {
                    Collider[] hits = Physics.OverlapSphere(transform.position, 2.2f, PlayerLayer);
                    foreach (Collider hit in hits)
                    {
                        if (hit.gameObject.GetComponent<PlayerHealth>())
                        {
                            hit.gameObject.GetComponent<PlayerHealth>().TakeDamage(Damage);
                            DamageWindow = 0.54f;
                        }
                    }
                }

                AttackWindow -= Time.deltaTime;
                TargetPosition = PlayerTarget.transform.position;
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(TargetPosition);
                }
                agent.speed = 1.5f;

                //if (agent.isActiveAndEnabled) agent.isStopped = true;

                if (AttackWindow <= 0)
                {
                    // Debug.Log("Attack Finished");
                    Vector3 offset = PlayerTarget.transform.position - transform.position;
                    if (offset.sqrMagnitude <= AttackRange * AttackRange) // If the player is still in range, apply damage
                    {
                        playerHealth.TakeDamage(Damage);
                        // Debug.Log($"Player hit for {Damage}");
                    }

                    GetComponentInChildren<Animator>().SetBool("IsAttacking", false);
                    GetComponentInChildren<Animator>().Play("Walk");
                    agent.speed = Speed;
                    IsAttacking = false;
                }
            }
        }
        if (AiType == 2) // Drone
        {

            if (!IsAttacking && AttackInterval <= 0 && AttackCD <= 0)
            {
                AttackInterval = 0.1f;
                Vector3 offset = PlayerTarget.transform.position - transform.position;

                if (offset.sqrMagnitude <= AttackRange * AttackRange)
                {
                    agent.speed = 15;
                    IsAttacking = true;
                    AttackCD = 2f;
                    AttackWindow = 1.2f;
                    AudioManager.Instance?.PlayAt(Sfx.EnemyAttack, transform.position);
                    DamageWindow = 1.2f;
                    Debug.Log("attacked");
                }
            }
            if (!IsAttacking)
            {

                TargetPosition = PlayerTarget.transform.position;
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(TargetPosition);
                }
            }
            else if (IsAttacking)
            {
                TargetPosition = PlayerTarget.transform.position;
                if (agent.isActiveAndEnabled && AttackWindow > 0.41f)
                {
                    agent.isStopped = false;
                    agent.SetDestination(TargetPosition);
                }

                AttackWindow -= Time.deltaTime;
                if (AttackWindow >= 1.0f && AttackWindow < 1.1f) GetComponentInChildren<Animator>().Play("Explode");
                if (AttackWindow <= 1 && AttackWindow > 0.16f) agent.speed = Mathf.Lerp(15, 0, 23 * Time.deltaTime);
                else if (AttackWindow <= 0.15) agent.speed = 0;

                if(DamageWindow < 0.08f)
                {
                    GameObject spawnedExplosion = Instantiate(DroneExplosionPrefab, transform.position, Quaternion.identity);
                    DamageWindow = 5;
                }

                if (AttackWindow <= 0)
                {
                    Collider[] hits = Physics.OverlapSphere(transform.position, 2.65f, PlayerLayer);
                    foreach (Collider hit in hits)
                    {
                        if (hit.gameObject.GetComponent<PlayerHealth>())
                        {
                            hit.gameObject.GetComponent<PlayerHealth>().TakeDamage(Damage);
                            //DamageWindow = 5;
                        }
                    }
                    //GameObject spawnedExplosion = Instantiate(DroneExplosionPrefab, transform.position, Quaternion.identity);

                    Destroy(gameObject);
                }
            }
        }
        else if (AiType == 3) // Froggie
        {

            if (!IsAttacking && AttackInterval <= 0 && AttackCD <= 0)
            {
                AttackInterval = 0.05f;
                IsAttacking = true;
                AttackWindow = 1f;
                DamageWindow = 1f;
                AttackCD = 2f;
                GetComponentInChildren<Animator>().Play("Hop");
            }
            if (!IsAttacking)
            {
                TargetPosition = PlayerTarget.transform.position;
                Vector3 direction = (PlayerTarget.transform.position - transform.position);
                transform.rotation = Quaternion.LookRotation(direction);
                agent.speed = 0;

                if (agent.isActiveAndEnabled)
                {
                    agent.SetDestination(TargetPosition);
                }
            }
            else if (IsAttacking)
            {

                AttackWindow -= Time.deltaTime;
                agent.speed = 20;

                if (agent.isActiveAndEnabled) agent.isStopped = false;

                if(DamageWindow > 0.05f && DamageWindow < 0.65f)
                {
                    Collider[] hits = Physics.OverlapSphere(transform.position, 1f, PlayerLayer);
                    foreach (Collider hit in hits)
                    {
                        if (hit.gameObject.GetComponent<PlayerHealth>())
                        {
                            hit.gameObject.GetComponent<PlayerHealth>().TakeDamage(Damage);
                            DamageWindow = 5;
                        }
                    }
                }

                if (AttackWindow <= 0)
                {

                    agent.speed = Speed;
                    IsAttacking = false;
                }
            }
        }
        else if (AiType == 4) // Tripod
        {

            if (!IsAttacking && AttackInterval <= 0 && AttackCD <= 0)
            {
                AttackInterval = 0.1f;
                Vector3 offset = PlayerTarget.transform.position - transform.position;
                Debug.DrawLine(transform.position + new Vector3(0, 0.5f, 0), transform.position + (offset.normalized * AttackRange), Color.blue);
                // Debug.Log("Distance: " + offset.sqrMagnitude + " range being: " + AttackRange * AttackRange);

                if (offset.sqrMagnitude <= AttackRange * AttackRange)
                {
                    GetComponentInChildren<Animator>().SetBool("IsFiring", true);
                    GetComponentInChildren<Animator>().Play("TripodShoot");
                    IsAttacking = true;
                    AttackCD = 9f;
                    AttackWindow = 6f;
                    DamageWindow = 0f;
                    AudioManager.Instance?.PlayAt(Sfx.EnemyAttack, transform.position);
                    Debug.Log("attacked");
                    
                }
            }
            if (!IsAttacking)
            {
                TargetPosition = PlayerTarget.transform.position;
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(TargetPosition);
                }
            }
            else if (IsAttacking)
            {

                AttackWindow -= Time.deltaTime;

                Vector3 direction = (PlayerTarget.transform.position - transform.position);
                transform.rotation = Quaternion.LookRotation(direction);
                TargetPosition = PlayerTarget.transform.position;
                

                if (agent.isActiveAndEnabled) agent.isStopped = true;

                if(DamageWindow <= 0)
                {
                    GameObject spawnedBullet = Instantiate(TripodBulletPrefab, TripodShootPivot.transform.position, Quaternion.identity);
                    spawnedBullet.GetComponent<ProjectileController>().Direction = direction.normalized;
                    spawnedBullet.GetComponent<ProjectileController>().Damage = Damage;
                    DamageWindow = 0.958f;
                }
                if (AttackWindow <= 0)
                {
                    // Debug.Log("Attack Finished");
                    Vector3 offset = PlayerTarget.transform.position - transform.position;
                    if (offset.sqrMagnitude <= AttackRange * AttackRange) // If the player is still in range, apply damage
                    {
                        playerHealth.TakeDamage(Damage);
                        // Debug.Log($"Player hit for {Damage}");
                    }

                    GetComponentInChildren<Animator>().SetBool("IsFiring", false);
                    agent.speed = Speed;
                    IsAttacking = false;
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (AiType == 0)
        {
            Gizmos.DrawWireSphere(transform.position + transform.forward * 1.1f, 0.8f);
        }
        if (AiType == 1)
        {
            Gizmos.DrawWireSphere(transform.position, 2.2f);
        }
        if (AiType == 2)
        {
            Gizmos.DrawWireSphere(transform.position, 2.65f);
        }
        if (AiType == 3)
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    private void LateUpdate()
    {
        hpSlider.transform.parent.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }

    // IDamageable entry point (hazards etc.) — no crit
    public void TakeDamage(float Amount)
    {
        TakeDamage(Amount, 0f, 1f);
    }

    public float TakeDamage(float Amount, float CritChance, float CritDamage)
    {
        float CritRoll = Random.Range(0f, 100f);
        bool IsCrit = false;
        if (CritChance*100f >= CritRoll) IsCrit = true;

        if(IsCrit) Amount *= CritDamage;
        Health -= Amount;
        
        if (IsCrit) ShowPopup(ref critPopup, ref critTotal, ref lastCritTime, Amount, true);
        else ShowPopup(ref normalPopup, ref normalTotal, ref lastNormalTime, Amount, false);
        flash?.Flash();

        AudioManager.Instance.PlayAt(Sfx.EnemyHit, transform.position);
        if (IsCrit) AudioManager.Instance?.PlayAt(Sfx.CritHit, transform.position);   // extra punch layered on crits
        return Amount;
    }

    // Rapid hits add up on one number instead of spawning a new popup every hit.
    // 'ref' lets the same method update either the normal or the crit set of fields.
    void ShowPopup(ref DamagePopup popup, ref float total, ref float lastTime, float amount, bool crit)
    {
        if (popup != null && Time.time - lastTime < popupSumWindow)
        {
            total += amount;
            popup.Refresh();
        }
        else
        {
            popup = Instantiate(DamagePopup, transform.position, Quaternion.identity).GetComponent<DamagePopup>();
            // crits float a bit higher so the two numbers don't overlap
            popup.TargetPosition = transform.position + new Vector3(0, crit ? 2f : 1.5f, 0);
            popup.IsCrit = crit;
            total = amount;
        }
        popup.PopupDisplayContent = ((int)total).ToString();
        lastTime = Time.time;
    }
}

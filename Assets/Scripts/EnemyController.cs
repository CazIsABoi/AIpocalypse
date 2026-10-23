using NUnit.Framework;
using System.Collections.Generic;
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

    // Artifimind Stuff, Boss
    [SerializeField] private int BossPhase;
    [SerializeField] private MindBarrierCore[] BarrierCores = new MindBarrierCore[3];
    [SerializeField] public bool ProtectiveBarrierActive;
    [SerializeField] private GameObject BarrierObj;

    [SerializeField] private float SpawnCooldown;
    [SerializeField] private GameObject[] SideRooms = new GameObject[4];
    [SerializeField] private EnemyData[] enemyPool = new EnemyData[5];
    [SerializeField] private RunClock clock;

    [SerializeField] private float SubmergeInCountdown;
    [SerializeField] private float SubmergeDuration;
    [SerializeField] private Vector3 SubmergeOriginalPos;

    [SerializeField] private List<GameObject> spawnedBossEnemies = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Health = MaxHealth;
        agent = GetComponent<NavMeshAgent>();
        flash = GetComponent<HitFlash>();
        PlayerTarget = GameObject.Find("Player");
        playerHealth = PlayerTarget.GetComponent<PlayerHealth>();
        SpeedMax = Speed * 3;
        if (AiType == 10) clock = FindAnyObjectByType<RunClock>();   // only the boss needs it (a scene search per spawn adds up)
    }

    // Update is called once per frame
    void Update()
    {
        

        if (Health <= 0 && !died)
        {
            died = true;
        }

        if (AiType != 10)
        {
            if (died)
            {
                GameObject spawnedXp = Instantiate(xpPrefab, transform.position, Quaternion.identity, transform.parent);
                spawnedXp.GetComponent<XpOrb>().Amount = Xp;
                AudioManager.Instance.PlayAt(AiType == 2 ? Sfx.DroneDeath : Sfx.EnemyDeath, transform.position);
                // boss adds are spawned without a handler, so find the one in the scene for them
                if (handler == null) handler = FindAnyObjectByType<EntityHandler>();
                handler?.RegisterKill();
                Destroy(gameObject); return;
            } // Enemy died, destroy the gameobject and exit the update loop.
        }
        else
        {
            if (died)
            {
                // the adds aren't children of the EntityHandler, so ClearEnemies() wouldn't remove them
                foreach (GameObject add in spawnedBossEnemies) if (add != null) Destroy(add);
                spawnedBossEnemies.Clear();

                clock.RevertBossUIChanges();
                //player.Teleport(new Vector3(-500f, 2, -13.5f));

                FindAnyObjectByType<GameController>().NextFloor();
                AudioManager.Instance?.Play(Sfx.BossDeath);
                clock.StartEndless();                          // switches to endless mode
                AudioManager.Instance?.Play(Sfx.OvertimeStart);
                Destroy(gameObject); return;
            }
        }

        if (AttackInterval > 0) AttackInterval -= Time.deltaTime;
        if (AttackCD > 0) AttackCD -= Time.deltaTime;
        if (DamageWindow > 0) DamageWindow -= Time.deltaTime;

        if (AiType != 10)
        {
            hpSlider.value = Health;
            hpSlider.maxValue = MaxHealth;
            hpSlider.fillRect.GetComponent<Image>().color = HpColor.Evaluate(Health / MaxHealth);
        }

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
                    AttackCD = 2f;
                    AttackWindow = 1f;
                    AudioManager.Instance?.PlayAt(Sfx.EnemyAttack, transform.position);
                    DamageWindow = 0.5f;
                    // Debug.Log("attacked");
                    GetComponentInChildren<Animator>().Play("Stomp");
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
        else if(AiType == 10)
        {
            if (clock.TimeLeft > 0f)   // the clock's own boss time, not a hardcoded 900
            {
                SubmergeOriginalPos = transform.position;
                SubmergeInCountdown = Random.Range(10f, 30f);
                return;
            }

            if (SubmergeInCountdown > 0 && SubmergeDuration <= 0) SubmergeInCountdown -= Time.deltaTime;
            else if(SubmergeInCountdown <= 0 && SubmergeDuration <= 0)
            {
                SubmergeInCountdown = Random.Range(10f, 30f);
                SubmergeDuration = Random.Range(4f, 11f);
            }

            if(SubmergeDuration > 0)
            {
                SubmergeDuration -= Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, SubmergeOriginalPos + new Vector3(0, -10, 0), 4 * Time.deltaTime);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, SubmergeOriginalPos, 4 * Time.deltaTime);
            }


            if (ProtectiveBarrierActive) BarrierObj.SetActive(true);
            else BarrierObj.SetActive(false);

            if (SpawnCooldown > 0)
            {
                SpawnCooldown -= Time.deltaTime;
            }
            else
            {
                spawnedBossEnemies.RemoveAll(e => e == null);   // drop dead adds so the cap counts living ones
                if (spawnedBossEnemies.Count < 80)
                {
                    if (BossPhase == 0)
                    {
                        int RoomChoice = Random.Range(0, 4);
                        GameObject enemy = Instantiate(enemyPool[0].EnemyPrefab, SideRooms[RoomChoice].transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity);
                        enemy.GetComponent<EnemyController>().MaxHealth = enemyPool[0].BaseHealth * 25f;
                        enemy.GetComponent<EnemyController>().Damage = enemyPool[0].BaseDamage * 3f;
                        spawnedBossEnemies.Add(enemy);
                        SpawnCooldown = 1.5f;
                    }
                    if (BossPhase == 1)
                    {
                        int RoomChoice = Random.Range(0, 4);
                        GameObject enemy = Instantiate(enemyPool[4].EnemyPrefab, SideRooms[RoomChoice].transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity);
                        enemy.GetComponent<EnemyController>().MaxHealth = enemyPool[4].BaseHealth * 25f;
                        enemy.GetComponent<EnemyController>().Damage = enemyPool[4].BaseDamage * 3f;
                        spawnedBossEnemies.Add(enemy);
                        SpawnCooldown = 1.2f;
                    }
                    if (BossPhase == 2)
                    {
                        int RoomChoice = Random.Range(0, 4);
                        GameObject enemy = Instantiate(enemyPool[2].EnemyPrefab, SideRooms[RoomChoice].transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity);
                        enemy.GetComponent<EnemyController>().MaxHealth = enemyPool[2].BaseHealth * 25f;
                        enemy.GetComponent<EnemyController>().Damage = enemyPool[2].BaseDamage * 3f;
                        spawnedBossEnemies.Add(enemy);
                        SpawnCooldown = 1.1f;
                    }
                    if (BossPhase == 3)
                    {
                        int RoomChoice = Random.Range(0, 4);
                        GameObject enemy = Instantiate(enemyPool[3].EnemyPrefab, SideRooms[RoomChoice].transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity);
                        enemy.GetComponent<EnemyController>().MaxHealth = enemyPool[3].BaseHealth * 25f;
                        enemy.GetComponent<EnemyController>().Damage = enemyPool[3].BaseDamage * 3f;
                        spawnedBossEnemies.Add(enemy);
                        SpawnCooldown = 1f;
                    }
                    if (BossPhase == 4)
                    {
                        int RoomChoice = Random.Range(0, 4);
                        GameObject enemy = Instantiate(enemyPool[1].EnemyPrefab, SideRooms[RoomChoice].transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity);
                        enemy.GetComponent<EnemyController>().MaxHealth = enemyPool[1].BaseHealth * 25f;
                        enemy.GetComponent<EnemyController>().Damage = enemyPool[1].BaseDamage * 3f;
                        spawnedBossEnemies.Add(enemy);
                        SpawnCooldown = 2f;
                    }
                    if (BossPhase == 5)
                    {
                        int RoomChoice = Random.Range(0, 4);
                        int EnemyChoice = Random.Range(0, 5);
                        GameObject enemy = Instantiate(enemyPool[EnemyChoice].EnemyPrefab, SideRooms[RoomChoice].transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity);
                        enemy.GetComponent<EnemyController>().MaxHealth = enemyPool[EnemyChoice].BaseHealth * 25f;
                        enemy.GetComponent<EnemyController>().Damage = enemyPool[EnemyChoice].BaseDamage * 3f;
                        spawnedBossEnemies.Add(enemy);
                        SpawnCooldown = 1.4f;
                    }
                    if (BossPhase == 6)
                    {
                        int RoomChoice = Random.Range(0, 4);
                        int EnemyChoice = Random.Range(0, 5);
                        GameObject enemy = Instantiate(enemyPool[EnemyChoice].EnemyPrefab, SideRooms[RoomChoice].transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity);
                        enemy.GetComponent<EnemyController>().MaxHealth = enemyPool[EnemyChoice].BaseHealth * 25f;
                        enemy.GetComponent<EnemyController>().Damage = enemyPool[EnemyChoice].BaseDamage * 3f;
                        spawnedBossEnemies.Add(enemy);
                        SpawnCooldown = 0.5f;
                    }
                }
                else
                {
                    for (int i = 0; i < spawnedBossEnemies.Count; i++)
                    {
                        if (spawnedBossEnemies[i] == null) spawnedBossEnemies.RemoveAt(i);
                    }
                }
            }

            if (Health <= 750000f && BossPhase == 0)
            {
                BarrierCores[0].BarrierActive = false;
                BossPhase = 1;
                ProtectiveBarrierActive = true;
            }
            else if(BossPhase == 1 && BarrierCores[0].destroyed)
            {
                BossPhase = 2;
                ProtectiveBarrierActive = false;
            }
            else if (Health <= 500000f && BossPhase == 2)
            {
                BarrierCores[1].BarrierActive = false;
                BossPhase = 3;
                ProtectiveBarrierActive = true;
            }
            else if (BossPhase == 3 && BarrierCores[1].destroyed)
            {
                BossPhase = 4;
                ProtectiveBarrierActive = false;
            }
            else if (Health <= 250000f && BossPhase == 4)
            {
                BarrierCores[2].BarrierActive = false;
                BossPhase = 5;
                ProtectiveBarrierActive = true;
            }
            else if (BossPhase == 5 && BarrierCores[2].destroyed)
            {
                BossPhase = 6;
                ProtectiveBarrierActive = false;
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
        if(AiType!=10)  hpSlider.transform.parent.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }

    // IDamageable entry point (hazards etc.) — no crit
    public void TakeDamage(float Amount)
    {
        TakeDamage(Amount, 0f, 1f);
    }

    public float TakeDamage(float Amount, float CritChance, float CritDamage)
    {
        if (AiType == 10 && ProtectiveBarrierActive) Amount = 0;

        float CritRoll = Random.Range(0f, 100f);
        bool IsCrit = false;
        if (CritChance*100f >= CritRoll) IsCrit = true;

        if(IsCrit) Amount *= CritDamage;
        Health -= Amount;
        
        if (Amount > 0f)   // shielded boss: no "0" popups or flash for every bullet
        {
            if (IsCrit) ShowPopup(ref critPopup, ref critTotal, ref lastCritTime, Amount, true);
            else ShowPopup(ref normalPopup, ref normalTotal, ref lastNormalTime, Amount, false);
            flash?.Flash();
        }

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

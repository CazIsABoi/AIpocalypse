using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private InputActionReference Melee;
    [SerializeField] private InputActionReference LeftClick;
    [SerializeField] private InputActionReference RightClick;

    [SerializeField] private LayerMask EnemyLayer;
    private PlayerStats stats;
    private PlayerHealth health;
    private PlayerEffects effects;
    [SerializeField, Range(0f, 1f)] private float meleeShootLock = 1f;   // part of the melee cooldown where shooting is blocked
    private float shootLock;
    [SerializeField, Range(0f, 1f)] private float meleeOverloadDrain = 0.35f;   // how much Overload charge a connecting swing removes
    private OverloadDischarge overload;

    [SerializeField] private GameObject RiflePivot;
    [SerializeField] private GameObject RifleMuzzle;

    [SerializeField] private Animator animator;
    public int BulletAmount = 1;
    public int BulletPierce = 1;
    [SerializeField] private GameObject BulletPrefab;
    [SerializeField] private float FirearmBaseCooldown = 0.2f;
    [SerializeField] private float FirearmCooldown;
    [SerializeField] private float CurrentFirearmCooldown;

    [SerializeField] private float MeleeBaseCooldown = 0.5f;
    [SerializeField] private float MeleeCooldown;
    [SerializeField] private float CurrentMeleeCooldown;
    [SerializeField] private List<GameObject> hitEnemies = new List<GameObject>();

    public float MeleeDamage;
    public float RangedDamage;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
        overload = GetComponent<OverloadDischarge>();
        effects = GetComponent<PlayerEffects>();
        if (effects == null) effects = gameObject.AddComponent<PlayerEffects>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        Vector3 direction = CalculateAimingDirection();
        Quaternion rotation = Quaternion.LookRotation(direction);
        RiflePivot.transform.rotation = rotation;

        Debug.DrawLine(transform.position, transform.position + CalculateAimingDirection() * 1.25f);
        
        MeleeCooldown = MeleeBaseCooldown / Mathf.Max(0.2f, 1f + stats.AttackSpeed + stats.meleeSpeedPercent);   // shared + melee-only speed
        MeleeDamage = stats.MeleeHit;          // Damage x 4 x Melee Damage % (PlayerStats)
        if (CurrentMeleeCooldown > 0) CurrentMeleeCooldown -= Time.deltaTime;

        FirearmCooldown = FirearmBaseCooldown / (1f + stats.AttackSpeed);
        RangedDamage = stats.GunHit;           // Damage x Gun Damage % (PlayerStats), melee is 40.
        if (CurrentFirearmCooldown > 0) CurrentFirearmCooldown -= Time.deltaTime;
        if (shootLock > 0) shootLock -= Time.deltaTime;

        BulletAmount = stats.BulletAmount;
        BulletPierce = stats.BulletPierce;

        if (RightClick.action.IsPressed())
        {
            if (CurrentMeleeCooldown <= 0)
            {
                animator.SetBool("IsAttacking", true);
                CurrentMeleeCooldown = MeleeCooldown;
                shootLock = MeleeCooldown * meleeShootLock;   // no shooting during the swing (it lasts ~one cooldown)
                CancelInvoke(nameof(ShootGun));                // and stop a burst that was already queued

                animator.GetComponentInChildren<Animator>().SetFloat("SlashSpeedVisual", 0.5f / MeleeCooldown);

                AudioManager.Instance.Play(Sfx.Melee);

                bool hitAny = false, crit = false;
                Collider[] hits = Physics.OverlapSphere(transform.position + CalculateAimingDirection() * 1.25f, stats.MeleeRadius, EnemyLayer);   // Melee Range widens the swing
                foreach (Collider hit in hits)
                {
                    float amount = 0f;
                    float healthBefore = 0f;
                    if (hit.TryGetComponent(out EnemyController enemy))
                    {
                        healthBefore = enemy.Health;

                        hitAny = true;
                        amount = enemy.TakeDamage(MeleeDamage, stats.CritChance, stats.CritDamage);   // enemies: with crits
                        if (amount > MeleeDamage) crit = true;   // damage dealt above normal = it was a crit
                        amount = amount * (stats.Lifesteal + stats.MeleeLifesteal); // Lifesteal (+ melee-only lifesteal): heal for a percentage of the damage dealt
                        health.HealLifesteal(Mathf.Clamp(amount, 0f, Mathf.Max(0f, healthBefore))); // never more than the enemy had, never negative (enemy already dead this frame)
                        CameraShake.Instance?.Add(0.2f);
                        CombatEvents.Hit(enemy, healthBefore - enemy.Health, HitSource.Melee);

                    }
                    else if (hit.TryGetComponent(out IDamageable target))
                        { target.TakeDamage(MeleeDamage); CameraShake.Instance?.Add(0.1f); }                                    // anything else hittable, e.g. the core
                }
                CombatEvents.Swung(transform.position + CalculateAimingDirection() * 1.25f, hitAny);
                if (hitAny)
                {
                    Hitstop.Instance?.Stop(crit ? 0.06f : 0.03f);
                    overload?.ReduceCharge(meleeOverloadDrain);   // swinging into a crowd keeps the robots from discharging
                    AudioManager.Instance?.Play(Sfx.MeleeHit);   // the swing connected (the swing itself is Sfx.Melee)
                }
            }
        }
        else if (!RightClick.action.IsPressed())
        {
            if (CurrentMeleeCooldown < 0) CurrentMeleeCooldown = 0;
            animator.SetBool("IsAttacking", false);
        }

        if (LeftClick.action.IsPressed() && shootLock <= 0)
        {
            if (CurrentFirearmCooldown <= 0)
            {
                CurrentFirearmCooldown = FirearmCooldown;
                for(int i = 0; i < BulletAmount; i++) Invoke(nameof(ShootGun), (i * 0.08f) / (1f + stats.AttackSpeed));
            }
        }
        else if (!LeftClick.action.IsPressed())
        {
            if (CurrentFirearmCooldown < 0) CurrentFirearmCooldown = 0;
        }
    }
    public void ShootGun()
    {
        GameObject shotBullet = Instantiate(BulletPrefab, RifleMuzzle.transform.position, Quaternion.identity);
        ProjectileController c = shotBullet.GetComponent<ProjectileController>();
        c.Direction = CalculateAimingDirection();
        c.Damage = RangedDamage;
        c.BulletPierce = Mathf.Max(1, BulletPierce);   // 0 = bullet hits nothing
        c.CritChance = stats.CritChance;
        c.CritDamage = stats.CritDamage;
        c.Owner = health;
        c.Lifesteal = stats.Lifesteal;
        c.BulletSpeed = c.BulletSpeed * (1f + stats.ProjectileSpeed);
        c.Bounces = effects.Level(EffectType.Ricochet);                 // 1 bounce per Ricochet level
        AudioManager.Instance.Play(Sfx.Shoot);
    }
    private Vector3 CalculateAimingDirection() => Aim.Direction(transform.position);   // mouse or right stick
}

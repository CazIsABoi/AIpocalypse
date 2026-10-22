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
    [SerializeField, Range(0f, 1f)] private float meleeShootLock = 1f;   // part of the melee cooldown where shooting is blocked
    private float shootLock;

    [SerializeField] private GameObject RiflePivot;
    [SerializeField] private GameObject RifleMuzzle;        // Its shooting Point.

    public int BulletAmount = 1;
    public int BulletPierce = 1;
    [SerializeField] private GameObject BulletPrefab;
    [SerializeField] private float FirearmBaseCooldown = 0.2f;   // Will be 0.2
    [SerializeField] private float FirearmCooldown;
    [SerializeField] private float CurrentFirearmCooldown;

    [SerializeField] private float MeleeBaseCooldown = 0.5f;   // Will be 0.5
    [SerializeField] private float MeleeCooldown;
    [SerializeField] private float CurrentMeleeCooldown;
    [SerializeField] private List<GameObject> hitEnemies = new List<GameObject>();

    public float MeleeDamage;
    public float RangedDamage;
    //public float CritChance = 0.1f;        // 0 -> 1, aka 0 -> 100%, 10% by default
    //public float CritDamage = 2f;          // Multiplies Damage Upright, 2f is 2x damage, 3f is 3x damage

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0f) return;
        Vector3 direction = CalculateAimingDirection();
        Quaternion rotation = Quaternion.LookRotation(-direction);
        RiflePivot.transform.rotation = rotation;

        //Debug.DrawLine(transform.position, GetMouseWorldPosition(), Color.red);
        Debug.DrawLine(transform.position, transform.position + CalculateAimingDirection() * 1.25f);
        
        MeleeCooldown = MeleeBaseCooldown / (1f + stats.AttackSpeed);  // Add all multipliers here
        MeleeDamage = stats.Damage * 4;         // Add melee damage multipliers here
        if (CurrentMeleeCooldown > 0) CurrentMeleeCooldown -= Time.deltaTime;

        FirearmCooldown = FirearmBaseCooldown / (1f + stats.AttackSpeed);  // Add all multipliers here
        RangedDamage = stats.Damage;         // Add melee damage multipliers here. 5 dmg per bullet by default, 25 dps, melee is 40.
        if (CurrentFirearmCooldown > 0) CurrentFirearmCooldown -= Time.deltaTime;
        if (shootLock > 0) shootLock -= Time.deltaTime;

        BulletAmount = stats.BulletAmount;
        BulletPierce = stats.BulletPierce;

        if (RightClick.action.IsPressed())
        {
            if (CurrentMeleeCooldown <= 0)
            {
                CurrentMeleeCooldown = MeleeCooldown;
                shootLock = MeleeCooldown * meleeShootLock;   // no shooting during the swing (it lasts ~one cooldown)
                CancelInvoke(nameof(ShootGun));                // and stop a burst that was already queued

                RiflePivot.GetComponentInChildren<Animator>().SetFloat("MeleeSpeed", 0.5f / MeleeCooldown);
                RiflePivot.GetComponentInChildren<Animator>().SetBool("IsSlashing", true);
                RiflePivot.GetComponentInChildren<Animator>().Play("RifleSlash");

                AudioManager.Instance.Play(Sfx.Melee);

                bool hitAny = false, crit = false;
                Collider[] hits = Physics.OverlapSphere(transform.position + CalculateAimingDirection() * 1.25f, 1.5f, EnemyLayer);
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
                        amount = amount * stats.Lifesteal; // Lifesteal: heal for a percentage of the damage dealt
                        health.Heal(Mathf.Clamp(amount, 0f, Mathf.Max(0f, healthBefore))); // never more than the enemy had, never negative (enemy already dead this frame)
                        CameraShake.Instance?.Add(0.2f);

                    }
                    else if (hit.TryGetComponent(out IDamageable target))
                        { target.TakeDamage(MeleeDamage); CameraShake.Instance?.Add(0.1f); }                                    // anything else hittable, e.g. the core
                }
                if (hitAny)
                {
                    Hitstop.Instance?.Stop(crit ? 0.06f : 0.03f);
                    AudioManager.Instance?.Play(Sfx.MeleeHit);   // the swing connected (the swing itself is Sfx.Melee)
                }
            }
        }
        else if (!RightClick.action.IsPressed())
        {
            if (CurrentMeleeCooldown < 0) CurrentMeleeCooldown = 0;
            RiflePivot.GetComponentInChildren<Animator>().SetBool("IsSlashing", false);
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
        c.CritChance = stats.CritChance;
        c.CritDamage = stats.CritDamage;
        c.Owner = health;
        c.Lifesteal = stats.Lifesteal;
        AudioManager.Instance.Play(Sfx.Shoot);
    }
    private Vector3 CalculateAimingDirection()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        mousePos.y = 0;
        Vector3 aimDirection = (mousePos - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
        return aimDirection;
    }
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
}

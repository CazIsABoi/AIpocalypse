using NUnit.Framework;
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
    }

    // Update is called once per frame
    void Update()
    {

        Vector3 direction = CalculateAimingDirection();
        Quaternion rotation = Quaternion.LookRotation(-direction);
        RiflePivot.transform.rotation = rotation;

        //Debug.DrawLine(transform.position, GetMouseWorldPosition(), Color.red);
        Debug.DrawLine(transform.position, transform.position + CalculateAimingDirection() * 1.25f);

        MeleeCooldown = MeleeBaseCooldown;  // Add all multipliers here
        MeleeDamage = stats.Damage * 4;         // Add melee damage multipliers here
        if (CurrentMeleeCooldown > 0) CurrentMeleeCooldown -= Time.deltaTime;

        FirearmCooldown = FirearmBaseCooldown;  // Add all multipliers here
        RangedDamage = stats.Damage;         // Add melee damage multipliers here. 5 dmg per bullet by default, 25 dps, melee is 40.
        if (CurrentFirearmCooldown > 0) CurrentFirearmCooldown -= Time.deltaTime;

        BulletAmount = stats.BulletAmount;
        BulletPierce = stats.BulletPierce;

        if (RightClick.action.IsPressed())
        {
            if (CurrentMeleeCooldown <= 0)
            {
                CurrentMeleeCooldown = MeleeCooldown;

                RiflePivot.GetComponentInChildren<Animator>().SetFloat("MeleeSpeed", 0.5f / MeleeCooldown);
                RiflePivot.GetComponentInChildren<Animator>().SetBool("IsSlashing", true);
                RiflePivot.GetComponentInChildren<Animator>().Play("RifleSlash");

                Collider[] hits = Physics.OverlapSphere(transform.position + CalculateAimingDirection() * 1.25f, 1.5f, EnemyLayer);
                foreach (Collider hit in hits)
                {
                    if (hit.TryGetComponent(out EnemyController enemy))
                        enemy.TakeDamage(MeleeDamage, stats.CritChance, stats.CritDamage);   // enemies: with crits
                    else if (hit.TryGetComponent(out IDamageable target))
                        target.TakeDamage(MeleeDamage);                                     // anything else hittable, e.g. the core
                }
                
            }
        }
        else if (!RightClick.action.IsPressed())
        {
            if (CurrentMeleeCooldown < 0) CurrentMeleeCooldown = 0;
            RiflePivot.GetComponentInChildren<Animator>().SetBool("IsSlashing", false);
        }

        if (LeftClick.action.IsPressed())
        {
            if (CurrentFirearmCooldown <= 0)
            {
                CurrentFirearmCooldown = FirearmCooldown;
                for(int i = 0; i < BulletAmount; i++) Invoke(nameof(ShootGun), i * 0.08f);


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

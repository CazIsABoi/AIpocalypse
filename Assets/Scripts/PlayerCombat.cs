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

    [SerializeField] private GameObject RiflePivot;

    [SerializeField] private float MeleeBaseCooldown = 0.5f;   // Will be 0.5
    [SerializeField] private float MeleeCooldown;
    [SerializeField] private float CurrentMeleeCooldown;
    [SerializeField] private List<GameObject> hitEnemies = new List<GameObject>();

    public float MeleeDamage;
    public float CritChance = 0.1f;        // 0 -> 1, aka 0 -> 100%, 10% by default
    public float CritDamage = 2f;          // Multiplies Damage Upright, 2f is 2x damage, 3f is 3x damage

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
        if (CurrentMeleeCooldown > 0) CurrentMeleeCooldown -= Time.deltaTime;

        if (RightClick.action.IsPressed())
        {
            Debug.Log("Holding Button");
            if (CurrentMeleeCooldown <= 0)
            {
                CurrentMeleeCooldown = MeleeCooldown;

                RiflePivot.GetComponentInChildren<Animator>().SetFloat("MeleeSpeed", 0.5f / MeleeCooldown);
                RiflePivot.GetComponentInChildren<Animator>().SetBool("IsSlashing", true);
                RiflePivot.GetComponentInChildren<Animator>().Play("RifleSlash");

                Collider[] hits = Physics.OverlapSphere(transform.position + CalculateAimingDirection() * 1.25f, 1.5f, EnemyLayer);
                foreach (Collider hit in hits)
                {
                    if (hit.gameObject.GetComponent<EnemyController>()) hit.gameObject.GetComponent<EnemyController>().TakeDamage(MeleeDamage, CritChance, CritDamage);
                }
            }
        }
        else if (!RightClick.action.IsPressed())
        {
            Debug.Log("Holding Button False");
            if (CurrentMeleeCooldown < 0) CurrentMeleeCooldown = 0;
            RiflePivot.GetComponentInChildren<Animator>().SetBool("IsSlashing", false);
        }
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

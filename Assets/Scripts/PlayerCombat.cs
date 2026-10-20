using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private InputActionReference Melee;
    [SerializeField] private InputActionReference LeftClick;
    [SerializeField] private InputActionReference RightClick;

    [SerializeField] private LayerMask EnemyLayer;
    private PlayerStats stats;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    // Update is called once per frame
    void Update()
    {

        //Debug.DrawLine(transform.position, GetMouseWorldPosition(), Color.red);
        Debug.DrawLine(transform.position, transform.position + CalculateAimingDirection() * 1.25f);

        if (Melee.action.WasPressedThisFrame())
        {
            Collider[] hits = Physics.OverlapSphere(transform.position + CalculateAimingDirection() * 1.25f, 1.5f, EnemyLayer);
            foreach (Collider hit in hits)
            {
                if (hit.TryGetComponent(out IDamageable target)) target.TakeDamage(stats.Damage);
            }
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

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionReference move;
    private CharacterController controller;
    private PlayerStats stats;
    [SerializeField] private float fallSpeed = 10f;   // constant downward push, keeps the player on the floor

    [SerializeField] private Animator animator;
    [SerializeField] private GameObject playerObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
    }
    private void Start()
    {
       // animator = GetComponentInChildren<Animator>();   
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = move.action.ReadValue<Vector2>();
        Vector3 moveDir = new Vector3(input.x, 0, input.y);

        Vector3 direction = CalculateAimingDirection();
        Quaternion rotation = Quaternion.LookRotation(direction);
        playerObj.transform.rotation = rotation;

        if (input == Vector2.zero) animator.SetBool("IsMoving", false);
        else animator.SetBool("IsMoving", true);

        Vector3 velocity = moveDir * stats.Speed + Vector3.down * fallSpeed;
        controller.Move(velocity * Time.deltaTime);
    }

    public void Teleport(Vector3 newPosition)
    {
        controller.enabled = false;
        transform.position = newPosition;
        controller.enabled = true;
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

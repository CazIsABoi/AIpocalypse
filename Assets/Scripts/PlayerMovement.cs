using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionReference move;
    private CharacterController controller;
    private PlayerStats stats;
    [SerializeField] private float fallSpeed = 10f;   // constant downward push, keeps the player on the floor

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
    }
    private void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = move.action.ReadValue<Vector2>();
        Vector3 moveDir = new Vector3(input.x, 0, input.y);

        Vector3 velocity = moveDir * stats.Speed + Vector3.down * fallSpeed;
        controller.Move(velocity * Time.deltaTime);
    }

    public void Teleport(Vector3 newPosition)
    {
        controller.enabled = false;
        transform.position = newPosition;
        controller.enabled = true;
    }
}

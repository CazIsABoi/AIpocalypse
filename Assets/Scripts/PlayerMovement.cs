using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionReference move;
    private CharacterController controller;
    [SerializeField] private float fallSpeed = 10f;   // constant downward push, keeps the player on the floor
    [SerializeField] private float BaseSpeed;
    [SerializeField] private float Speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Speed = BaseSpeed;
    }
    private void Start()
    {
        Speed = BaseSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = move.action.ReadValue<Vector2>();
        Vector3 moveDir = new Vector3(input.x, 0, input.y);

        Vector3 velocity = moveDir * Speed + Vector3.down * fallSpeed;
        controller.Move(velocity * Time.deltaTime);
    }

    public void Teleport(Vector3 newPosition)
    {
        controller.enabled = false;
        transform.position = newPosition;
        controller.enabled = true;
    }
}

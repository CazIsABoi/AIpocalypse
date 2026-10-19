using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionReference move;
    private CharacterController controller;
    [SerializeField] private float BaseSpeed;
    [SerializeField] private float Speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Speed = BaseSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDir = new Vector3(move.action.ReadValue<Vector2>().x, 0, move.action.ReadValue<Vector2>().y);
        controller.Move(moveDir * Time.deltaTime * Speed);
    }

    public void Teleport(Vector3 newPosition)
    {
        controller.enabled = false;
        transform.position = newPosition;
        controller.enabled = true;
    }
}

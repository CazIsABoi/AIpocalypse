using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private Transform head;                         // empty child at eye height
    [SerializeField] private InputActionReference moveAction;        // Vector2: WASD composite, left stick
    [SerializeField] private InputActionReference lookAction;        // Vector2: mouse delta
    [SerializeField] private InputActionReference cursorAction;      // Button: Escape, leaves a screen or frees the cursor
    [SerializeField] private InputActionReference interactAction;    // Button: left mouse, uses the screen you're looking at

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private GameObject crosshair;                   // only shown while walking with the mouse locked

    [Header("Screens")]
    [SerializeField] private float maxReach = 20f;                    // how far the look ray goes; each FocusScreen has its own reach
    [SerializeField] private float focusTime = 0.6f;
    [SerializeField] private AnimationCurve focusCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public Transform Head => head;

    enum State { Walking, Moving, Focused }
    private State state;
    private CharacterController body;
    private Transform cam;
    private float pitch, fallSpeed;
    private bool pausedFree, lockedBeforePause;

    void Awake()
    {
        body = GetComponent<CharacterController>();
        ShowCrosshair(false);   // Awake runs even while this component starts disabled
    }

    void OnEnable()
    {
        cam = Camera.main.transform;
        moveAction.action.Enable();
        lookAction.action.Enable();
        cursorAction.action.Enable();
        interactAction.action.Enable();
        state = State.Walking;
        pausedFree = false;
        SetLocked(true);
    }

    void OnDisable()
    {
        StopAllCoroutines();   // disabling a component doesn't stop its coroutines by itself
        SetLocked(false);
        ShowCrosshair(false);
        pitch = 0f;
        head.localRotation = Quaternion.identity;
    }

    void Update()
    {
        // settings open (GamePause): free the cursor, then lock it again on close if we were walking with it locked
        if (GamePause.IsPaused)
        {
            if (!pausedFree)
            {
                pausedFree = true;
                lockedBeforePause = Cursor.lockState == CursorLockMode.Locked;
                SetLocked(false);
            }
            return;
        }
        if (pausedFree)
        {
            pausedFree = false;
            if (lockedBeforePause && state == State.Walking) SetLocked(true);
        }

        if (state == State.Moving) return;

        if (state == State.Focused)
        {
            if (cursorAction.action.WasPressedThisFrame()) Leave();
            return;
        }

        if (cursorAction.action.WasPressedThisFrame()) SetLocked(Cursor.lockState != CursorLockMode.Locked);
        bool active = Cursor.lockState == CursorLockMode.Locked;

        // click a screen you're looking at (and close enough to) to sit down at it
        if (active && interactAction.action.WasPressedThisFrame()
            && Physics.Raycast(cam.position, cam.forward, out var hit, maxReach, ~0, QueryTriggerInteraction.Collide))
        {
            var screen = hit.collider.GetComponentInParent<FocusScreen>();
            if (screen != null && hit.distance <= screen.Reach)
            {
                StartCoroutine(Focus(screen.FocusPoint));
                return;
            }
        }

        if (active)
        {
            Vector2 d = lookAction.action.ReadValue<Vector2>() * lookSensitivity;
            transform.Rotate(0f, d.x, 0f);
            pitch = Mathf.Clamp(pitch - d.y, -maxPitch, maxPitch);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector2 input = active ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Vector3 move = Vector3.ClampMagnitude(transform.right * input.x + transform.forward * input.y, 1f) * moveSpeed;

        fallSpeed = body.isGrounded ? -1f : fallSpeed + gravity * Time.deltaTime;
        move.y = fallSpeed;
        body.Move(move * Time.deltaTime);
    }

    // only follow the head while walking: during a glide or at a screen, the coroutine owns the camera
    void LateUpdate()
    {
        if (state == State.Walking) cam.SetPositionAndRotation(head.position, head.rotation);
        ShowCrosshair(state == State.Walking && Cursor.lockState == CursorLockMode.Locked);
    }

    void ShowCrosshair(bool show)
    {
        if (crosshair != null && crosshair.activeSelf != show) crosshair.SetActive(show);
    }

    IEnumerator Focus(Transform point)
    {
        state = State.Moving;
        yield return CameraMove.To(cam, point, focusTime, focusCurve);
        SetLocked(false);   // free mouse for the menu once we've arrived
        state = State.Focused;
    }

    IEnumerator Unfocus()
    {
        state = State.Moving;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);   // new
        SetLocked(true);
        yield return CameraMove.To(cam, head, focusTime, focusCurve);
        state = State.Walking;
    }

    void SetLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    public void Leave()
    {
        if (state == State.Focused) StartCoroutine(Unfocus());
    }
}
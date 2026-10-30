using UnityEngine;
using UnityEngine.InputSystem;
public class CameraFocus : MonoBehaviour
{
    [SerializeField] private InputActionReference focusAction;   // e.g. Z, right mouse button or a gamepad shoulder
    [SerializeField] private Transform focusPoint;               // an empty object in front of the wall: where to zoom to
    [SerializeField] private bool holdToFocus = false;           // false = press to toggle, true = only while held
    [SerializeField] private float smoothing = 6f;               // higher = snappier
    [SerializeField] private CanvasGroup[] hideWhenFocused;      // e.g. the credits/config/terminate buttons: fade out while zoomed in
    // bool to stop the camera from moving while another script is moving it (e.g. MenuFlow's MoveCamera coroutine). The other script should set this to true while it's moving the camera, and false when done.
    public bool StopMoving;

    private Vector3 restPosition;
    private Quaternion restRotation;
    private bool focused;

    void Awake()
    {
        restPosition = transform.position;
        restRotation = transform.rotation;
        StopMoving = false;
    }

    void OnEnable() { if (focusAction != null) focusAction.action.Enable(); }   // actions only fire while enabled

    void Update()
    {
        if (focusAction != null && focusPoint != null)
        {
            if (holdToFocus) focused = focusAction.action.IsPressed();
            else if (focusAction.action.WasPressedThisFrame()) focused = !focused;
        }

        if (StopMoving) return;   // another script is moving the camera, so don't override it

        Vector3 targetPosition = focused ? focusPoint.position : restPosition;
        Quaternion targetRotation = focused ? focusPoint.rotation : restRotation;

        // ease toward the target: 1 - Exp(-speed * dt) feels the same at any frame rate (like RewardCard's hover)
        float k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, k);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, k);

        // the side buttons fade with the same easing; while hidden they can't be clicked by accident
        foreach (var group in hideWhenFocused)
        {
            if (group == null) continue;
            group.alpha = Mathf.Lerp(group.alpha, focused ? 0f : 1f, k);
            group.interactable = group.blocksRaycasts = !focused;
        }
    }

    // Buttons can call these too (e.g. a magnifier icon next to the tree)
    public void Toggle() => focused = !focused;
    public void Unfocus() => focused = false;

    public void SetRestPoint(Transform point)
    {
        restPosition = point.position;
        restRotation = point.rotation;
    }
}

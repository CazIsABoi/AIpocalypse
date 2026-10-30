using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;


[AutoStaticsCleanup]
public static partial class Aim
{
    const float deadzone = 0.3f;
    static Vector3 lastStick;

    public static Vector3 Direction(Vector3 from)
    {
        if (MenuFocus.UsingGamepad && Gamepad.current != null)
        {
            Vector2 aim = Gamepad.current.rightStick.ReadValue();
            if (aim.sqrMagnitude > deadzone * deadzone) lastStick = new Vector3(aim.x, 0, aim.y).normalized;   // same axes as Move
            if (lastStick != Vector3.zero) return lastStick;

            Vector2 walk = Gamepad.current.leftStick.ReadValue();
            return walk.sqrMagnitude > deadzone * deadzone ? new Vector3(walk.x, 0, walk.y).normalized : Vector3.forward;
        }

        Vector3 target = MouseOnGround();
        Vector3 dir = new Vector3(target.x - from.x, 0, target.z - from.z);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;   // LookRotation(zero) logs a warning
    }

    // Where to throw/place something: the mouse on the ground, pulled in to maxRange if it's further.
    // Gamepad has no cursor, so it throws the full range in the aim direction. Keeps from's height.
    public static Vector3 Point(Vector3 from, float maxRange)
    {
        Vector3 offset = Direction(from) * maxRange;
        if (!(MenuFocus.UsingGamepad && Gamepad.current != null))
        {
            Vector3 mouse = MouseOnGround();
            offset = Vector3.ClampMagnitude(new Vector3(mouse.x - from.x, 0, mouse.z - from.z), maxRange);
        }
        return from + offset;
    }

    static Vector3 MouseOnGround()
    {
        if (Mouse.current == null || Camera.main == null) return Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        return groundPlane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
    }
}

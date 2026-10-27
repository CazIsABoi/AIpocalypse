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

    static Vector3 MouseOnGround()
    {
        if (Mouse.current == null || Camera.main == null) return Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        return groundPlane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
    }
}

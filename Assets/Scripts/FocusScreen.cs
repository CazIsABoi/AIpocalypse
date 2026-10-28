using UnityEngine;

public class FocusScreen : MonoBehaviour
{
    [SerializeField] private Transform focusPoint;   // where the camera sits to use this screen
    [SerializeField] private float reach = 3f;       // how close you must be to use it: big screens far away need more
    public Transform FocusPoint => focusPoint;
    public float Reach => reach;
}

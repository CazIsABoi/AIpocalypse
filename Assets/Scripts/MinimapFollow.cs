using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [SerializeField] private Transform target;      
    [SerializeField] private float height = 50f;    

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 newPosition = target.position;
            newPosition.y = height;
            transform.position = newPosition;
        }
    }
}

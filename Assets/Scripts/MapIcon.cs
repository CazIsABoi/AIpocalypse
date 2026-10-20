using UnityEngine;

public class MapIcon : MonoBehaviour
{
    [SerializeField] private float worldHeight = 2.8f;

    void LateUpdate()
    {
        Vector3 newPosition = transform.position;
        transform.position = new Vector3(newPosition.x, worldHeight, newPosition.z);
    }
}

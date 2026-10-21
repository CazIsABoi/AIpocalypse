using UnityEngine;

public class MapIcon : MonoBehaviour
{
    [SerializeField] private float worldHeight = 2.8f;
    public float WorldHeight { get => worldHeight; set => worldHeight = value; }   // above fogHeight = visible through fog

    void LateUpdate()
    {
        Vector3 newPosition = transform.position;
        transform.position = new Vector3(newPosition.x, worldHeight, newPosition.z);
    }
}

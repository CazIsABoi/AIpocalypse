using UnityEngine;

public class BarrierParticle : MonoBehaviour
{
    public Vector3 TargetPosition;
    [SerializeField] private float Lifetime = 3f;

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, TargetPosition, 10 * Time.deltaTime);
        Lifetime -= Time.deltaTime;
        if(Lifetime < 0) Destroy(gameObject);
    }
}

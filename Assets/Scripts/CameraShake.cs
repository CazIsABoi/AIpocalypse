using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[AutoStaticsCleanup]
partial class CameraShake : MonoBehaviour      
{
    public static CameraShake Instance;
    [SerializeField] float maxOffset = 0.4f;   // world units at full trauma
    [SerializeField] float decay = 1.5f;       // trauma lost per second
    float trauma;
    Vector3 basePos;

    void Awake() { Instance = this; basePos = transform.localPosition; }

    public void Add(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    void LateUpdate()
    {
        if (Time.timeScale == 0f) { transform.localPosition = basePos; return; }
        float shake = trauma * trauma;
        Vector3 dir = Random.insideUnitSphere;
        dir.y = 0f;                 
        transform.localPosition = basePos + dir * shake * maxOffset;
        trauma = Mathf.MoveTowards(trauma, 0, decay * Time.deltaTime);
    }
}
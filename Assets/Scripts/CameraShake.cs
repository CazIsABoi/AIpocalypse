using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[AutoStaticsCleanup]
partial class CameraShake : MonoBehaviour      
{
    public static CameraShake Instance;
    [SerializeField] float maxOffset = 0.4f;   // world units at full trauma
    [SerializeField] float decay = 1.5f;       // trauma lost per second
    float trauma;
    float addedThisFrame;   // the biggest Add this frame
    Vector3 basePos;

    void Awake() { Instance = this; basePos = transform.localPosition; }

    // Many hits in the same frame (a swing through 8 enemies, a chain of explosions) count as ONE shake,
    // the strongest of them, instead of adding up to full trauma.
    public void Add(float amount)
    {
        if (amount <= addedThisFrame) return;
        trauma = Mathf.Clamp01(trauma + amount - addedThisFrame);   // only the part above what this frame already added
        addedThisFrame = amount;
    }

    void LateUpdate()
    {
        addedThisFrame = 0f;
        if (Time.timeScale == 0f) { transform.localPosition = basePos; return; }
        float shake = trauma * trauma * GameSettings.Shake;   // accessibility slider: 0 = none
        Vector3 dir = Random.insideUnitSphere;
        dir.y = 0f;                 
        transform.localPosition = basePos + dir * shake * maxOffset;
        trauma = Mathf.MoveTowards(trauma, 0, decay * Time.deltaTime);
    }
}
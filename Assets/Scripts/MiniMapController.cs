using UnityEngine;
using UnityEngine.InputSystem;

public class MinimapController : MonoBehaviour
{
    [SerializeField] private InputActionReference toggleMap, toggleMapWebGL;   
    [SerializeField] private Camera minimapCam;
    [SerializeField] private GameObject smallMap, bigMap;
    [SerializeField] private float smallSize = 30f, bigSize = 120f;
    private bool isBig;

    void Update()
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL // WebGL build
        if (toggleMapWebGL.action.triggered) ToggleMap();
#endif

#if !UNITY_WEBPLAYER && !UNITY_WEBGL // Non-WebGL build
        if (toggleMap.action.triggered) ToggleMap();
#endif

    }

    private void ToggleMap()
    {
        isBig = !isBig;
        smallMap.SetActive(!isBig);
        bigMap.SetActive(isBig);
        minimapCam.orthographicSize = isBig ? bigSize : smallSize;
    }

    public bool IsBig => isBig;  
}
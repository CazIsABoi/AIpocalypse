using UnityEngine;
using UnityEngine.InputSystem;

public class MinimapController : MonoBehaviour
{
    [SerializeField] private InputActionReference toggleMap;   
    [SerializeField] private Camera minimapCam;
    [SerializeField] private GameObject smallMap, bigMap;
    [SerializeField] private float smallSize = 30f, bigSize = 120f;
    private bool isBig;

    void Update()
    {
        if (toggleMap.action.triggered)
        {
            isBig = !isBig;
            smallMap.SetActive(!isBig);
            bigMap.SetActive(isBig);
            minimapCam.orthographicSize = isBig ? bigSize : smallSize;
        }
    }

    public bool IsBig => isBig;  
}
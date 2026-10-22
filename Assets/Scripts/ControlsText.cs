using UnityEngine;

public class ControlsText : MonoBehaviour
{
    private TMPro.TMP_Text text;
    private void Awake()
    {
        text = GetComponent<TMPro.TMP_Text>();
#if UNITY_WEBGL
    text.text = "WASD move · LMB shoot · RMB melee · M map · Tab pause";
#else
        text.text = "WASD move · LMB shoot · RMB melee · Tab map · Esc pause";
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

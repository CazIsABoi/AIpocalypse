using UnityEngine;

// All selectable player masks. One asset, shared by the menu selector and the in-game PlayerMask.
[CreateAssetMenu(menuName = "Player/Mask Set")]
public class MaskSet : ScriptableObject
{
    [System.Serializable]
    public struct Mask
    {
        public string name;          // shown in the selector, e.g. "GLITCH_RED"
        public Material material;    // goes on the mask renderer
    }

    public Mask[] masks;

    public int Count => masks != null ? masks.Length : 0;

    // Safe lookup: a saved index from an older build (masks removed since) falls back to the first mask
    public Mask Get(int index) => masks[Count > 0 ? Mathf.Clamp(index, 0, Count - 1) : 0];
}

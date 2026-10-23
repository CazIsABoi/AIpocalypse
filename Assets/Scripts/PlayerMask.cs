using UnityEngine;

// On the Player: puts the mask chosen in the menu onto the mask renderer(s).
// Also used by the menu preview model, so both always look the same.
public class PlayerMask : MonoBehaviour
{
    [SerializeField] private MaskSet maskSet;
    [SerializeField] private Renderer[] targets;          // the mask mesh(es)
    [SerializeField] private int materialSlot = 0;        // which material slot is the mask (-1 = every slot)

    void Awake() => Show(GameSettings.MaskIndex);

    public void Show(int index)
    {
        if (maskSet == null || maskSet.Count == 0) return;
        Material mat = maskSet.Get(index).material;
        if (mat == null) return;

        foreach (Renderer r in targets)
        {
            if (r == null) continue;
            Material[] mats = r.sharedMaterials;           // copy, change, assign back (arrays aren't live)
            for (int i = 0; i < mats.Length; i++)
                if (materialSlot < 0 || i == materialSlot) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }
}

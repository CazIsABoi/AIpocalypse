using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu step between "> initialize_" and the intro: pick a mask with the arrows, then confirm.
// The preview is a PlayerMask on a model that a separate camera renders into a RawImage (RenderTexture).
public class MaskSelectUI : MonoBehaviour
{
    [SerializeField] private MaskSet maskSet;
    [SerializeField] private GameObject panel;           // the whole selector, hidden until "initialize" is pressed
    [SerializeField] private TMP_Text nameText;          // "// mask: GLITCH_RED"
    [SerializeField] private TMP_Text counterText;       // optional "2 / 5"
    [SerializeField] private PlayerMask preview;         // the preview model
    [SerializeField] private Transform spinTarget;       // what rotates in the preview (usually the preview model)
    [SerializeField] private float spinSpeed = 30f;      // degrees per second
    [SerializeField] private string nextScene = "Intro";

    private int index;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        index = Wrap(GameSettings.MaskIndex);
        Refresh();
    }

    void Update()
    {
        if (spinTarget != null && (panel == null || panel.activeInHierarchy))
            spinTarget.Rotate(0f, spinSpeed * Time.unscaledDeltaTime, 0f, Space.World);
    }

    // Hook "> initialize_" to this instead of StartGame
    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close() { if (panel != null) panel.SetActive(false); }   // optional back button

    public void Next() { index = Wrap(index + 1); Refresh(); }           // ArrowRight
    public void Previous() { index = Wrap(index - 1); Refresh(); }       // ArrowLeft

    public void Apply() => GameSettings.MaskIndex = index;   // the Run Picker saves the mask without loading a scene

    public void Confirm()
    {
        Apply();
        SceneManager.LoadScene(nextScene);
    }

    // % alone gives negatives for -1, so add Count first: (−1 + 5) % 5 = 4 → wraps to the last mask
    int Wrap(int i) => maskSet == null || maskSet.Count == 0 ? 0 : (i % maskSet.Count + maskSet.Count) % maskSet.Count;

    void Refresh()
    {
        if (maskSet == null || maskSet.Count == 0) return;
        if (nameText != null) nameText.text = $"// mask: {maskSet.Get(index).name}";
        if (counterText != null) counterText.text = $"{index + 1} / {maskSet.Count}";
        if (preview != null) preview.Show(index);
    }
}

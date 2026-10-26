using System.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[AutoStaticsCleanup]
partial class MenuFlow : MonoBehaviour
{
    // Set by the death screen's Return button, so the menu opens on the picker instead of the title
    public static bool OpenPickerOnLoad;

    const int TitleSlide = 0, PickerSlide = 1, TreeSlide = 2;   // slide N sits N slide heights down the strip

    [SerializeField] private RunPicker picker;
    [SerializeField] private RectTransform strip;
    [SerializeField] private float slideHeight = 1080f;  // the Projection's height
    [SerializeField] private CanvasGroup projection;     // flickers and dims during a change
    [SerializeField] private string introScene = "Intro";
    [SerializeField] private InputActionReference startAction;   // optional: your "any key" action (keyboard/mouse/gamepad already work without it)

    [Header("Slide change")]
    [SerializeField] private float slideTime = 0.45f;
    [SerializeField] private AnimationCurve slideCurve = new AnimationCurve(   // fast push, small overshoot, settles: a mechanical "clunk"
        new Keyframe(0f, 0f, 0f, 2.5f), new Keyframe(0.75f, 1.04f), new Keyframe(1f, 1f));
    [SerializeField, Range(0f, 1f)] private float dimDuringChange = 0.35f;     // how dark the light gets mid-change
    [SerializeField] private float flicker = 0.25f;

    [Header("Sound (the Menu has no AudioManager)")]
    [SerializeField] private AudioClip slideClip;        // the clack
    [SerializeField] private AudioMixerGroup output;     // SFX group, so the volume slider applies
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private AudioSource source;
    private int current;
    private bool changing;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = output;
    }

    void OnEnable() { if (startAction != null) startAction.action.Enable(); }   // actions only fire while enabled

    void Start()
    {
        if (OpenPickerOnLoad)
        {
            OpenPickerOnLoad = false;   // only this once: the next normal menu load starts on the title again
            picker.Open();
            Snap(PickerSlide);
        }
        else Snap(TitleSlide);
    }

    void Update()
    {
        if (current != TitleSlide || changing) return;
        // the title also has Settings, Credits, Quit and the name field: using those must not start a run
        var events = UnityEngine.EventSystems.EventSystem.current;
        bool typing = events != null && events.currentSelectedGameObject != null
                   && events.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null;

        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !OverButton(events);
        bool pressed = clicked
                    || (!typing && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                    || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                    || (!typing && startAction != null && startAction.action.WasPressedThisFrame());
        if (pressed) Begin();
    }

    // Is the mouse on something clickable (a button, a toggle, a text field)?
    // The projection itself is UI too, so "over any UI" would block every click on the title.
    private readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    bool OverButton(UnityEngine.EventSystems.EventSystem events)
    {
        if (events == null) return false;
        var pointer = new UnityEngine.EventSystems.PointerEventData(events) { position = Mouse.current.position.ReadValue() };
        hits.Clear();
        events.RaycastAll(pointer, hits);
        foreach (var hit in hits)
            if (hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return true;
        return false;
    }

    void Begin()
    {
        if (!GameSettings.IntroSeen)
        {
            GameSettings.IntroSeen = true;
            SceneManager.LoadScene(introScene);
            return;
        }
        picker.Open();   // fill the slide first, so it's ready as it slides into view
        StartCoroutine(ChangeSlide(PickerSlide));
    }

    // Button on the picker slide
    public void Back()
    {
        if (!changing) StartCoroutine(ChangeSlide(TitleSlide));
    }

    // Button on the picker slide: the perk tree has its own slide, one further down the strip
    public void OpenTree()
    {
        if (!changing) StartCoroutine(ChangeSlide(TreeSlide));
    }

    // Button on the tree slide
    public void BackToPicker()
    {
        if (!changing) StartCoroutine(ChangeSlide(PickerSlide));
    }

    void Snap(int slide)
    {
        current = slide;
        strip.anchoredPosition = new Vector2(strip.anchoredPosition.x, slide * slideHeight);
        if (projection != null) projection.alpha = 1f;
    }

    // Moving the strip UP by one slide height shows the next slide; down goes back
    IEnumerator ChangeSlide(int slide)
    {
        changing = true;
        if (slideClip != null) source.PlayOneShot(slideClip, volume);

        float from = strip.anchoredPosition.y, to = slide * slideHeight;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / slideTime)
        {
            float y = Mathf.LerpUnclamped(from, to, slideCurve.Evaluate(t));
            strip.anchoredPosition = new Vector2(strip.anchoredPosition.x, y);

            // the light dips in the middle of the change and flickers, like the bulb shaking in the old machine
            if (projection != null)
            {
                float dip = Mathf.Sin(t * Mathf.PI) * dimDuringChange;
                projection.alpha = Mathf.Clamp01(1f - dip - Random.value * flicker * Mathf.Sin(t * Mathf.PI));
            }
            yield return null;
        }

        Snap(slide);   // land exactly: the loop's last step can stop just short of the target
        changing = false;
    }
}

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

    const int PickerSlide = 0, TreeSlide = 1;   // slide N sits N slide heights down the strip

    [SerializeField] private RunPicker picker;
    [SerializeField] private RectTransform strip;
    [SerializeField] private float slideHeight = 1080f;  // the Projection's height
    [SerializeField] private CanvasGroup projection;     // flickers and dims during a change
    [SerializeField] private string introScene = "Intro";
    [SerializeField] private InputActionReference startAction;
    [SerializeField] private FirstPersonController player;

    [Header("Press any key to start")]
    [SerializeField] private Transform overWorldPoint;
    [SerializeField] private Transform bunkerPoint;
    [SerializeField] private float cameraMoveTime = 0.75f;
    [SerializeField] private AnimationCurve cameraCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float timeBeforeInputAllowed = 0.5f;
    private float inputTimer;
    private Coroutine moveCamera;

    [Header("Bunker entry")]
    [SerializeField] private Transform divePoint;
    [SerializeField] private Transform bunkerCeilingPoint;
    [SerializeField] private Animator hatch;
    [SerializeField] private string hatchTrigger = "Open";
    [SerializeField] private CanvasGroup blackout;
    [SerializeField] private float diveTime = 1.5f;
    [SerializeField, Range(0f, 1f)] private float openHatchAt = 0.6f;
    [SerializeField] private float fadeTime = 0.25f;
    [SerializeField] private float blackTime = 0.4f;
    [SerializeField] private float descendTime = 1.5f;
    bool CanDive => divePoint != null && bunkerCeilingPoint != null && blackout != null;
    private bool started;

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
            OpenPickerOnLoad = false;
            started = true;
            picker.Open();
            player.enabled = true;
        }
        else
        {
            player.enabled = false;   
            Camera.main.transform.SetPositionAndRotation(overWorldPoint.position, overWorldPoint.rotation);
        }
        Snap(PickerSlide);
        inputTimer = timeBeforeInputAllowed;
        if (blackout != null)
        {
            blackout.alpha = 0f;
            blackout.blocksRaycasts = false;
        }
        if (hatch != null) hatch.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    void Update()
    {
        if (started || moveCamera != null) return;
        // the title also has Settings, Credits, Quit and the name field: using those must not start a run
        var events = UnityEngine.EventSystems.EventSystem.current;
        bool typing = events != null && events.currentSelectedGameObject != null
                   && events.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null;

        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !OverButton(events);
        bool pressed = clicked
                    || (!typing && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                    || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                    || (!typing && startAction != null && startAction.action.WasPressedThisFrame());
        inputTimer -= Time.unscaledDeltaTime;
        if (inputTimer <= 0f && pressed) Begin();
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
        started = true;
        picker.Open();
        if (CanDive) moveCamera = StartCoroutine(EnterBunker());
        else Move(player.Head, cameraMoveTime, () => player.enabled = true);
    }

    IEnumerator EnterBunker()
    {
        var cam = Camera.main.transform;
        if (hatch != null) StartCoroutine(OpenHatchAfter(diveTime * openHatchAt));
        yield return CameraMove.To(cam, divePoint, diveTime, cameraCurve);
        yield return Fade(1f);
        cam.SetPositionAndRotation(bunkerCeilingPoint.position, bunkerCeilingPoint.rotation);
        yield return new WaitForSecondsRealtime(blackTime);
        StartCoroutine(Fade(0f));
        yield return CameraMove.To(cam, player.Head, descendTime, cameraCurve);
        moveCamera = null;
        player.enabled = true;
    }

    IEnumerator LeaveBunker()
    {
        var cam = Camera.main.transform;
        yield return Fade(1f);
        cam.SetPositionAndRotation(overWorldPoint.position, overWorldPoint.rotation);
        CloseHatch();
        yield return new WaitForSecondsRealtime(blackTime);
        yield return Fade(0f);
        moveCamera = null;
    }

    IEnumerator OpenHatchAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        hatch.SetTrigger(hatchTrigger);
    }

    void CloseHatch()
    {
        if (hatch == null) return;
        hatch.ResetTrigger(hatchTrigger);
        hatch.Rebind();
        hatch.Update(0f);
    }

    IEnumerator Fade(float to)
    {
        float from = blackout.alpha;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / fadeTime)
        {
            blackout.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        blackout.alpha = to;
    }

    // Button on the picker slide
    public void Back()
    {
        if (changing || moveCamera != null) return;
        started = false;
        inputTimer = timeBeforeInputAllowed;
        player.enabled = false;   // hand the camera back to MenuFlow, frees the cursor
        if (CanDive) moveCamera = StartCoroutine(LeaveBunker());
        else Move(overWorldPoint, cameraMoveTime, null);
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
    void PlaceCamera(Transform point)
    {
        Camera.main.transform.SetPositionAndRotation(point.position, point.rotation);
        FindAnyObjectByType<CameraFocus>().SetRestPoint(point);
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

    private IEnumerator MoveCamera(Transform to, float time, System.Action done)
    {
        yield return CameraMove.To(Camera.main.transform, to, time, cameraCurve);
        moveCamera = null;
        done?.Invoke();
    }

    private void Move(Transform to, float time, System.Action done)
    {
        if (moveCamera != null) return;
        moveCamera = StartCoroutine(MoveCamera(to, time, done));
    }
}
public static class CameraMove
{

    public static IEnumerator To(Transform cam, Transform to, float time, AnimationCurve curve)
    {
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / time)
        {
            float k = curve.Evaluate(t);
            cam.SetPositionAndRotation(Vector3.Lerp(startPos, to.position, k), Quaternion.Slerp(startRot, to.rotation, k));
            yield return null;
        }
        cam.SetPositionAndRotation(to.position, to.rotation);
    }
}
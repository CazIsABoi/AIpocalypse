using System.Collections;
using TMPro;
using UnityEngine;

// First-floor help: a controls hint at the start, a hint when you first get near the Core,
// and an arrow circling the player that points toward the Core.
public class TutorialHints : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameController game;
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private TMP_Text hintText;          // a HUD text (Raycast Target off)
    [SerializeField] private RectTransform arrow;        // a UI Image with your ArrowRight sprite (points right = 0°)

    [Header("Texts")]
    [SerializeField] private string startHint = "Hold LMB to shoot or Hold RMB to melee";
    [SerializeField] private string coreHint = "Hit the AI Core with your melee to destroy it";

    [Header("Timing")]
    [SerializeField] private float startDelay = 1f;       // after the floor is ready
    [SerializeField] private float showTime = 5f;
    [SerializeField] private float fadeTime = 0.4f;

    [Header("Core")]
    [SerializeField] private float coreHintDistance = 9f; // how close before the Core hint shows
    [SerializeField] private int arrowUntilFloor = 1;     // arrow only on floors up to this one

    [Header("Arrow")]
    [SerializeField] private float arrowRadius = 110f;    // pixels from the player's screen position
    [SerializeField] private float arrowHideDistance = 6f;// hide when this close to the Core
    [SerializeField] private float playerHeight = 1f;     // aim from the body, not the feet

    private Transform player;
    private Core core;
    private bool startShown, coreShown;
    private float coreSearchTimer;
    private Coroutine showing;

    void Start()
    {
        player = GameObject.Find("Player").transform;
        if (hintText != null) hintText.alpha = 0f;
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null || !dungeon.IsReady) return;

        // the Core is placed when a floor generates, so look it up again when we don't have one
        if (core == null)
        {
            coreSearchTimer -= Time.unscaledDeltaTime;
            if (coreSearchTimer <= 0f) { coreSearchTimer = 0.5f; core = FindAnyObjectByType<Core>(); }
        }

        if (!startShown && game.Floor == 1)
        {
            startShown = true;
            Show(startHint, startDelay);
        }

        if (!coreShown && core != null && FlatDistance(core.transform.position) < coreHintDistance)
        {
            coreShown = true;
            Show(coreHint, 0f);
        }

        UpdateArrow();
    }

    void UpdateArrow()
    {
        if (arrow == null) return;
        bool show = game.Floor <= arrowUntilFloor && core != null && !GamePause.IsPaused
                    && FlatDistance(core.transform.position) > arrowHideDistance;
        if (arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
        if (!show) return;

        Camera cam = Camera.main;
        Vector3 from = cam.WorldToScreenPoint(player.position + Vector3.up * playerHeight);
        Vector3 to = cam.WorldToScreenPoint(core.transform.position);
        Vector2 dir = ((Vector2)(to - from)).normalized;

        // sit on a circle around the player, turned toward the Core (Screen Space Overlay canvas: position = pixels)
        arrow.position = (Vector2)from + dir * arrowRadius;
        arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    float FlatDistance(Vector3 p)
    {
        Vector3 d = p - player.position;
        d.y = 0f;
        return d.magnitude;
    }

    public void Show(string message, float delay)
    {
        if (hintText == null) return;
        if (showing != null) StopCoroutine(showing);
        showing = StartCoroutine(ShowRoutine(message, delay));
    }

    // unscaled time: hints keep fading even while a level-up or the floor fade has the game paused
    IEnumerator ShowRoutine(string message, float delay)
    {
        for (float d = 0; d < delay; d += Time.unscaledDeltaTime) yield return null;
        hintText.text = message;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / fadeTime) { hintText.alpha = t; yield return null; }
        hintText.alpha = 1f;
        for (float d = 0; d < showTime; d += Time.unscaledDeltaTime) yield return null;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / fadeTime) { hintText.alpha = 1f - t; yield return null; }
        hintText.alpha = 0f;
        showing = null;
    }
}

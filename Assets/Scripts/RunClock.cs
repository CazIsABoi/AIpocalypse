using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RunClock : MonoBehaviour
{
    [SerializeField] private int bossFloor = 20;          
    public float Elapsed { get; private set; }
    public float FloorTime { get; private set; }          
    public bool BossActive { get; private set; }          
    public bool Endless { get; private set; }
    public float EndlessTime { get; private set; }
    public event System.Action BossTime;                  // telemetry listens to it
    public void StartEndless() { Endless = true; }       // call this when the boss dies to start the endless timer


    [SerializeField] private PlayerMovement player;
    [SerializeField] private InputActionReference TEMP;

    [SerializeField] private EventDirector controller;
    [SerializeField] private Image cogIcon;
    [SerializeField] private Slider xpBarSlider;
    [SerializeField] private GameObject EventHUD;
    [SerializeField] private GameObject OverallTimer;
    GameController game;

    private void Start()
    {
        player = GameObject.Find("Player").GetComponent<PlayerMovement>();
        controller = FindAnyObjectByType<EventDirector>();
    }
    void OnEnable() { game = FindAnyObjectByType<GameController>(); game.FloorChanged += ResetFloorTime; }
    void OnDisable() { if (game != null) game.FloorChanged -= ResetFloorTime; }
    void ResetFloorTime(int floor) => FloorTime = 0f;

    void Update()
    {
        float delta = Time.deltaTime;
        Elapsed += delta;
        FloorTime += delta;

        if (Endless) EndlessTime += delta;  

#if UNITY_EDITOR
        // debug only: skip to the boss (never in a build)
        if (TEMP != null && TEMP.action.WasPressedThisFrame())
            bossFloor = FindAnyObjectByType<GameController>().Floor;   // skip to the next floor, which is the boss floor
#endif
    }
    [SerializeField] private CutScenePlayer outro;   // plays when the boss dies (Play On Start off)

    // Boss died: outro first (if there is one), then continue the run
    public void BossDefeated()
    {
        if (outro != null) outro.Play(ContinueAfterBoss);
        else ContinueAfterBoss();
    }

    // Everything that happens after the boss: UI back, next floor, endless mode
    public void ContinueAfterBoss()
    {
        RevertBossUIChanges();
        StartEndless();
        BossActive = false;
        FindAnyObjectByType<GameController>().NextFloor();
        AudioManager.Instance?.Play(Sfx.OvertimeStart);
    }

    public void RevertBossUIChanges()
    {
        controller.ResumeEvents();
        Camera.main.fieldOfView = 60;
        xpBarSlider.fillRect.GetComponent<Image>().color = new Color(237/255f, 241f/255f, 90f/255);
        xpBarSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(750, 150);
        cogIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 90);
        EventHUD.SetActive(true);       // back on for endless (the timer shows "ENDLESS")
        OverallTimer.SetActive(true);
    }
    public void StartBoss()
    {
        controller.StopEvents();
        Camera.main.fieldOfView = 80;
        xpBarSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(750, 150);
        cogIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 90);
        EventHUD.SetActive(false);
        OverallTimer.SetActive(false);

        AudioManager.Instance?.Play(Sfx.BossSpawn);
        player.Teleport(new Vector3(-500f, 2, -13.5f));
    }

    public bool TryStartBoss(int floor)
    {
        if (floor == bossFloor && !Endless) {
            BossActive = true;
            BossTime?.Invoke();
            StartBoss();
            return true;
        }
        return false;
    }
}
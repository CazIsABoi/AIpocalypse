using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RunClock : MonoBehaviour
{
    [SerializeField] private float bossTime = 900f;   
    public float Elapsed { get; private set; }
    public float TimeLeft => Mathf.Max(0f, bossTime - Elapsed);
    public float Progress => Elapsed / bossTime;       
    public event System.Action BossTime;               
    private bool bossFired;
    public bool Endless { get; private set; }            
    public float EndlessTime { get; private set; }       // seconds since the boss died
    public void StartEndless() { Endless = true; }       // call this when the boss dies to start the endless timer


    [SerializeField] private PlayerMovement player;
    [SerializeField] private InputActionReference TEMP;

    [SerializeField] private EventDirector controller;
    [SerializeField] private Image cogIcon;
    [SerializeField] private Slider xpBarSlider;
    [SerializeField] private GameObject EventHUD;
    [SerializeField] private GameObject OverallTimer;

    private void Start()
    {
        player = GameObject.Find("Player").GetComponent<PlayerMovement>();
        controller = FindAnyObjectByType<EventDirector>();
    }
    void Update()
    {
        Elapsed += Time.deltaTime;
        if (TimeLeft <= 0f && !bossFired)
        {
            bossFired = true;
            BossTime?.Invoke();   // for anything else that wants to react to the boss
            StartBoss();
        }
        if (Endless)
        {
            EndlessTime += Time.deltaTime;
        }

#if UNITY_EDITOR
        // debug only: skip to the boss (never in a build)
        if (TEMP != null && TEMP.action.WasPressedThisFrame())
            Elapsed = Mathf.Max(Elapsed, bossTime);
#endif
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

}
using System.Collections;
using UnityEngine;

public class EventDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityHandler spawner;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private DungeonGenerator dungeon;
    [SerializeField] private LevelUpUI rewards;
    [SerializeField] private Transform player;
    private EventContext ctx;
    private Coroutine loop;

    [Header("Events")]
    [SerializeField] private GameEvent[] events;
    [SerializeField] private float minWaitTime = 60f;
    [SerializeField] private float maxWaitTime = 90f;
    [SerializeField] private float textShowDuration;
    private bool firstEvent = true;
    private GameEvent current;
    public float NextEventIn { get; private set; }     
    public float EventTimeLeft { get; private set; }   
    public GameEvent Current => current;
    public bool Paused { get; private set; }

    private void Awake()
    {
        ctx = new EventContext { Spawner = spawner, Stats = stats, Dungeon = dungeon, Rewards = rewards, Player = player,
                                 Movement = player.GetComponent<PlayerMovement>() };
    }
    public void OnFloorChanged(int floor)
    {
        StopAllCoroutines();                 // stops the loop AND the running event
        if (current != null) { current.End(ctx); Destroy(current); current = null; }
        loop = null;                    
    }

    IEnumerator RunEvent(GameEvent e)
    {
        current = e;
        e.Begin(ctx);
        if (!e.Started) 
        {
            e.End(ctx);
            Destroy(e);
            current = null;
            yield break;
        }
        AudioManager.Instance?.Play(Sfx.EventStart);
        if (e.Duration > 0) {
            float t = 0f;
            while (t < e.Duration)
            {
                e.Tick(ctx);
                t += Time.deltaTime;
                EventTimeLeft = e.TimeLeft(t);
                yield return null;       // next frame
            }
        }
        else
        {
            while (!e.IsDone(ctx))
            {
                e.Tick(ctx);
                EventTimeLeft = e.TimeLeft(0f);   // events without a Duration report their own time (Bounty)
                yield return null;       // next frame
            }
        }
        e.End(ctx);
        AudioManager.Instance?.Play(Sfx.EventEnd);
        EventTimeLeft = 0f;
        Destroy(e);
        current = null;
    }
    IEnumerator DirectorLoop()
    {
        while (true)
        {
            Debug.Log("EventDirector waiting for next event...");
            if (firstEvent)
            {
                firstEvent = false;
                NextEventIn = Random.Range(minWaitTime / 2, maxWaitTime / 2);
            }
            else
            {
                NextEventIn = Random.Range(minWaitTime, maxWaitTime);
            }
            while (NextEventIn > 0f) { NextEventIn -= Time.deltaTime; yield return null; }

            Debug.Log("EventDirector starting next event...");
            GameEvent picked = Pick();
            if (picked == null) continue;
            Debug.Log($"EventDirector picked event: {picked.Title}");
            yield return StartCoroutine(RunEvent(Instantiate(picked)));
        }
    }

    public void StartLoop()
    {
        if (Paused) return;
        if (loop == null) loop = StartCoroutine(DirectorLoop());
        else Debug.LogWarning("EventDirector loop already running.");
    }

    private GameEvent Pick()
    {
        if (events.Length == 0) return null;
        int totalWeight = 0;
        foreach (var e in events) totalWeight += e.Weight;
        int choice = Random.Range(0, totalWeight);
        foreach (var e in events)
        {
            choice -= e.Weight;
            if (choice < 0) return e;
        }
        return events[0];
    }
    public void StopEvents()         
    {
        Paused = true;
        StopAllCoroutines();         
        if (current != null) { current.End(ctx); Destroy(current); current = null; }
        loop = null;
        NextEventIn = 0f; EventTimeLeft = 0f;
    }

    public void ResumeEvents()     
    {
        Paused = false;
        StartLoop();
    }
}

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
    [SerializeField] private TMPro.TextMeshProUGUI bannerText;
    private EventContext ctx;
    private Coroutine loop;

    [Header("Events")]
    [SerializeField] private GameEvent[] events;
    [SerializeField] private float minWaitTime = 60f;
    [SerializeField] private float maxWaitTime = 90f;
    [SerializeField] private float textShowDuration;
    private bool firstEvent = true;
    private GameEvent current;

    private void Awake()
    {
        ctx = new EventContext { Spawner = spawner, Stats = stats, Dungeon = dungeon, Rewards = rewards, Player = player };
    }
    public void OnFloorChanged(int floor)
    {
        StopAllCoroutines();                 // stops the loop AND the running event
        if (current != null) { current.End(ctx); Destroy(current); current = null; }
        bannerText.gameObject.SetActive(false);
        loop = null;                    
    }

    IEnumerator RunEvent(GameEvent e)
    {
        current = e;
        e.Begin(ctx);
        yield return StartCoroutine(ShowBanner(e.Title, textShowDuration));
        if (e.Duration > 0) {
            float t = 0f;
            while (t < e.Duration)
            {
                e.Tick(ctx);
                t += Time.deltaTime;
                yield return null;       // next frame
            }
        }
        else
        {
            while (!e.IsDone(ctx))
            {
                e.Tick(ctx);
                yield return null;       // next frame
            }
        }
        e.End(ctx);
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
                yield return new WaitForSeconds(Random.Range(minWaitTime / 2, maxWaitTime / 2)); // first event happens sooner
            }
            else yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
            Debug.Log("EventDirector starting next event...");
            GameEvent picked = Pick();
            if (picked == null) continue;
            Debug.Log($"EventDirector picked event: {picked.Title}");
            yield return StartCoroutine(RunEvent(Instantiate(picked)));
        }
    }

    IEnumerator ShowBanner(string text, float duration)
    {
        bannerText.text = text;
        bannerText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        bannerText.gameObject.SetActive(false);
    }

    public void StartLoop()
    {
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
}

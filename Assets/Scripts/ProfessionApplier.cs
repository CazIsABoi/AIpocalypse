using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// On the Player in MainScene. At the start of a run it puts the chosen profession's active perk nodes on the
// player (like cards picked before the first enemy); when the player dies it gives that profession XP.
[AutoStaticsCleanup]
public partial class ProfessionApplier : MonoBehaviour
{
    [SerializeField] private ProfessionSet professions;   // Data/Professions/AllProfessions

    [Header("XP per run")]
    [SerializeField] private float xpPerMinute = 3f;
    [SerializeField] private float xpPerKill = 0.02f;
    [SerializeField] private float xpPerFloor = 15f;
    [SerializeField] private float runXpCap = 350f;   // diminishing returns: one run can never give more than this

    public static ProfessionData Current { get; private set; }   // the profession of THIS run 

    PlayerStats stats;
    PlayerEffects effects;
    PlayerHealth health;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
        effects = GetComponent<PlayerEffects>();
        if (effects == null) effects = gameObject.AddComponent<PlayerEffects>();
    }

    void OnEnable() { if (health != null) health.Died += OnDied; }
    void OnDisable() { if (health != null) health.Died -= OnDied; }

    void Start()
    {
        // the profession picked in the menu; the very first run has none yet, so go by the mask
        if (professions.ById(RunProfession.ProfessionId) != null)
        {
            Current = professions.ById(RunProfession.ProfessionId);
        }
        else
        {
            Current = professions.ForMask(GameSettings.MaskIndex);
        }

        if (Current == null) return;   // no tree made for this mask yet

        // same as applying a trade-off card, plus an optional effect level
        var activeNodes = RunProfession.ActiveNodes(Current);
        foreach (var node in activeNodes)
        {
            foreach (var change in node.Changes)
            {
                stats.Add(change.Stat, change.Amount);
            }
            if (node.GivesEffect)
            {
                effects.Add(node.Effect, node.EffectLevels);
            }
        }
    }

    // Telemetry sends on the death screen, which can open before OnDied runs: it awards first so the XP is in the row
    public static void AwardNow() => FindAnyObjectByType<ProfessionApplier>()?.OnDied();

    bool awarded;

    void OnDied()
    {
        if (Current == null || awarded) return;
        awarded = true;
        float minutes = FindAnyObjectByType<RunClock>().Elapsed / 60f;   // float: a 2.5-minute run counts as 2.5, not 2 or 3
        int kills = FindAnyObjectByType<EntityHandler>().Kills;
        int floor = FindAnyObjectByType<GameController>().Floor;
        float raw = minutes * xpPerMinute + kills * xpPerKill + floor * xpPerFloor;
        // Diminishing returns: almost 1:1 for short runs, then flattening toward runXpCap.
        // With 350: 4 min / floor 5 (~90 raw) -> ~78, 25 min / floor 20 (~415) -> ~245, a long endless run -> ~350
        int xp = Mathf.RoundToInt(runXpCap * (1f - Mathf.Exp(-raw / runXpCap)));
        int levelsGained = ProfessionProgress.AddXp(Current, xp);
        RunTelemetry.Instance?.ProfessionXp(xp);
    }
}

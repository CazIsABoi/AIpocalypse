using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// On the Player in MainScene. At the start of a run it puts the chosen profession's active perk nodes on the
// player (like cards picked before the first enemy); when the player dies it gives that profession XP.
[AutoStaticsCleanup]
public partial class ProfessionApplier : MonoBehaviour
{
    [SerializeField] private ProfessionSet professions;   // Data/Professions/AllProfessions

    [Header("XP per run")]
    [SerializeField] private float xpPerMinute = 10f;
    [SerializeField] private float xpPerKill = 0.2f;
    [SerializeField] private float xpPerFloor = 5f;

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

    void OnDied()
    {
        if (Current == null) return;
        // a 2-minute run (25 kills, floor 1) is ~30 XP, a 15-minute run ~270 XP
        float minutes = FindAnyObjectByType<RunClock>().Elapsed / 60f;   // float: a 2.5-minute run counts as 2.5, not 2 or 3
        int kills = FindAnyObjectByType<EntityHandler>().Kills;
        int floor = FindAnyObjectByType<GameController>().Floor;
        int xp = Mathf.RoundToInt(minutes * xpPerMinute + kills * xpPerKill + floor * xpPerFloor);
        int levelsGained = ProfessionProgress.AddXp(Current, xp);
        Debug.Log($"Run XP: {xp}, Levels Gained: {levelsGained}");
    }
}

using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// Saved by number in the Inspector: only ever add new values at the END
public enum Sfx
{
    Shoot,            // LazerGun_SingleShot
    Melee,            // Melee (swing)
    EnemyHit,         // EnemyHit
    EnemyDeath,       // EnemyDeath_MAYBE
    CoreHit,          // Metal_hitting
    XpPickup,         // Pickup
    LevelUp,          // placeholder: HealthPickup        → needs a sound
    CardPick,         // Reload
    EventStart,       // Alarm
    PlayerHurt,       // Sword_on_steel
    PlayerDeath,      // Death

    // --- combat ---
    MeleeHit,         // swing connects with an enemy (separate from the swing)  → needs a sound
    CritHit,          // extra "punch" layered on a crit                          → needs a sound
    Heal,             // lifesteal / regen tick (long min interval)  HealthPickup

    // --- enemies ---
    EnemyAttack,      // an enemy hits / lunges at the player                     → needs a sound
    EnemySpawn,       // optional, quiet, when a wave arrives                     → needs a sound

    // --- core & floors ---
    CoreDestroyed,    // core goes down                                           → needs a sound
    FloorComplete,    // fade to the next floor                                   → needs a sound
    ChestOpen,        // treasure chest looted                                    → needs a sound

    // --- events ---
    EventEnd,         // event over (Lockdown open, Swarm calms down)            → needs a sound
    DataLeak,         // Robotic_Glitching
    LockdownBarrier,  // barriers slam into the doors                            → needs a sound
    BountySpawn,      // marked enemy appears                                     → needs a sound
    BountyKilled,     // bounty reward                                            → needs a sound

    // --- hazards ---
    HazardWarn,       // plate about to turn on                                   → needs a sound
    HazardZap,        // plate is live / hits something                           → needs a sound

    // --- player state ---
    LowHealth,        // warning beep when HP drops low (plays once per drop)     → needs a sound

    // --- UI ---
    CardHover,        // mouse over a reward card                                 → needs a sound
    RareCardReveal,   // extra sound for Epic / Legendary cards (later: explosion) → needs a sound
    ButtonClick,      // menus, settings                                          → needs a sound
    ButtonHover,      // menus, settings                                          → needs a sound

    // --- run timer & boss ---
    TimerWarning,     // last minute before the boss (e.g. a tick each second)    → needs a sound
    BossWarning,      // boss is about to arrive                                  → needs a sound
    BossSpawn,        // boss appears                                             → needs a sound
    BossDeath,        // boss killed                                              → needs a sound
    OvertimeStart,    // endless mode begins                                      → needs a sound

    // --- reward cards (added at the END on purpose: enums are saved as numbers) ---
    CardFlip,         // card starts flipping over                                → needs a sound
    CardFlyIn,        // each card sliding up into place (plays 3x, keep it short) → needs a sound
    CardReveal,       // front shows (Common/Uncommon/Rare; Epic+ use RareCardReveal) → needs a sound
}

[AutoStaticsCleanup]
partial class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [SerializeField] private UnityEngine.Audio.AudioMixer mixer;

    [System.Serializable]
    public class Entry
    {
        public Sfx id;
        public AudioClip[] clips;                 // several = pick one at random
        [Range(0f, 1f)] public float volume = 1f;
        public float pitchVariance = 0.05f;       // ±5%, stops "machine gun" repetition
        public float minInterval = 0.05f;         // spam protection
        [HideInInspector] public float lastPlayed = -999f;
    }

    [SerializeField] private AudioSource source;  // 2D, Output = SFX group
    [SerializeField] private Entry[] sounds;

    [SerializeField] private AudioSource source3DPrefab;   // Spatial Blend 1, Output = SFX, Play On Awake off
    [SerializeField] private int poolSize = 16;
    private AudioSource[] pool;
    private int next;

    bool Muted(Sfx id) =>
(!GameSettings.GunSounds && id == Sfx.Shoot) ||
(!GameSettings.HitSounds && (id == Sfx.EnemyHit || id == Sfx.CritHit || id == Sfx.EnemyDeath));

    void Awake() 
    { 
        Instance = this; 
        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var src = Instantiate(source3DPrefab, transform);
            pool[i] = src;
        }
    }
    private void Start()
    {
        GameSettings.ApplyAudio(mixer);
        Screen.fullScreen = GameSettings.Fullscreen != 0;
    }

    public void Play(Sfx id)
    {
        if (!TryGetClip(id, out Entry entry, out AudioClip clip)) return;
        source.pitch = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);
        source.PlayOneShot(clip, entry.volume);
    }

    public void PlayAt(Sfx id, Vector3 position)
    {
        if (!TryGetClip(id, out Entry entry, out AudioClip clip)) return;
        AudioSource s = pool[next];
        next = (next + 1) % poolSize;
        s.transform.position = position;
        s.pitch = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);
        s.PlayOneShot(clip, entry.volume);
    }

    bool TryGetClip(Sfx id, out Entry entry, out AudioClip clip)
    {
        entry = null; clip = null;
        if (Muted(id)) return false;
        foreach (var e in sounds)
        {
            if (e.id != id) continue;
            if (e.clips.Length == 0) return false;                                   // no sound yet
            if (Time.unscaledTime - e.lastPlayed < e.minInterval) return false;      // spam protection
            entry = e;
            clip = e.clips[Random.Range(0, e.clips.Length)];
            if (clip == null) return false;                                          
            e.lastPlayed = Time.unscaledTime;
            return true;
        }
        return false;                                                                // no entry for this id
    }
}
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
    LevelUp,          // HealthPickup
    CardPick,         // Reload
    EventStart,       // Alarm
    PlayerHurt,       // Sword_on_steel
    PlayerDeath,      // Death

    // --- combat ---
    MeleeHit,         // swing connects with an enemy (separate from the swing)
    CritHit,          // extra "punch" layered on a crit
    Heal,             // lifesteal / regen tick (long min interval)  HealthPickup

    // --- enemies ---
    EnemyAttack,      // an enemy hits / lunges at the player
    EnemySpawn,       // optional, quiet, when a wave arrives

    // --- core & floors ---
    CoreDestroyed,    // core goes down
    FloorComplete,    // fade to the next floor
    ChestOpen,        // treasure chest looted

    // --- events ---
    EventEnd,         // event over (Lockdown open, Swarm calms down)
    DataLeak,         // Robotic_Glitching
    LockdownBarrier,  // barriers slam into the doors
    BountySpawn,      // marked enemy appears
    BountyKilled,     // bounty reward

    // --- hazards ---
    HazardWarn,       // plate about to turn on
    HazardZap,        // plate is live / hits something

    // --- player state ---
    LowHealth,        // warning beep when HP drops low (plays once per drop)

    // --- UI ---
    CardHover,        // mouse over a reward card
    RareCardReveal,   // extra sound for Epic / Legendary cards (later: explosion)
    ButtonClick,      // menus, settings
    ButtonHover,      // menus, settings                                          → needs a sound (no entry yet)

    // --- run timer & boss ---
    TimerWarning,     // last minute before the boss (e.g. a tick each second)
    BossWarning,      // boss is about to arrive
    BossSpawn,        // boss appears
    BossDeath,        // boss killed
    OvertimeStart,    // endless mode begins

    // --- reward cards (added at the END on purpose: enums are saved as numbers) ---
    CardFlip,         // card starts flipping over
    CardFlyIn,        // each card sliding up into place (plays 3x, keep it short)
    CardReveal,       // front shows (Common/Uncommon/Rare; Epic+ use RareCardReveal)
    DroneDeath,       // drone explodes (instead of EnemyDeath)
    RareCardFlyIn,    // a Rare+ card slides in (a hint before the flip; RareCardReveal plays on the flip)

    // --- Effects ---
    Shockwave,         // ShockwaveSwingEffect (long min interval)                 → needs a sound (no entry yet)

    // --- meta-progression & pickups ---
    ThoughtTick,       // Thoughts counting up on the death screen (short, plays often)
    HealthPickup,      // health orb collected

    // --- class abilities ---
    AbilityReady,      // cooldown finished, the ability can be used again           → needs a sound
    GrenadeThrow,      // Frag Grenade thrown                                         → needs a sound
    GrenadeExplode,    // Frag Grenade lands and explodes (3D, at the blast)          → needs a sound
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

    private AudioSource steadySource;
    [SerializeField] private Entry[] sounds;

    [SerializeField] private AudioSource source3DPrefab;   // Spatial Blend 1, Output = SFX, Play On Awake off
    [SerializeField] private int poolSize = 16;
    private AudioSource[] pool;
    private int next;

    bool Muted(Sfx id) =>
(!GameSettings.GunSounds && id == Sfx.Shoot) ||
(!GameSettings.HitSounds && (id == Sfx.EnemyHit || id == Sfx.CritHit || id == Sfx.EnemyDeath || id == Sfx.DroneDeath));

    void Awake() 
    { 
        Instance = this; 
        // Second 2D source for sounds without pitch variance (long reveals, stingers), so a later
        // pitch-varied sound on 'source' can't bend them while they're still playing
        steadySource = gameObject.AddComponent<AudioSource>();
        steadySource.playOnAwake = false;
        steadySource.outputAudioMixerGroup = source.outputAudioMixerGroup;
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
        GameSettings.ApplyDisplay();
    }

    public void Play(Sfx id)
    {
        if (!TryGetClip(id, out Entry entry, out AudioClip clip)) return;
        if (entry.pitchVariance <= 0f) { steadySource.PlayOneShot(clip, entry.volume); return; }
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
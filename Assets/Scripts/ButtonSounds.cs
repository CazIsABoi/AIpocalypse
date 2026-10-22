using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Put on a Canvas: every Button under it plays a click (reward cards excluded, they have their own sounds).
// Uses the AudioManager when the scene has one, otherwise plays the clip itself (the Menu has no AudioManager).
public class ButtonSounds : MonoBehaviour
{
    [SerializeField] private AudioClip fallbackClip;        // ButtonClick.wav, used when there's no AudioManager
    [SerializeField] private AudioMixerGroup fallbackOutput; // the SFX group, so the volume sliders still apply
    [SerializeField, Range(0f, 1f)] private float fallbackVolume = 0.8f;
    private AudioSource source;

    void Awake()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))   // true = inactive panels too
        {
            if (button.GetComponent<RewardCard>() != null) continue;
            button.onClick.AddListener(Click);
        }
    }

    void Click()
    {
        if (AudioManager.Instance != null) { AudioManager.Instance.Play(Sfx.ButtonClick); return; }
        if (fallbackClip == null) return;
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = fallbackOutput;
        }
        source.PlayOneShot(fallbackClip, fallbackVolume);
    }
}

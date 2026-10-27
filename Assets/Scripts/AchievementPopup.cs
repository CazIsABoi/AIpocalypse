using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UI;

[AutoStaticsCleanup]
public partial class AchievementPopup : MonoBehaviour
{
    static AchievementPopup instance;

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text title;           // "Up Close"
    [SerializeField] private TMP_Text description;     // "Destroy 1000 AIs with melee" (+ the reward line)
    [SerializeField] private Image icon;               // optional
    [SerializeField] private float showTime = 3.5f;
    [SerializeField] private float fadeTime = 0.3f;
    [SerializeField] private float slide = 40f;        // pixels it slides in from below

    readonly Queue<AchievementData> queue = new Queue<AchievementData>();
    RectTransform rect;
    Vector2 restPos;
    bool showing;

    void Awake()
    {
        instance = this;
        rect = (RectTransform)transform;
        restPos = rect.anchoredPosition;
        group.alpha = 0f;
        group.blocksRaycasts = false;   // never in the way of clicks
    }

    public static void Show(AchievementData a)
    {
        if (instance == null) { Debug.Log($"Achievement unlocked: {a.Title}"); return; }
        instance.queue.Enqueue(a);
        if (!instance.showing) instance.StartCoroutine(instance.Play());
    }

    IEnumerator Play()
    {
        showing = true;
        while (queue.Count > 0)
        {
            var a = queue.Dequeue();
            title.text = a.Title;
            description.text = a.Reward != null ? $"{a.Description}\n<color={StatNames.Good}>Idea unlocked: {a.Reward.Title}</color>" : a.Description;
            if (icon != null) { icon.sprite = a.Icon; icon.enabled = a.Icon != null; }
            AudioManager.Instance?.Play(Sfx.BountyKilled);  

            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(showTime);
            yield return Fade(1f, 0f);
        }
        showing = false;
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            float k = t / fadeTime;
            group.alpha = Mathf.Lerp(from, to, k);
            rect.anchoredPosition = restPos + Vector2.down * slide * (1f - group.alpha);   // rises in, sinks out
            yield return null;
        }
        group.alpha = to;
        rect.anchoredPosition = restPos + Vector2.down * slide * (1f - to);
    }
}

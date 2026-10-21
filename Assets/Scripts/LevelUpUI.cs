using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private RewardPool levelUpPool;
    [SerializeField] private GameObject panel;
    [SerializeField] private RewardCard[] cards;      // drag the 3 cards in
    [SerializeField] private RectTransform cardRow;   // the parent of the cards (shaken on Legendary)
    [SerializeField] private UnityEngine.UI.Image flash;   // full-screen white, last child of the panel, raycast off

    [Header("Juice")]
    [SerializeField] private float flyStagger = 0.08f;   // delay between each card flying in
    [SerializeField] private float lingerTime = 0.5f;    // how long the picked card stays before closing
    [SerializeField] private float rowShakeTime = 0.3f;
    [SerializeField] private float flashTime = 0.25f;
    [SerializeField] private float flashCooldown = 0.5f; // never flash rapidly (photosensitivity)

    private int pendingLevelUps;
    private bool isOpen;
    private bool showingLevelUp;                      // level-up or chest?
    private bool choosing;                            // picked card is lingering, ignore more picks
    private Vector2 rowBasePos;
    private Coroutine rowShake;
    private Coroutine flashRoutine;
    private float lastFlash = -10f;

    void Awake() { if (cardRow != null) rowBasePos = cardRow.anchoredPosition; }

    void OnEnable()
    {
        stats.LeveledUp += OnLeveledUp;
        foreach (var card in cards) card.Revealed += OnCardRevealed;
    }
    void OnDisable()
    {
        stats.LeveledUp -= OnLeveledUp;
        foreach (var card in cards) card.Revealed -= OnCardRevealed;
    }

    void OnLeveledUp(int newLevel)
    {
        pendingLevelUps++;
        if (isOpen) return;
        AudioManager.Instance.Play(Sfx.LevelUp);
        showingLevelUp = true;
        ShowChoices(levelUpPool);
    }

    public void ShowChoices(RewardPool pool)
    {
        if (health.IsDead) return;                // XP picked up while dying: no cards on the game-over screen
        List<RewardData> choices = PickRewards(pool, cards.Length);
        panel.SetActive(true);                    // first: coroutines can't start on inactive cards
        StopRowShake();
        StopFlash();

        for (int i = 0; i < cards.Length; i++)
        {
            if (i < choices.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(choices[i], Choose, stats);
                cards[i].FlyIn(i * flyStagger);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
        isOpen = true;
        GamePause.Add(PauseReason.LevelUp);
    }

    public bool ShowChest(RewardPool chestPool)
    {
        if (isOpen) return false;          
        showingLevelUp = false;
        ShowChoices(chestPool);
        return true;
    }

    void Choose(RewardData reward)
    {
        if (choosing) return;
        StartCoroutine(ChooseRoutine(reward));
    }

    IEnumerator ChooseRoutine(RewardData reward)
    {
        choosing = true;
        foreach (var card in cards)
        {
            if (!card.gameObject.activeSelf) continue;
            card.Lock();                                   // no second pick during the linger
            if (card.Reward == reward) card.PlayChosen();
            else card.PlayDismissed();
        }
        AudioManager.Instance?.Play(Sfx.CardPick);
        yield return new WaitForSecondsRealtime(lingerTime);   // realtime: the game is still paused
        choosing = false;

        reward.Apply(stats);
        if (showingLevelUp) pendingLevelUps--;
        if (pendingLevelUps > 0)
        {
            showingLevelUp = true;
            ShowChoices(levelUpPool); // next level
        }
        else
        {
            Close();
        }
    }

    void OnCardRevealed(RaritySettings.Style style)
    {
        if (style.flashAlpha > 0f && flash != null && Time.unscaledTime - lastFlash >= flashCooldown)
        {
            lastFlash = Time.unscaledTime;
            StopFlash();
            flashRoutine = StartCoroutine(Flash(Mathf.Min(style.flashAlpha, 0.8f)));
        }

        if (style.rowShake <= 0f || cardRow == null) return;
        StopRowShake();
        rowShake = StartCoroutine(ShakeRow(style.rowShake));
    }

    // Instant white, then fade out. Fading IN would feel slow; the hit should be immediate.
    IEnumerator Flash(float alpha)
    {
        Color c = Color.white;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / flashTime)
        {
            c.a = Mathf.Lerp(alpha, 0f, t);
            flash.color = c;
            yield return null;
        }
        c.a = 0f;
        flash.color = c;
        flashRoutine = null;
    }

    void StopFlash()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = null;
        if (flash != null) flash.color = new Color(1f, 1f, 1f, 0f);
    }

    IEnumerator ShakeRow(float strength)
    {
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / rowShakeTime)
        {
            cardRow.anchoredPosition = rowBasePos + Random.insideUnitCircle * strength * (1f - t);
            yield return null;
        }
        cardRow.anchoredPosition = rowBasePos;
        rowShake = null;
    }

    void StopRowShake()
    {
        if (rowShake != null) StopCoroutine(rowShake);
        rowShake = null;
        if (cardRow != null) cardRow.anchoredPosition = rowBasePos;
    }

    void Close()
    {
        panel.SetActive(false);
        isOpen = false;
        GamePause.Remove(PauseReason.LevelUp);   // stays paused if dead / pause menu etc.
    }

    public List<RewardData> PickRewards(RewardPool pool, int count)
    {
        List<RewardData> options = new List<RewardData>(pool.Rewards);
        List<RewardData> chosen = new List<RewardData>();

        for (int i = 0; i < count && options.Count > 0; i++)
        {
            int total = 0;
            foreach (var reward in options) total += reward.Weight;

            int roll = Random.Range(0, total);

            foreach (var reward in options)
            {
                roll -= reward.Weight;
                if (roll < 0)
                {
                    chosen.Add(reward);
                    options.Remove(reward);
                    break;
                }
            }
        }
        return chosen; 
    }
}
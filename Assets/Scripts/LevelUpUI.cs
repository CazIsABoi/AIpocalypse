using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMPro.TMP_Text sourceLabel;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private RewardPool levelUpPool;
    [SerializeField] private GameObject panel;
    [SerializeField] private RewardCard[] cards;      // drag the 3 cards in
    [SerializeField] private RectTransform cardRow;   // the parent of the cards (shaken on Legendary)
    [SerializeField] private UnityEngine.UI.Image flash;   // full-screen white, last child of the panel, raycast off

    // Rarity odds shift as the run goes on: early runs can't roll Legendaries, late runs lean Epic+.
    // Each value multiplies a card's own Weight for that rarity (0 = never, 1 = normal).
    [System.Serializable]
    public struct RarityTier
    {
        public float fromMinute;
        public float common, uncommon, rare, epic, legendary;
        public float Get(Rarity r) => r switch
        {
            Rarity.Common => common, Rarity.Uncommon => uncommon, Rarity.Rare => rare,
            Rarity.Epic => epic, Rarity.Legendary => legendary, _ => 1f
        };
    }

    [Header("Rarity over time")]
    [SerializeField] private RarityTier[] tiers =
    {
        new RarityTier { fromMinute = 0,  common = 1f,   uncommon = 1f,   rare = 0.6f, epic = 0.25f, legendary = 0f },
        new RarityTier { fromMinute = 5,  common = 0.8f, uncommon = 1f,   rare = 1f,   epic = 0.6f,  legendary = 0.25f },
        new RarityTier { fromMinute = 10, common = 0.5f, uncommon = 0.8f, rare = 1f,   epic = 1f,    legendary = 0.6f },
    };
    [SerializeField] private RarityTier endlessTier =
        new RarityTier { common = 0.25f, uncommon = 0.5f, rare = 1f, epic = 1.5f, legendary = 1.5f };   // after the boss
    private RunClock clock;

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

    void Awake()
    {
        if (cardRow != null) rowBasePos = cardRow.anchoredPosition;
        clock = FindAnyObjectByType<RunClock>();
    }

    RarityTier CurrentTier()
    {
        if (clock != null && clock.Endless) return endlessTier;
        float minutes = clock != null ? clock.Elapsed / 60f : 0f;
        RarityTier tier = tiers.Length > 0 ? tiers[0] : endlessTier;
        foreach (var t in tiers) if (minutes >= t.fromMinute) tier = t;   // last tier we've reached
        return tier;
    }

    float WeightOf(RewardData reward, RarityTier tier) => reward.Weight * tier.Get(reward.GetRarity());

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
        int shownLevel = stats.Level - pendingLevelUps + 1;
        sourceLabel.text = pool == levelUpPool ? $"LEVEL {shownLevel}" : pool.sourceText;

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
        // leave out cards that would do nothing right now (capped stats)
        List<RewardData> options = new List<RewardData>();
        foreach (var reward in pool.Rewards)
            if (reward.CanOffer(stats)) options.Add(reward);
        if (options.Count == 0) options.AddRange(pool.Rewards);   // never open an empty card screen
        List<RewardData> chosen = new List<RewardData>();
        RarityTier tier = CurrentTier();

        // if this tier zeroes out everything left (e.g. a Legendary-only pool early on), use plain weights
        float check = 0f;
        foreach (var reward in options) check += WeightOf(reward, tier);
        if (check <= 0f) tier = new RarityTier { common = 1, uncommon = 1, rare = 1, epic = 1, legendary = 1 };

        for (int i = 0; i < count && options.Count > 0; i++)
        {
            float total = 0f;
            foreach (var reward in options) total += WeightOf(reward, tier);
            if (total <= 0f) break;                     // only zero-weight cards left

            float roll = Random.Range(0f, total);

            foreach (var reward in options)
            {
                roll -= WeightOf(reward, tier);
                if (roll < 0f)
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
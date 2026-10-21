using System.Collections.Generic;
using UnityEngine;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private RewardPool levelUpPool;
    [SerializeField] private GameObject panel;
    [SerializeField] private RewardCard[] cards;      // drag the 3 cards in

    private int pendingLevelUps;
    private bool isOpen;
    private bool showingLevelUp;                      // level-up or chest?

    void OnEnable() { stats.LeveledUp += OnLeveledUp; }
    void OnDisable() { stats.LeveledUp -= OnLeveledUp; }

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
        List<RewardData> choices = PickRewards(pool, cards.Length);

        for (int i = 0; i < cards.Length; i++)
        {
            if (i < choices.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(choices[i], Choose, stats);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
        panel.SetActive(true);
        isOpen = true;
        Time.timeScale = 0;
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

    void Close()
    {
        panel.SetActive(false);
        isOpen = false;
        if (!health.IsDead) Time.timeScale = 1;
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
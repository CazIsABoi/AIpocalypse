using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunPicker : MonoBehaviour
{
    [SerializeField] private IdeaSet ideas;            // Data/Ideas/AllIdeas
    [SerializeField] private IdeaSlot slotPrefab;
    [SerializeField] private Transform listParent;     // e.g. an object with a Grid Layout Group
    [SerializeField] private TMP_Text slotsText;       // "IDEAS 2 / 3"
    [SerializeField] private MaskSelectUI masks;      
    [SerializeField] private string mainScene = "MainScene";

    private readonly List<IdeaSlot> slots = new List<IdeaSlot>();
    private readonly List<IdeaData> equipped = new List<IdeaData>();

    public void Open()
    {
       if (slots.Count < 0)
        {        
            foreach (var idea in ideas.Ideas)
            {
                var slot = Instantiate(slotPrefab, listParent);
                slots.Add(slot);
                bool unlocked = Progress.IsUnlocked(idea);
                bool isEquipped = RunLoadout.IsEquipped(idea) && unlocked;
                if (isEquipped)
                    equipped.Add(idea);
                slot.Setup(idea, unlocked, OnSlotClicked);
            }
        }
        foreach (var slot in slots)
        {
            bool isEquipped = equipped.Contains(slot.Idea);
            bool unlocked = Progress.IsUnlocked(slot.Idea);
            slot.SetEquipped(isEquipped);
        }
        Refresh();
    }

    void OnSlotClicked(IdeaSlot slot)
    {
        if (equipped.Contains(slot.Idea))
        {
            equipped.Remove(slot.Idea);
        }
        else if (equipped.Count < RunLoadout.MaxSlots)
        {
            equipped.Add(slot.Idea);
        }
        else
        {
            AudioManager.Instance?.Play(Sfx.CoreHit); // temp
        }
        Refresh();
    }

    void Refresh()
    {
        foreach (var slot in slots)
        {
            bool isEquipped = equipped.Contains(slot.Idea);
            slot.SetEquipped(isEquipped);
        }
        slotsText.text = $"IDEAS {equipped.Count} / {RunLoadout.MaxSlots}";
    }

    public void Surprise()
    {
        equipped.Clear();
        for (int i = 0; i < RunLoadout.MaxSlots; i++)
        {
            int rand = Random.Range(0, ideas.Ideas.Count);
            equipped.Add(ideas.Ideas[rand]);
        }
    }

    public void Clear()
    {
        equipped.Clear();
        Refresh();
    }

    public void StartRun()
    {
        masks.Apply();
        RunLoadout.Set(equipped);
        SceneManager.LoadScene(mainScene);
    }
}

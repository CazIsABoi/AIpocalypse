using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// The picker slide: choose up to RunLoadout.MaxSlots Ideas to bring, then start the run.
// (The mask is MaskSelectUI's, the perk tree is PerkTreeUI's; both sit on the same slide.)
public class RunPicker : MonoBehaviour
{
    [SerializeField] private IdeaSet ideas;            // Data/Ideas/AllIdeas
    [SerializeField] private IdeaSlot slotPrefab;
    [SerializeField] private Transform listParent;     // e.g. an object with a Grid Layout Group
    [SerializeField] private TMP_Text slotsText;       // "IDEAS 2 / 3"
    [SerializeField] private MaskSelectUI masks;
    [SerializeField] private UnityEngine.UI.Button startButton;   // greyed out while a locked mask (no profession yet) is selected
    [SerializeField] private string mainScene = "MainScene";

    void OnEnable() { if (masks != null) masks.Changed += OnMaskChanged; }
    void OnDisable() { if (masks != null) masks.Changed -= OnMaskChanged; }
    void OnMaskChanged(int index) { if (startButton != null) startButton.interactable = !masks.IsLocked; }

    private readonly List<IdeaSlot> slots = new List<IdeaSlot>();
    private readonly List<IdeaData> equipped = new List<IdeaData>();

    // Called by MenuFlow every time the picker slide is shown
    public void Open()
    {
        if (slots.Count == 0)   // build the list once; later visits only refresh it
        {
            // one slot per Idea, and start from last run's loadout (if those Ideas are still unlocked)
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
        Refresh();
    }

    // Equip / unequip; when all slots are full a click is refused with a sound
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

    // Button: fill the slots with random unlocked Ideas, never the same one twice
    public void Surprise()
    {
        equipped.Clear();
        List<IdeaData> unlocked = ideas.Ideas.FindAll(idea => idea != null && Progress.IsUnlocked(idea));   // a copy: removing from it never touches the asset
        while (equipped.Count < RunLoadout.MaxSlots && unlocked.Count > 0)
        {
            int rand = Random.Range(0, unlocked.Count);
            equipped.Add(unlocked[rand]);
            unlocked.RemoveAt(rand);   // can't be picked twice
        }
        Refresh();
    }

    // Button: a run with no Ideas, like the very first one
    public void Clear()
    {
        equipped.Clear();
        Refresh();
    }

    // Button: save the mask and loadout first (the new scene reads them as it starts), then load the run
    public void StartRun()
    {
        if (masks.IsLocked) return;   // the button is greyed out, but Enter/gamepad could still press it
        masks.Apply();
        RunLoadout.Set(equipped);
        SceneManager.LoadScene(mainScene);
    }
}

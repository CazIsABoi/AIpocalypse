using System.Collections.Generic;
using UnityEngine;

// An "Idea": a whole upgrade line (e.g. all lifesteal cards) the player recalls between runs.
// Recalling it unlocks every card in Cards. Those cards should have StartsUnlocked off.
[CreateAssetMenu(menuName = "Rewards/Idea")]
public class IdeaData : ScriptableObject
{
    public string Id;                   // stable save key: never change after release
    public string Title;
    [TextArea] public string Description;
    public Sprite Icon;
    public Rarity Rarity = Rarity.Rare; // only picks the card colours (RaritySettings)
    public List<RewardData> Cards;

    // On = a plain upgrade (stronger stats): once recalled its cards are in every run, no loadout slot needed.
    // Off = a build choice (effects, lifesteal, pierce...): only in the run when equipped in the Run Picker.
    public bool AlwaysInPool;

    // Optional: this Idea is only offered on recall once that one is recalled (e.g. Melee lifesteal needs Parasitism)
    public IdeaData Requires;

    // On = never offered on recall: an achievement hands it out instead (its AchievementData.Reward)
    public bool UnlockedByAchievement;

    // On = a slot Idea: no cards, recalling it adds one loadout slot. Offered (guaranteed, every recall until taken)
    // once this share of the other Ideas is recalled. Also tick AlwaysInPool so it's never equipped.
    public bool AddsSlot;
    [Range(0f, 1f)] public float OfferedAt = 0.25f;
}

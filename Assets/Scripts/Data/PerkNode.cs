using System.Collections.Generic;
using UnityEngine;

// One node in a profession's perk tree. Bought once with that profession's points (permanent), then applied
// at the start of every run where it's active, like a card. Tier = height in the tree (0 = the free root),
// Branch = 0 trunk / 1 left / 2 right... Majors on the same tier are exclusive: one is active per run.
[CreateAssetMenu(menuName = "Professions/Perk Node")]
public class PerkNode : ScriptableObject
{
    public string Id;                   // stable save key: never change after release
    public string Title;
    [TextArea] public string Description;
    public Sprite Icon;

    [Header("Place in the tree")]
    public int Tier;                    // 0 = root, counts up toward the capstones
    public int Branch;                  // 0 = trunk, 1 = left, 2 = right...
    public int Side;                    // 0 = on the branch, -1 / 1 = a small side perk sticking out left / right of it
    public bool IsMajor;                // majors on the same tier are exclusive: one per run
    public string BranchName;           // a branch's first major: the header above the branch ("MELEE"), empty = its Title
    public int Cost = 1;                // profession points to unlock it (the root should be 0)
    public List<PerkNode> Requires;     // must be unlocked AND active first (usually the node just below)

    [Header("What it does (applied at the start of a run, like a card)")]
    public TradeOffUpgrade.StatChange[] Changes;   // e.g. +0.1 MaxHealth... same as trade-off cards
    public bool GivesEffect;                       // tick to also give an on-hit effect
    public EffectType Effect;
    public int EffectLevels = 1;
    public AbilityUpgrade AbilityUpgrade;          // changes how the class ability works (branch majors: one per run)
}

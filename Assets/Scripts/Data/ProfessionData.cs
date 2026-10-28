using System.Collections.Generic;
using UnityEngine;

// A profession (class): Soldier, Chef... Picked through its mask in the Run Picker.
// Playing a run as this profession earns it XP; every level is one point for its perk tree.
[CreateAssetMenu(menuName = "Professions/Profession")]
public class ProfessionData : ScriptableObject
{
    public string Id;                   // stable save key: never change after release
    public string Title;                // "SOLDIER"
    [TextArea] public string Description;
    public int MaskIndex;               // which mask in MaskSet picks this profession
    public List<PerkNode> Nodes;        // the WHOLE tree, root included
    public ClassAbility Ability;        // the active on the ability button (none = no button this run)

    [Header("Levelling")]
    public int MaxLevel = 15;           // one point per level; keep it close to the tree's total cost
    public int XpPerLevel = 100;        // level 1 -> 2 costs this much, every level after costs a bit more
    public float XpGrowth = 1.15f;      // each level costs 15% more than the one before
}

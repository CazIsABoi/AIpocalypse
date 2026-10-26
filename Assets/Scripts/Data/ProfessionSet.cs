using System.Collections.Generic;
using UnityEngine;

// Every profession, one asset (like AllIdeas). The Run Picker and the run itself look professions up here.
[CreateAssetMenu(menuName = "Professions/Profession Set")]
public class ProfessionSet : ScriptableObject
{
    public List<ProfessionData> Professions;

    public ProfessionData ForMask(int maskIndex) => Professions.Find(p => p != null && p.MaskIndex == maskIndex);
    public ProfessionData ById(string id) => Professions.Find(p => p != null && p.Id == id);
}

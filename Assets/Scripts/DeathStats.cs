using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DeathStats : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private TMP_Text buildText;
    [SerializeField] private RunClock clock;
    [SerializeField] private GameController game;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private EntityHandler spawner;

    void OnEnable()
    {
        var shown = new HashSet<string>();
        string build = "";
        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))  
        {
            string name = StatNames.Get(type);
            if (!shown.Add(name)) continue;  
            build += $"{name}<pos=60%>{StatNames.Format(type, stats.Get(type))}\n";
        }
        buildText.text = build;

        int m = (int)(clock.Elapsed / 60), s = (int)(clock.Elapsed % 60);
        text.text = $"TIME SURVIVED  {m:00}:{s:00}\nFLOOR  {game.Floor}\nLEVEL  {stats.Level}\nKILLS  {spawner.Kills}";

        float best = PlayerPrefs.GetFloat("best_time", 0f);
        if (clock.Elapsed > best) { PlayerPrefs.SetFloat("best_time", clock.Elapsed); PlayerPrefs.Save(); text.text += "\nNEW BEST!"; }
    }
}
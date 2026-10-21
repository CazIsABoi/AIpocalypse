using UnityEngine;

public class RunClock : MonoBehaviour
{
    [SerializeField] private float bossTime = 900f;   
    public float Elapsed { get; private set; }
    public float TimeLeft => Mathf.Max(0f, bossTime - Elapsed);
    public float Progress => Elapsed / bossTime;       
    public event System.Action BossTime;               
    private bool bossFired;
    public bool Endless { get; private set; }            
    public float EndlessTime { get; private set; }       // seconds since the boss died
    public void StartEndless() { Endless = true; }       // call this when the boss dies to start the endless timer

    void Update()
    {
        Elapsed += Time.deltaTime;
        if (TimeLeft <= 0f && !bossFired)
        {
            bossFired = true;
            BossTime?.Invoke();
        }
        if (Endless)
        {
            EndlessTime += Time.deltaTime;
        }
    }
}
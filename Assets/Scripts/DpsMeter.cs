using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// Player damage per second over sliding windows. EnemyController.TakeDamage feeds it (gun, melee and effects).
[AutoStaticsCleanup]
public static partial class DpsMeter
{
    class Window
    {
        readonly float[] bins;
        readonly float binTime;
        int last = int.MinValue;

        public Window(float seconds, float binTime)
        {
            this.binTime = binTime;
            bins = new float[Mathf.RoundToInt(seconds / binTime)];
        }

        public void Add(float amount) { Advance(); bins[Index(last)] += amount; }

        public float PerSecond()
        {
            Advance();
            float sum = 0f;
            foreach (float b in bins) sum += b;
            return sum / (bins.Length * binTime);
        }

        // Empty the bins time has moved past since the last call
        void Advance()
        {
            int now = Mathf.FloorToInt(Time.time / binTime);
            if (last == int.MinValue || now - last >= bins.Length) System.Array.Clear(bins, 0, bins.Length);
            else for (int i = last + 1; i <= now; i++) bins[Index(i)] = 0f;
            last = now;
        }

        int Index(int i) => (i % bins.Length + bins.Length) % bins.Length;
    }

    static Window shortWindow = new Window(5f, 0.25f);
    static Window longWindow = new Window(60f, 1f);

    public static float Current => shortWindow.PerSecond();      // the HUD number
    public static float LastMinute => longWindow.PerSecond();    // telemetry: DPS at the end of the run
    public static float Peak { get; private set; }               // best 5 s of the run

    public static void Add(float amount)
    {
        if (amount <= 0f) return;
        shortWindow.Add(amount);
        longWindow.Add(amount);
        Peak = Mathf.Max(Peak, shortWindow.PerSecond());
    }

    public static void Reset()
    {
        shortWindow = new Window(5f, 0.25f);
        longWindow = new Window(60f, 1f);
        Peak = 0f;
    }
}

using UnityEngine;

namespace GiaoQua
{
    /// <summary>State of the run in progress. Lives only until the app closes; progress that must stay goes to SaveService.</summary>
    public static class GameSession
    {
        public static int LevelIndex;
        public static int CoinsThisRun;
        public static int CoinsInLevel;
        public static bool DiedThisRun;
        public static float StartTime;

        public static void BeginLevel(int index)
        {
            LevelIndex = index;
            CoinsThisRun = 0;
            CoinsInLevel = 0;
            DiedThisRun = false;
            StartTime = Time.time;
        }

        public static int Seconds => Mathf.Max(1, Mathf.RoundToInt(Time.time - StartTime));
    }
}

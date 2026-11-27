using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Settings applied once when the game starts, on every platform.</summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Application.targetFrameRate = 60;               // ignored by browsers, which use their own refresh rate
            Screen.sleepTimeout = SleepTimeout.NeverSleep;  // the phone screen must not turn off while playing
            Application.runInBackground = true;
        }
    }
}

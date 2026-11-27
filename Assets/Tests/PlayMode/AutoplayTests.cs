using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    /// <summary>The machine plays whole levels by itself, from the start, with nobody touching the keys.</summary>
    public class AutoplayTests : LevelTestBase
    {
        IEnumerator PlayWholeLevel(string scene, float limitSeconds)
        {
            GameSession.StartAutoplay();
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return null;
            level = LevelController.Instance;
            hero = PlayerController.Current;
            Assert.IsNotNull(hero.GetComponent<AutoPlayer>(), "autoplay attached to the hero");
            float left = limitSeconds;
            var trace = new System.Text.StringBuilder();
            float nextLog = 0f;
            while (level.State != LevelState.Won && left > 0f)
            {
                left -= Time.deltaTime;
                if (limitSeconds - left >= nextLog)
                {
                    nextLog += 5f;
                    trace.Append($"[{limitSeconds - left:0}s x={hero.transform.position.x:0.0} y={hero.transform.position.y:0.0} {hero.GetComponent<AutoPlayer>().StageName} {level.State}] ");
                    if (trace.Length > 3000) trace.Remove(0, trace.Length - 3000);
                }
                yield return null;
            }
            if (level.State != LevelState.Won) Debug.Log("TRACE " + trace);
            Assert.AreEqual(LevelState.Won, level.State, $"autoplay did not finish {scene} (x={hero.transform.position.x:0.0}, {hero.GetComponent<AutoPlayer>().DebugInfo}) TRACE: {trace}");
        }

        [UnityTest, Timeout(300000)] public IEnumerator Autoplay_plays_level_one_to_the_end() { yield return PlayWholeLevel("Level1", 150f); }
        [UnityTest, Timeout(420000)] public IEnumerator Autoplay_plays_level_two_to_the_end() { yield return PlayWholeLevel("Level2", 330f); }
        [UnityTest, Timeout(480000)] public IEnumerator Autoplay_plays_level_three_to_the_end() { yield return PlayWholeLevel("Level3", 400f); }

        // A slow phone or emulator draws few frames; the bot must still see each pit in time.
        [UnityTest, Timeout(300000)]
        public IEnumerator Autoplay_finishes_level_one_at_eight_frames_per_second() { yield return PlayAtFps("Level1", 8f); }

        [UnityTest, Timeout(300000)]
        public IEnumerator Autoplay_finishes_level_one_at_four_frames_per_second() { yield return PlayAtFps("Level1", 4f); }

        IEnumerator PlayAtFps(string scene, float fps)
        {
            Time.captureDeltaTime = 1f / fps;
            try { yield return PlayWholeLevel(scene, 150f); }
            finally { Time.captureDeltaTime = 0f; }
        }

        [UnityTest]
        public IEnumerator A_demo_run_never_changes_the_real_save()
        {
            SaveService.AddCoins(5);
            SaveService.Save();
            GameSession.StartAutoplay();
            SaveService.AddCoins(100);               // inside the demo
            Assert.AreEqual(100, SaveService.Data.coins);
            GameSession.EndAutoplay();
            Assert.AreEqual(5, SaveService.Data.coins, "the real save is back");
            yield return null;
        }
    }
}

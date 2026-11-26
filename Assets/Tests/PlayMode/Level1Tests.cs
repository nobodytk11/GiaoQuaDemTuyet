using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    /// <summary>Plays the real Level1 scene with a simple bot.</summary>
    public class Level1Tests : LevelTestBase
    {
        [UnityTest]
        public IEnumerator The_scene_has_everything_a_level_needs()
        {
            yield return Load("Level1");
            Assert.IsNotNull(Object.FindFirstObjectByType<Gift>(), "Gift");
            Assert.IsNotNull(Object.FindFirstObjectByType<GoalZone>(), "GoalZone");
            Assert.IsNotNull(Object.FindFirstObjectByType<Seesaw>(), "Seesaw");
            Assert.IsNotNull(Object.FindFirstObjectByType<KillZone>(), "KillZone");
            Assert.GreaterOrEqual(Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length, 2);
            Assert.GreaterOrEqual(Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None).Length, 25);
            Assert.GreaterOrEqual(Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length, 5);
            yield return Seconds(0.6f);
            Assert.IsTrue(hero.Grounded, "the hero starts standing on the ground");
            Assert.AreEqual(1, GameSession.LevelIndex + 1);
            Assert.GreaterOrEqual(GameSession.CoinsInLevel, 25);
        }

        [UnityTest]
        public IEnumerator A_bot_runs_from_the_start_to_the_moving_platform_without_dying()
        {
            yield return Load("Level1");
            var health = hero.GetComponent<PlayerHealth>();
            float limit = 70f;
            while (hero.transform.position.x < 90.5f && limit > 0f)
            {
                BotStep();
                Assert.IsFalse(health.IsDead, $"the bot died at x={hero.transform.position.x:0.0}");
                limit -= Time.deltaTime;
                yield return null;
            }
            Assert.Greater(hero.transform.position.x, 90f, "did not get to the end of the walking part in time");
            Assert.Greater(SaveService.Data.coins, 5, "picked up some coins on the way");
        }

        [UnityTest]
        public IEnumerator The_moving_platform_carries_the_hero_across_the_long_pit()
        {
            yield return Load("Level1");
            var mp = Object.FindFirstObjectByType<MovingPlatform>();
            Assert.IsNotNull(mp, "MovingPlatform");
            hero.Teleport(new Vector2(91.5f, 2f));
            yield return Seconds(0.4f);
            // wait until the platform is close to the left edge, then step on
            yield return WaitFor(() => mp.transform.position.x < 95f, "platform near the left end", 12f);
            input.MoveValue = 1f;
            input.PressJump();
            yield return WaitFor(() => hero.Grounded && hero.transform.position.x > 92.5f && hero.transform.position.y > 2.5f, "hero on the platform", 4f);
            input.MoveValue = 0f;
            // ride to the far end
            yield return WaitFor(() => mp.transform.position.x > 98.2f, "platform at the right end", 15f);
            input.MoveValue = 1f;
            input.PressJump();
            yield return WaitFor(() => hero.transform.position.x > 101.5f && hero.Grounded, "hero on the far ground", 5f);
            Assert.IsFalse(hero.GetComponent<PlayerHealth>().IsDead);
        }

        [UnityTest]
        public IEnumerator Jumping_onto_the_seesaw_pad_delivers_the_gift_and_wins_level_one()
        {
            yield return Load("Level1");
            var seesaw = Object.FindFirstObjectByType<Seesaw>();
            hero.Teleport(new Vector2(110.5f, 2f));
            yield return Seconds(0.5f);
            input.MoveValue = -1f;
            input.PressJump();
            yield return Seconds(0.35f);
            input.MoveValue = 0f;
            yield return WaitFor(() => level.State == LevelState.Won, "level won", 9f);
            Assert.AreEqual(1, seesaw.LaunchCount);
            Assert.IsTrue(SaveService.Data.stars[0] >= 1);
            Assert.AreEqual(2, SaveService.Data.unlockedLevels);
        }
    }
}

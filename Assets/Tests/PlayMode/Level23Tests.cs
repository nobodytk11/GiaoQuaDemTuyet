using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    public class Level2Tests : LevelTestBase
    {
        [UnityTest]
        public IEnumerator The_scene_has_ice_icicles_and_the_seesaw_that_needs_a_hard_landing()
        {
            yield return Load("Level2");
            Assert.AreEqual(2, GameSession.LevelIndex + 1);
            Assert.IsNotNull(Object.FindFirstObjectByType<IceSurface>(), "ice");
            Assert.GreaterOrEqual(Object.FindObjectsByType<FallingIcicle>(FindObjectsSortMode.None).Length, 3);
            Assert.IsNotNull(Object.FindFirstObjectByType<MovingPlatform>());
            Assert.IsNotNull(Object.FindFirstObjectByType<BouncePad>());
            Assert.Greater(Object.FindFirstObjectByType<Seesaw>().minImpact, 12f);
        }

        [UnityTest] public IEnumerator Bot_crosses_the_first_ice_stretch_and_pit() { yield return Load("Level2"); yield return RunSegment(3f, 50f); }
        [UnityTest] public IEnumerator Bot_runs_under_the_icicles_over_the_spikes() { yield return Load("Level2"); yield return RunSegment(50f, 78f); }
        [UnityTest] public IEnumerator Bot_climbs_with_the_spring_and_passes_the_enemies() { yield return Load("Level2"); yield return RunSegment(90f, 112f); }
        [UnityTest] public IEnumerator Bot_crosses_the_last_ice_stretch_with_spikes() { yield return Load("Level2"); yield return RunSegment(110f, 124f); }

        [UnityTest]
        public IEnumerator The_moving_platform_takes_the_hero_across_the_long_pit()
        {
            yield return Load("Level2");
            var mp = Object.FindFirstObjectByType<MovingPlatform>();
            yield return CrossWithPlatform(mp, 78.5f, 88.5f);
        }

        [UnityTest]
        public IEnumerator A_jump_from_the_ground_is_too_weak_but_the_spring_route_wins_the_level()
        {
            yield return Load("Level2");
            var seesaw = Object.FindFirstObjectByType<Seesaw>();
            // coming down from the top of an ordinary jump (1.7 tiles above the pad) is not hard enough
            hero.Teleport(new Vector2(130.4f, 2.6f + 1.7f));
            yield return Seconds(1.2f);
            Assert.AreEqual(0, seesaw.LaunchCount, "a plain jump is too weak on this level");
            Assert.GreaterOrEqual(seesaw.WeakHitCount, 1);
            hero.Teleport(new Vector2(133.5f, 2f));
            // the spring route
            yield return SpringToPad(133.5f);
            yield return WaitFor(() => level.State == LevelState.Won, "level 2 won", 9f);
            Assert.AreEqual(1, seesaw.LaunchCount);
            Assert.AreEqual(3, SaveService.Data.unlockedLevels);
        }
    }

    public class Level3Tests : LevelTestBase
    {
        [UnityTest]
        public IEnumerator The_scene_has_wind_low_gravity_melting_platforms_and_a_chaser()
        {
            yield return Load("Level3");
            Assert.AreEqual(3, GameSession.LevelIndex + 1);
            Assert.GreaterOrEqual(Object.FindObjectsByType<WindField>(FindObjectsSortMode.None).Length, 2);
            Assert.IsNotNull(Object.FindFirstObjectByType<GravityZone>());
            Assert.GreaterOrEqual(Object.FindObjectsByType<MeltingPlatform>(FindObjectsSortMode.None).Length, 4);
            Assert.IsNotNull(Object.FindFirstObjectByType<EnemyChaser>());
        }

        [UnityTest] public IEnumerator Bot_walks_into_the_headwind() { yield return Load("Level3"); yield return RunSegment(3f, 41f, 60f); }
        [UnityTest] public IEnumerator Bot_runs_over_the_melting_bridge() { yield return Load("Level3"); yield return RunSegment(36f, 56f, 40f); }
        [UnityTest] public IEnumerator Bot_gets_past_the_chaser_and_the_spikes() { yield return Load("Level3"); yield return RunSegment(55f, 81f, 40f); }
        [UnityTest] public IEnumerator Bot_crosses_the_low_gravity_area() { yield return Load("Level3"); yield return RunSegment(81f, 102f, 40f); }

        [UnityTest]
        public IEnumerator The_moving_platform_takes_the_hero_across_the_pit()
        {
            yield return Load("Level3");
            var mp = Object.FindFirstObjectByType<MovingPlatform>();
            yield return CrossWithPlatform(mp, 102.5f, 111.5f);
        }

        [UnityTest]
        public IEnumerator The_shelf_in_the_low_gravity_zone_is_reachable_only_inside_it()
        {
            yield return Load("Level3");
            // shelf top is 4 tiles above the ground (y = 6), at x 88..93
            hero.Teleport(new Vector2(86.5f, 2f));
            yield return Seconds(0.5f);
            input.PressJump();
            float apex = 0f;
            for (float t = 0f; t < 2.5f; t += Time.deltaTime) { apex = Mathf.Max(apex, hero.transform.position.y); yield return null; }
            Assert.Greater(apex, 5.6f, $"inside the low gravity zone the jump reaches the shelf (apex {apex:0.0})");
            hero.Teleport(new Vector2(72f, 2f));
            yield return Seconds(0.5f);
            input.PressJump();
            float normal = 0f;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { normal = Mathf.Max(normal, hero.transform.position.y); yield return null; }
            Assert.Less(normal, 4.6f, "outside the zone the same jump is much lower");
        }

        WindField Gusts() { foreach (var w in Object.FindObjectsByType<WindField>(FindObjectsSortMode.None)) if (w.period > 0f) return w; return null; }

        [UnityTest]
        public IEnumerator Launching_while_the_wind_blows_misses_but_waiting_for_calm_wins()
        {
            yield return Load("Level3");
            var seesaw = Object.FindFirstObjectByType<Seesaw>();
            var goal = Object.FindFirstObjectByType<GoalZone>();
            var gusts = Gusts();
            Assert.IsNotNull(gusts);

            // 1) launch while the gust is blowing: the wind carries the gift away
            yield return WaitFor(() => gusts.Active && Time.time % gusts.period < 0.4f, "a gust starts", 12f);
            seesaw.gift.Launch(seesaw.launchVelocity);
            yield return Seconds(4.2f);
            Assert.IsFalse(goal.Reached, "the gust blew the gift off course");
            yield return WaitFor(() => seesaw.gift.AtHome, "gift back home", 14f);

            // 2) wait far away for the calm, then take the spring route
            yield return WaitFor(() => !gusts.Active && Time.time % gusts.period > gusts.onSeconds + 0.1f && Time.time % gusts.period < gusts.onSeconds + 0.5f, "calm begins", 14f);
            yield return SpringToPad(127.5f);
            yield return WaitFor(() => level.State == LevelState.Won, "level 3 won", 9f);
            Assert.AreEqual(1, seesaw.LaunchCount, "only the real launch counts");
        }
    }
}

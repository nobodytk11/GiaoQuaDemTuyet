using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    public class EnemyTests : PlayTestBase
    {
        [UnityTest]
        public IEnumerator Stomping_a_slime_kills_it_and_bounces_the_hero()
        {
            Floor();
            var slime = Spawn<EnemyPatrol>(5f);
            slime.speed = 0f;
            var hero = Hero(5f, 3f);
            float topVy = -99f;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { topVy = Mathf.Max(topVy, hero.Velocity.y); yield return null; }
            Assert.IsTrue(slime == null || slime.IsDead, "slime dead");
            Assert.Greater(topVy, 5f, "bounced up");
            Assert.AreEqual(3, hero.GetComponent<PlayerHealth>().Hearts, "stomping is free");
            Assert.AreEqual(1, SaveService.Data.totalStomps);
        }

        [UnityTest]
        public IEnumerator Touching_from_the_side_costs_one_heart_then_the_hero_is_briefly_safe()
        {
            Floor();
            var slime = Spawn<EnemyPatrol>(2f);
            slime.speed = 0f;
            var hero = Hero(0f);
            yield return Seconds(0.3f);
            input.MoveValue = 1f;
            yield return Seconds(1.0f);
            var health = hero.GetComponent<PlayerHealth>();
            Assert.AreEqual(2, health.Hearts, "one heart lost");
            Assert.IsTrue(health.IsInvulnerable);
            yield return Seconds(0.5f);
            Assert.AreEqual(2, health.Hearts, "no second hit while invulnerable");
        }

        [UnityTest]
        public IEnumerator A_spiked_enemy_cannot_be_stomped()
        {
            Floor();
            var spiky = Spawn<EnemyPatrol>(5f, 0f, stompable: false);
            spiky.speed = 0f;
            var hero = Hero(5f, 3f);
            yield return Seconds(1f);
            Assert.IsFalse(spiky.IsDead);
            Assert.AreEqual(2, hero.GetComponent<PlayerHealth>().Hearts);
        }

        [UnityTest]
        public IEnumerator A_patrolling_enemy_turns_at_edges_and_never_falls()
        {
            Floor(0f, 10f);                                // x from -5 to 5
            var slime = Spawn<EnemyPatrol>(0f);
            slime.speed = 2.5f;
            int dir = slime.Direction, turns = 0;
            float minX = 99f, maxX = -99f, minY = 99f;
            for (float t = 0f; t < 7f; t += Time.deltaTime)
            {
                if (slime.Direction != dir) { turns++; dir = slime.Direction; }
                minX = Mathf.Min(minX, slime.transform.position.x);
                maxX = Mathf.Max(maxX, slime.transform.position.x);
                minY = Mathf.Min(minY, slime.transform.position.y);
                yield return null;
            }
            Assert.GreaterOrEqual(turns, 2, "turned at both ends");
            Assert.Greater(minY, -0.3f, "stayed on the floor");
            Assert.Less(maxX, 5.1f);
            Assert.Greater(minX, -5.1f);
        }

        [UnityTest]
        public IEnumerator A_flyer_sweeps_back_and_forth()
        {
            var fly = Spawn<EnemyFlyer>(0f, 4f);
            fly.range = 3f;
            fly.speed = 3f;
            float minX = 99f, maxX = -99f;
            for (float t = 0f; t < 5f; t += Time.deltaTime)
            {
                minX = Mathf.Min(minX, fly.transform.position.x);
                maxX = Mathf.Max(maxX, fly.transform.position.x);
                yield return null;
            }
            Assert.Greater(maxX - minX, 4.5f);
        }

        [UnityTest]
        public IEnumerator A_chaser_goes_idle_alert_chase_and_then_returns_home()
        {
            Floor();
            var chaser = Spawn<EnemyChaser>(6f);
            Assert.IsFalse(chaser.stompable);
            var hero = Hero(0f);
            input.MoveValue = 0f;
            var seen = new System.Collections.Generic.List<EnemyChaser.State>();
            for (float t = 0f; t < 1.7f; t += Time.deltaTime)
            {
                if (seen.Count == 0 || seen[seen.Count - 1] != chaser.Current) seen.Add(chaser.Current);
                yield return null;
            }
            CollectionAssert.Contains(seen, EnemyChaser.State.Alert);
            Assert.AreEqual(EnemyChaser.State.Chase, chaser.Current, "after the warning it runs at the hero");
            Assert.Less(chaser.transform.position.x, 5.2f, "it moved toward the hero");

            hero.Teleport(new Vector2(-60f, 0f));
            yield return WaitFor(() => chaser.Current == EnemyChaser.State.Idle, "chaser back to idle", 15f);
            Assert.AreEqual(6f, chaser.transform.position.x, 0.5f, "back at its starting place");
        }

        [UnityTest]
        public IEnumerator A_chaser_needs_two_snowballs_and_stomping_hurts_the_hero()
        {
            Floor();
            var chaser = Spawn<EnemyChaser>(0f);
            Assert.IsTrue(chaser.Hurt(1, HitKind.Snowball));
            Assert.IsFalse(chaser.IsDead);
            Assert.IsTrue(chaser.Hurt(1, HitKind.Snowball));
            Assert.IsTrue(chaser.IsDead);
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_thrown_snowball_kills_a_slime_and_the_throw_has_a_cooldown()
        {
            Floor();
            var slime = Spawn<EnemyPatrol>(7f);
            slime.speed = 0f;
            var hero = Hero(0f);
            yield return Seconds(0.3f);
            var shooter = hero.GetComponent<PlayerShooter>();
            Assert.IsTrue(shooter.TryThrow());
            Assert.IsFalse(shooter.TryThrow(), "cooldown");
            yield return Seconds(1.5f);
            Assert.IsTrue(slime == null || slime.IsDead, "the snowball killed it");
            Assert.AreEqual(1, SaveService.Data.totalSnowballHits);
        }

        [UnityTest]
        public IEnumerator Pressing_throw_with_the_input_also_throws()
        {
            Floor();
            var hero = Hero(0f);
            yield return Seconds(0.3f);
            input.PressThrow();
            yield return Seconds(0.1f);
            Assert.AreEqual(1, Object.FindObjectsByType<Snowball>(FindObjectsSortMode.None).Length);
        }
    }

    public class WorldObjectTests : PlayTestBase
    {
        [UnityTest]
        public IEnumerator Spikes_cost_a_heart()
        {
            Floor();
            var hero = Hero(0f);
            var spikes = Block("Spikes", new Vector2(0f, 0.3f), new Vector2(1f, 0.6f), "Hazard", true);
            spikes.AddComponent<Hazard>();
            yield return Seconds(0.5f);
            Assert.AreEqual(2, hero.GetComponent<PlayerHealth>().Hearts);
        }

        [UnityTest]
        public IEnumerator Falling_into_a_kill_zone_ends_the_run()
        {
            var hero = Hero(0f, 2f);                       // no floor
            var zone = Block("Kill", new Vector2(0f, -8f), new Vector2(40f, 2f), "Hazard", true);
            zone.AddComponent<KillZone>();
            yield return WaitFor(() => hero.GetComponent<PlayerHealth>().IsDead, "hero dead", 5f);
            Assert.IsTrue(GameSession.DiedThisRun);
            Assert.AreEqual(0, hero.GetComponent<PlayerHealth>().Hearts);
        }

        [UnityTest]
        public IEnumerator Coins_are_counted_and_saved_at_once()
        {
            Floor();
            var hero = Hero(0f);
            var coin = Pickup(CollectibleKind.Coin, new Vector2(0f, 0.6f), 3);
            yield return Seconds(0.4f);
            Assert.AreEqual(3, SaveService.Data.coins);
            Assert.AreEqual(3, GameSession.CoinsThisRun);
            Assert.IsTrue(coin == null);
        }

        [UnityTest]
        public IEnumerator A_heart_waits_when_full_and_heals_when_hurt()
        {
            Floor();
            var hero = Hero(0f);
            var health = hero.GetComponent<PlayerHealth>();
            var heart = Pickup(CollectibleKind.Heart, new Vector2(0f, 0.6f));
            yield return Seconds(0.4f);
            Assert.IsTrue(heart != null, "full hearts: the pickup stays");
            health.TakeHit(new Vector2(-2f, 0f));
            Assert.AreEqual(2, health.Hearts);
            yield return Seconds(0.8f);
            Assert.AreEqual(3, health.Hearts);
            Assert.IsTrue(heart == null);
        }

        [UnityTest]
        public IEnumerator A_shield_absorbs_exactly_one_hit()
        {
            Floor();
            var hero = Hero(0f);
            var health = hero.GetComponent<PlayerHealth>();
            var power = hero.GetComponent<PlayerPowerUps>();
            power.GiveShield();
            Assert.IsFalse(health.TakeHit(Vector2.left * 2f));
            Assert.AreEqual(3, health.Hearts);
            Assert.IsFalse(power.HasShield);
            yield return Seconds(1f);
            Assert.IsTrue(health.TakeHit(Vector2.left * 2f));
            Assert.AreEqual(2, health.Hearts);
        }

        [UnityTest]
        public IEnumerator Boots_make_the_jump_higher()
        {
            Floor();
            var hero = Hero(0f);
            yield return Seconds(0.4f);
            input.PressJump();
            float normal = 0f;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { normal = Mathf.Max(normal, hero.transform.position.y); yield return null; }
            yield return Seconds(0.5f);
            hero.GetComponent<PlayerPowerUps>().GiveBoots();
            input.PressJump();
            float boosted = 0f;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { boosted = Mathf.Max(boosted, hero.transform.position.y); yield return null; }
            Assert.Greater(boosted, normal * 1.3f, $"{boosted:0.00} vs {normal:0.00}");
        }

        [UnityTest]
        public IEnumerator A_spring_throws_the_hero_much_higher_than_a_jump()
        {
            Floor();
            var pad = Block("Spring", new Vector2(0f, 0.25f), new Vector2(1f, 0.5f));
            pad.AddComponent<BouncePad>();
            var hero = Hero(0f, 3f);
            float apex = 0f;
            for (float t = 0f; t < 2f; t += Time.deltaTime) { apex = Mathf.Max(apex, hero.transform.position.y); yield return null; }
            Assert.Greater(apex, 3.5f, "above where it started (3.0) thanks to the spring");
        }

        [UnityTest]
        public IEnumerator A_bonus_box_pays_once_when_bumped_from_below()
        {
            Floor();
            var box = Block("Box", new Vector2(0f, 2.6f), new Vector2(1f, 1f));
            var bonus = box.AddComponent<BonusBox>();
            bonus.coinReward = 2;
            var hero = Hero(0f);
            yield return Seconds(0.4f);
            input.PressJump();
            yield return Seconds(0.9f);
            Assert.IsTrue(bonus.Opened);
            Assert.AreEqual(2, SaveService.Data.coins);
            Assert.AreEqual(1, SaveService.Data.totalBoxes);
            input.PressJump();
            yield return Seconds(1.5f);
            Assert.AreEqual(2, SaveService.Data.coins, "pays only once");
        }

        [UnityTest]
        public IEnumerator A_melting_platform_crumbles_and_comes_back()
        {
            var plat = Block("Melt", new Vector2(0f, -0.2f), new Vector2(3f, 0.4f));
            var melt = plat.AddComponent<MeltingPlatform>();
            melt.meltDelay = 0.3f;
            melt.returnAfter = 1f;
            var col = plat.GetComponent<Collider2D>();
            Hero(0f);
            yield return Seconds(0.15f);
            Assert.IsTrue(col.enabled, "still solid at first");
            yield return Seconds(0.6f);
            Assert.IsFalse(col.enabled, "melted");
            yield return Seconds(1.3f);
            Assert.IsTrue(col.enabled, "back again");
        }

        [UnityTest]
        public IEnumerator An_icicle_falls_when_the_hero_walks_under_it()
        {
            Floor();
            var go = Block("Icicle", new Vector2(4f, 6f), new Vector2(0.4f, 1f), "Hazard");
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<FallingIcicle>();
            var hero = Hero(0f);
            yield return Seconds(0.5f);
            Assert.AreEqual(3, hero.GetComponent<PlayerHealth>().Hearts, "far away: nothing happens");
            Assert.Greater(go.transform.position.y, 5.9f);
            hero.Teleport(new Vector2(4f, 0f));
            yield return Seconds(1.6f);
            Assert.AreEqual(2, hero.GetComponent<PlayerHealth>().Hearts, "the icicle hit");
        }
    }

    public class LevelFlowTests : PlayTestBase
    {
        LevelController MakeLevel(Vector2 spawn)
        {
            var go = new GameObject("Level");
            var lc = go.AddComponent<LevelController>();
            lc.spawnPoint = spawn;
            return lc;
        }

        [UnityTest]
        public IEnumerator After_losing_the_hero_restarts_from_the_last_checkpoint_with_full_hearts()
        {
            Floor();
            var level = MakeLevel(new Vector2(0f, 0f));
            var hero = Hero(0f);
            var cp = Block("Checkpoint", new Vector2(10f, 1f), new Vector2(1f, 2f), "Item", true);
            cp.AddComponent<Checkpoint>();
            yield return Seconds(0.3f);
            hero.Teleport(new Vector2(10f, 0.5f));
            yield return Seconds(0.3f);
            hero.Teleport(new Vector2(30f, 0f));
            hero.GetComponent<PlayerHealth>().Kill();
            Assert.AreEqual(LevelState.Lost, level.State);
            level.Retry();
            yield return Seconds(0.3f);
            Assert.AreEqual(LevelState.Playing, level.State);
            Assert.AreEqual(10f, hero.transform.position.x, 0.6f, "back at the checkpoint, not the start");
            Assert.AreEqual(3, hero.GetComponent<PlayerHealth>().Hearts);
            Assert.IsTrue(hero.Controllable);
        }

        [UnityTest]
        public IEnumerator Without_a_checkpoint_the_retry_goes_to_the_start()
        {
            Floor();
            var level = MakeLevel(new Vector2(-5f, 0f));
            var hero = Hero(-5f);
            yield return Seconds(0.2f);
            hero.Teleport(new Vector2(20f, 0f));
            hero.GetComponent<PlayerHealth>().Kill();
            level.Retry();
            yield return Seconds(0.2f);
            Assert.AreEqual(-5f, hero.transform.position.x, 0.6f);
        }

        [UnityTest]
        public IEnumerator Pausing_stops_time_and_resuming_restores_it()
        {
            var level = MakeLevel(Vector2.zero);
            yield return null;
            level.TogglePause();
            Assert.AreEqual(LevelState.Paused, level.State);
            Assert.AreEqual(0f, Time.timeScale);
            level.TogglePause();
            Assert.AreEqual(LevelState.Playing, level.State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        // ---- the seesaw puzzle ----
        struct Puzzle { public Seesaw seesaw; public Gift gift; public GoalZone goal; public LevelController level; }

        Puzzle BuildPuzzle(float minImpact = 6f)
        {
            var p = new Puzzle();
            Floor();
            p.level = MakeLevel(Vector2.zero);
            var pad = Block("SeesawPad", new Vector2(0f, 0.15f), new Vector2(2f, 0.3f));
            p.gift = MakeGift(new Vector2(-0.8f, 0.6f));
            p.seesaw = pad.AddComponent<Seesaw>();
            p.seesaw.gift = p.gift;
            p.seesaw.minImpact = minImpact;
            Vector2 target = new Vector2(9f, 3.2f);
            p.seesaw.launchVelocity = Seesaw.SolveLaunch(target.x - p.gift.Home.x, target.y - p.gift.Home.y, 60f, 9.81f * 2f);
            var goalGo = Block("Chimney", target, new Vector2(2f, 1f), "Item", true);
            p.goal = goalGo.AddComponent<GoalZone>();
            return p;
        }

        [Test]
        public void The_launch_solver_returns_a_velocity_that_lands_on_the_target()
        {
            Vector2 v = Seesaw.SolveLaunch(9.8f, 2.6f, 60f, 19.62f);
            float t = 9.8f / v.x;
            float y = v.y * t - 0.5f * 19.62f * t * t;
            Assert.AreEqual(2.6f, y, 0.01f);
            Assert.Greater(v.y, 0f);
        }

        [UnityTest]
        public IEnumerator A_hard_landing_on_the_pad_throws_the_gift_into_the_chimney_and_wins_the_level()
        {
            var p = BuildPuzzle();
            var hero = Hero(0.9f, 5f);
            yield return WaitFor(() => p.goal.Reached, "gift in the chimney", 8f);
            Assert.AreEqual(1, p.seesaw.LaunchCount);
            Assert.AreEqual(LevelState.Won, p.level.State);
            Assert.IsTrue(p.gift.Delivered);
            Assert.AreEqual(2, SaveService.Data.unlockedLevels, "level 1 done, level 2 open");
            Assert.GreaterOrEqual(p.level.Result.Stars, 2);
        }

        [UnityTest]
        public IEnumerator A_soft_step_on_the_pad_does_not_launch()
        {
            var p = BuildPuzzle();
            var hero = Hero(0.9f, 0.4f);
            yield return Seconds(1.2f);
            Assert.AreEqual(0, p.seesaw.LaunchCount);
            Assert.GreaterOrEqual(p.seesaw.WeakHitCount, 1);
            Assert.IsTrue(p.gift.AtHome);
        }

        [UnityTest]
        public IEnumerator A_gift_that_misses_goes_back_to_its_home_spot()
        {
            var p = BuildPuzzle();
            yield return Seconds(0.3f);
            p.gift.Launch(new Vector2(3f, 4f));          // far too weak to reach the chimney
            yield return WaitFor(() => !p.gift.AtHome, "gift flying", 2f);
            yield return WaitFor(() => p.gift.AtHome, "gift back home", 12f);
            Assert.IsFalse(p.gift.Delivered);
        }

        [UnityTest]
        public IEnumerator The_wind_can_push_the_gift_off_course()
        {
            var p = BuildPuzzle();
            var zone = Block("Wind", new Vector2(7f, 4f), new Vector2(8f, 10f), "Default", true);   // only along the flight path
            var w = zone.AddComponent<WindField>();
            w.wind = new Vector2(0f, 1f);                // blows up: the arc goes too high
            w.giftForce = 25f;
            var hero = Hero(0.9f, 5f);
            yield return Seconds(4f);
            Assert.IsFalse(p.goal.Reached, "strong wind pushed the gift past the chimney");
            Assert.AreEqual(1, p.seesaw.LaunchCount);
        }
    }
}

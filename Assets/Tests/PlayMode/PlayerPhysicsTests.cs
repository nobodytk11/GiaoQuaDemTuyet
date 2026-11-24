using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    public class PlayerPhysicsTests : PlayTestBase
    {
        [UnityTest]
        public IEnumerator Hero_settles_on_the_floor_and_is_grounded()
        {
            Floor();
            var hero = Hero(0f, 2f);
            yield return Seconds(1f);
            Assert.IsTrue(hero.Grounded);
            Assert.AreEqual(0f, hero.transform.position.y, 0.08f);
        }

        [UnityTest]
        public IEnumerator Holding_right_reaches_max_speed_and_releasing_stops()
        {
            Floor();
            var hero = Hero(0f);
            yield return Seconds(0.3f);
            input.MoveValue = 1f;
            yield return Seconds(1f);
            Assert.AreEqual(hero.maxSpeed, hero.Velocity.x, 0.4f);
            input.MoveValue = 0f;
            yield return Seconds(1f);
            Assert.Less(Mathf.Abs(hero.Velocity.x), 0.2f);
        }

        [UnityTest]
        public IEnumerator A_held_jump_reaches_about_the_set_height()
        {
            Floor();
            var hero = Hero(0f);
            yield return Seconds(0.4f);
            input.PressJump();
            float apex = 0f;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { apex = Mathf.Max(apex, hero.transform.position.y); yield return null; }
            Assert.AreEqual(hero.jumpHeight, apex, 0.3f);
        }

        [UnityTest]
        public IEnumerator A_quick_tap_gives_a_shorter_hop()
        {
            Floor();
            var hero = Hero(0f);
            yield return Seconds(0.4f);
            input.PressJump();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            input.ReleaseJump();
            float apex = 0f;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { apex = Mathf.Max(apex, hero.transform.position.y); yield return null; }
            Assert.Less(apex, hero.jumpHeight * 0.7f);
            Assert.Greater(apex, 0.3f);
        }

        [UnityTest]
        public IEnumerator Jump_still_works_a_moment_after_leaving_an_edge()
        {
            Block("Ledge", new Vector2(-5f, -0.5f), new Vector2(10f, 1f));    // top y=0 up to x=0
            var hero = Hero(-1f);
            yield return Seconds(0.4f);
            input.MoveValue = 1f;
            yield return WaitFor(() => hero.transform.position.x >= 0.45f, "hero past the edge");
            input.PressJump();
            yield return Seconds(0.3f);
            Assert.Greater(hero.Velocity.y, -1f, "coyote jump must lift the hero, not just let it fall");
            Assert.Greater(hero.transform.position.y, 0.3f);
        }

        [UnityTest]
        public IEnumerator Jump_pressed_just_before_landing_is_remembered()
        {
            Floor();
            var hero = Hero(0f, 0.25f);
            yield return new WaitForFixedUpdate();
            input.PressJump();                       // pressed while still in the air
            yield return Seconds(0.35f);
            Assert.Greater(hero.transform.position.y, 0.3f, "buffered jump should fire on landing");
        }

        [UnityTest]
        public IEnumerator Ice_makes_stopping_much_slower()
        {
            // normal ground
            var floor = Floor();
            var hero = Hero(0f);
            yield return Seconds(0.3f);
            input.MoveValue = 1f;
            yield return Seconds(0.7f);
            input.MoveValue = 0f;
            float startX = hero.transform.position.x;
            yield return Seconds(1.5f);
            float normalStop = hero.transform.position.x - startX;
            Object.Destroy(hero.gameObject);
            floor.AddComponent<IceSurface>();
            yield return null;
            var hero2 = Hero(-20f);
            hero2.transform.position = new Vector2(-20f, 0f);
            yield return Seconds(0.3f);
            input.MoveValue = 1f;
            yield return Seconds(1.2f);          // ice accelerates slowly, give it more time
            float speedOnIce = hero2.Velocity.x;
            input.MoveValue = 0f;
            float startIce = hero2.transform.position.x;
            yield return Seconds(1.5f);
            float iceStop = hero2.transform.position.x - startIce;
            Assert.Less(normalStop, 1.0f, "on normal ground it stops within a step");
            Assert.Greater(speedOnIce, 2f);
            Assert.Greater(iceStop, normalStop * 3f, $"ice slide {iceStop:0.00} should be far longer than {normalStop:0.00}");
        }

        [UnityTest]
        public IEnumerator A_moving_platform_carries_the_hero()
        {
            var plat = Block("Platform", new Vector2(0f, -0.2f), new Vector2(4f, 0.4f));
            var rb = plat.AddComponent<Rigidbody2D>();
            var mp = plat.AddComponent<MovingPlatform>();
            mp.offset = new Vector2(6f, 0f);
            mp.speed = 2f;
            var hero = Hero(0f, 0f);
            yield return Seconds(1.5f);
            Assert.Greater(plat.transform.position.x, 2f, "platform moved");
            Assert.AreEqual(plat.transform.position.x, hero.transform.position.x, 0.8f, "hero rides along");
            Assert.IsTrue(hero.Grounded);
        }

        [UnityTest]
        public IEnumerator Wind_pushes_the_hero_but_keeping_control()
        {
            Floor();
            var zone = Block("Wind", new Vector2(0f, 3f), new Vector2(60f, 8f), "Default", true);
            var wind = zone.AddComponent<WindField>();
            wind.wind = new Vector2(3f, 0f);
            var hero = Hero(0f);
            yield return Seconds(1.2f);
            Assert.AreEqual(3f, hero.Velocity.x, 0.5f, "standing still, the wind carries the hero");
            input.MoveValue = -1f;
            yield return Seconds(1f);
            Assert.AreEqual(-3.5f, hero.Velocity.x, 0.6f, "walking against the wind is slower (6.5 - 3)");
        }

        [UnityTest]
        public IEnumerator A_low_gravity_zone_makes_the_same_jump_much_higher()
        {
            Floor();
            var hero = Hero(0f);
            yield return Seconds(0.4f);
            input.PressJump();
            float normalApex = 0f;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { normalApex = Mathf.Max(normalApex, hero.transform.position.y); yield return null; }
            yield return Seconds(0.6f);
            var zone = Block("Low", new Vector2(0f, 4f), new Vector2(20f, 20f), "Default", true);
            zone.AddComponent<GravityZone>();
            yield return Seconds(0.3f);
            input.PressJump();
            float lowApex = 0f;
            for (float t = 0f; t < 2.5f; t += Time.deltaTime) { lowApex = Mathf.Max(lowApex, hero.transform.position.y); yield return null; }
            Assert.Greater(lowApex, normalApex * 1.5f, $"low gravity apex {lowApex:0.0} vs normal {normalApex:0.0}");
        }

        [UnityTest]
        public IEnumerator Ducking_makes_the_hero_shorter_and_standing_up_needs_room()
        {
            Floor();
            Block("Low ceiling", new Vector2(0f, 1.2f), new Vector2(6f, 0.4f));    // underside at y = 1.0
            var hero = Hero(-5f);
            var box = hero.GetComponent<BoxCollider2D>();
            yield return Seconds(0.4f);
            float standing = box.size.y;
            input.DuckValue = true;
            yield return Seconds(0.3f);
            Assert.IsTrue(hero.Ducking);
            Assert.Less(box.size.y, standing * 0.8f);

            input.MoveValue = 1f;                         // crawl under the ceiling
            yield return WaitFor(() => hero.transform.position.x >= 0f, "hero under the ceiling");
            input.DuckValue = false;
            yield return Seconds(0.3f);
            Assert.IsTrue(hero.Ducking, "no room to stand under a ceiling at 1.0");

            yield return WaitFor(() => hero.transform.position.x >= 3.6f, "hero past the ceiling");
            yield return Seconds(0.3f);
            Assert.IsFalse(hero.Ducking, "free again after the ceiling");
            Assert.AreEqual(standing, box.size.y, 0.001f);
        }
    }
}

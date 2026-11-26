using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    /// <summary>Shared helpers for playing a real level scene with a bot.</summary>
    public abstract class LevelTestBase : PlayTestBase
    {
        protected PlayerController hero;
        protected LevelController level;

        protected IEnumerator Load(string scene)
        {
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return null;
            hero = PlayerController.Current;
            Assert.IsNotNull(hero, "no hero in " + scene);
            hero.Input = input;
            level = LevelController.Instance;
            Assert.IsNotNull(level);
            yield return new WaitForFixedUpdate();
        }

        /// <summary>One decision of the runner bot: run right, jump over pits, walls, spikes and enemies.</summary>
        protected void BotStep()
        {
            input.MoveValue = 1f;
            var p = (Vector2)hero.transform.position;
            if (!hero.Grounded)
            {
                input.JumpHeldValue = hero.Velocity.y > 0.5f;
                return;
            }
            int ground = LayerMask.GetMask("Ground");
            bool pit = !Physics2D.Raycast(p + new Vector2(0.75f, 0.5f), Vector2.down, 1.6f, ground);     // jump at the very edge
            bool wall = Physics2D.Raycast(p + new Vector2(0f, 0.3f), Vector2.right, 0.9f, ground);
            bool danger = Physics2D.OverlapBox(p + new Vector2(2.1f, 0.5f), new Vector2(2.6f, 0.9f), 0f, LayerMask.GetMask("Hazard", "Enemy")) != null;
            if (pit || wall || danger) input.PressJump();
        }

        /// <summary>Puts the hero on the ground at fromX and lets the bot run until it passes toX.</summary>
        protected IEnumerator RunSegment(float fromX, float toX, float timeLimit = 40f)
        {
            hero.Teleport(new Vector2(fromX, 2f));
            hero.GetComponent<PlayerHealth>().ResetFull();
            yield return new WaitForFixedUpdate();
            var health = hero.GetComponent<PlayerHealth>();
            float left = timeLimit;
            while (hero.transform.position.x < toX && left > 0f)
            {
                BotStep();
                Assert.IsFalse(health.IsDead, $"the bot died at x={hero.transform.position.x:0.0} (segment {fromX}..{toX})");
                left -= Time.deltaTime;
                yield return null;
            }
            input.MoveValue = 0f;
            Assert.Greater(hero.transform.position.x, toX - 0.01f, $"did not get from {fromX} to {toX} in time (reached {hero.transform.position.x:0.0})");
        }

        /// <summary>Rides the horizontal moving platform whose home is near x and steps off on the far side.</summary>
        protected IEnumerator CrossWithPlatform(MovingPlatform mp, float groundLeftX, float groundRightX)
        {
            hero.Teleport(new Vector2(groundLeftX, 2f));
            hero.GetComponent<PlayerHealth>().ResetFull();
            yield return Seconds(0.4f);
            float home = mp.transform.position.x;
            yield return WaitFor(() => mp.transform.position.x < home + 0.8f, "platform near its left end", 14f);
            input.MoveValue = 1f;
            input.PressJump();
            yield return WaitFor(() => hero.Grounded && hero.transform.position.y > 2.5f, "hero on the platform", 4f);
            input.MoveValue = 0f;
            yield return WaitFor(() => mp.transform.position.x > home + 3.2f, "platform near its right end", 14f);
            input.MoveValue = 1f;
            input.PressJump();
            yield return WaitFor(() => hero.transform.position.x > groundRightX && hero.Grounded, "hero on the far ground", 5f);
            Assert.IsFalse(hero.GetComponent<PlayerHealth>().IsDead);
        }

        /// <summary>The spring route: drop on the spring, steer left, land on the seesaw pad.</summary>
        protected IEnumerator SpringToPad(float springX)
        {
            hero.Teleport(new Vector2(springX, 3.2f));
            hero.GetComponent<PlayerHealth>().ResetFull();
            yield return WaitFor(() => hero.Velocity.y > 8f, "spring bounce", 4f);
            input.MoveValue = -1f;
            yield return Seconds(0.45f);
            input.MoveValue = 0f;
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    /// <summary>Small helpers that build a flat test world in code.</summary>
    public abstract class PlayTestBase
    {
        protected ScriptedInput input;
        System.Collections.Generic.HashSet<GameObject> existing;

        [SetUp]
        public void BaseSetUp()
        {
            // objects that were here before the test (the test runner itself) must survive the clean-up
            existing = new System.Collections.Generic.HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
            SaveService.UseStore(new MemoryStore());
            Time.timeScale = 1f;
            GameSession.BeginLevel(0);
            input = new ScriptedInput();
        }

        [TearDown]
        public void BaseTearDown()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (!existing.Contains(go)) Object.Destroy(go);
            Time.timeScale = 1f;
            SaveService.UseStore(new PlayerPrefsStore());
        }

        protected static GameObject Block(string name, Vector2 center, Vector2 size, string layer = "Ground", bool trigger = false)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(layer) };
            go.transform.position = center;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = trigger;
            return go;
        }

        /// <summary>Flat floor whose top surface is y = 0.</summary>
        protected static GameObject Floor(float centerX = 0f, float width = 200f)
        {
            return Block("Floor", new Vector2(centerX, -0.5f), new Vector2(width, 1f));
        }

        protected PlayerController Hero(float x, float y = 0f)
        {
            var go = PlayerFactory.Create(new Vector2(x, y));
            var pc = go.GetComponent<PlayerController>();
            pc.Input = input;
            return pc;
        }

        protected static EnemyPatrol Patrol(float x, float y = 0f, float speed = 0f)
        {
            var go = new GameObject("Slime") { layer = LayerMask.NameToLayer("Enemy") };
            go.transform.position = new Vector2(x, y);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.Square(Color.magenta);
            go.transform.localScale = new Vector3(0.8f, 0.7f, 1f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.offset = new Vector2(0f, 0.5f);
            var e = go.AddComponent<EnemyPatrol>();
            e.speed = speed;
            return e;
        }

        protected static T Spawn<T>(float x, float y = 0f, bool stompable = true) where T : Enemy
        {
            var go = new GameObject(typeof(T).Name) { layer = LayerMask.NameToLayer("Enemy") };
            go.transform.position = new Vector2(x, y);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.Square(Color.magenta);
            go.transform.localScale = new Vector3(0.8f, 0.7f, 1f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.offset = new Vector2(0f, 0.5f);
            var e = go.AddComponent<T>();
            if (!stompable) e.stompable = false;       // leave each enemy's own default alone
            return e;
        }

        protected static Collectible Pickup(CollectibleKind kind, Vector2 at, int amount = 1)
        {
            var go = new GameObject("Pickup " + kind) { layer = LayerMask.NameToLayer("Item") };
            go.transform.position = at;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            var c = go.AddComponent<Collectible>();
            c.kind = kind;
            c.amount = amount;
            return c;
        }

        protected static Gift MakeGift(Vector2 home)
        {
            var go = new GameObject("Gift") { layer = LayerMask.NameToLayer("Gift") };
            go.transform.position = home;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 0.6f);
            return go.AddComponent<Gift>();
        }

        protected static IEnumerator Seconds(float s) { yield return new WaitForSeconds(s); }

        /// <summary>Waits until the condition holds. Fails the test (instead of hanging) after the time limit.</summary>
        protected static IEnumerator WaitFor(System.Func<bool> condition, string what, float limitSeconds = 8f)
        {
            float t = 0f;
            while (!condition())
            {
                t += Time.deltaTime;
                if (t > limitSeconds) Assert.Fail("timed out waiting for: " + what);
                yield return null;
            }
        }
    }
}

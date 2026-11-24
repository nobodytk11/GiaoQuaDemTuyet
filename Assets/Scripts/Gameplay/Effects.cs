using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Sprites made in code (circles and squares) for things that have no picture yet, like tests and snowballs.</summary>
    public static class ProceduralSprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Circle(Color color, int size = 32)
        {
            string key = "c" + color + size;
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01(r - d);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
                }
            tex.Apply();
            return cache[key] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public static Sprite Square(Color color)
        {
            string key = "s" + color;
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            return cache[key] = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }
    }

    /// <summary>Throws snowballs with J. At most three flying at once.</summary>
    public class PlayerShooter : MonoBehaviour
    {
        public Sprite snowballSprite;
        public float cooldown = 0.35f;
        public int maxActive = 3;
        public Vector2 muzzle = new Vector2(0.45f, 0.8f);
        public Vector2 speed = new Vector2(11f, 3.5f);

        PlayerController controller;
        float nextAllowed;
        readonly List<Snowball> active = new List<Snowball>();

        void Awake() { controller = GetComponent<PlayerController>(); }

        void Update()
        {
            if (controller == null || controller.Input == null || !controller.Controllable) return;
            if (controller.Input.ThrowDown) TryThrow();
        }

        public bool TryThrow()
        {
            active.RemoveAll(s => s == null);
            if (Time.time < nextAllowed || active.Count >= maxActive) return false;
            nextAllowed = Time.time + cooldown;
            int dir = controller != null ? controller.Facing : 1;
            Vector2 pos = (Vector2)transform.position + new Vector2(muzzle.x * dir, muzzle.y);
            var vel = new Vector2(speed.x * dir, speed.y) + (controller != null ? new Vector2(controller.Velocity.x * 0.3f, 0f) : Vector2.zero);
            active.Add(Snowball.Spawn(pos, vel, snowballSprite));
            GameAudio.Play("snowball_throw");
            return true;
        }
    }

    public class Snowball : MonoBehaviour
    {
        public float lifetime = 2.5f;
        float born;

        public static Snowball Spawn(Vector2 position, Vector2 velocity, Sprite sprite)
        {
            var go = new GameObject("Snowball") { layer = LayerMask.NameToLayer("Projectile") };
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : ProceduralSprites.Circle(Color.white, 24);
            sr.sortingOrder = 5;
            go.transform.localScale = sprite != null ? Vector3.one * 0.5f : Vector3.one * 0.3f;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.4f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = velocity;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = sprite != null ? 0.25f : 0.5f;
            var ball = go.AddComponent<Snowball>();
            ball.born = Time.time;
            return ball;
        }

        void Update() { if (Time.time - born > lifetime) Destroy(gameObject); }

        void OnCollisionEnter2D(Collision2D c)
        {
            var enemy = c.collider.GetComponentInParent<Enemy>();
            if (enemy != null) enemy.Hurt(1, HitKind.Snowball);
            else if (c.collider.GetComponentInParent<BonusBox>() == null) GameAudio.Play("snowball_hit", 0.5f);
            Destroy(gameObject);
        }
    }
}

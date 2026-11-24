using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua.Gameplay
{
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

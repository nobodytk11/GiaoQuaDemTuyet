using System;
using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>
    /// Base of every enemy: hearts, being stomped or hit by a snowball, and hurting the hero on touch.
    /// Spiked enemies set stompable = false (stepping on them hurts the hero).
    /// </summary>
    public abstract class Enemy : MonoBehaviour
    {
        public int hp = 1;
        public bool stompable = true;
        public bool hurtBySnowball = true;
        public int contactDamage = 1;

        public bool IsDead { get; private set; }
        public event Action<Enemy, HitKind> Killed;

        protected Rigidbody2D rb;
        protected SpriteRenderer sprite;
        protected Collider2D col;
        protected LayerMask groundMask;
        protected Vector2 home;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sprite = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            groundMask = LayerMask.GetMask("Ground");
            home = transform.position;
        }

        public bool Hurt(int damage, HitKind kind)
        {
            if (IsDead) return false;
            if (kind == HitKind.Snowball && !hurtBySnowball) return false;
            hp -= damage;
            if (hp <= 0) { Die(kind); return true; }
            StartCoroutine(Flash());
            GameAudio.Play("boss_hit", 0.6f);
            return true;
        }

        IEnumerator Flash()
        {
            if (sprite == null) yield break;
            sprite.color = new Color(1f, 0.5f, 0.5f);
            yield return new WaitForSeconds(0.12f);
            if (sprite != null) sprite.color = Color.white;
        }

        void Die(HitKind kind)
        {
            IsDead = true;
            if (col != null) col.enabled = false;
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
            if (kind == HitKind.Stomp) SaveService.CountStomp();
            else if (kind == HitKind.Snowball) SaveService.CountSnowballHit();
            GameAudio.Play("stomp");
            Killed?.Invoke(this, kind);
            StartCoroutine(Squash());
        }

        IEnumerator Squash()
        {
            Vector3 start = transform.localScale;
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float k = t / 0.3f;
                transform.localScale = new Vector3(start.x * (1f + 0.3f * k), start.y * (1f - 0.8f * k), start.z);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnCollisionEnter2D(Collision2D c) { TouchPlayer(c.collider); }
        void OnCollisionStay2D(Collision2D c) { TouchPlayer(c.collider); }
        void OnTriggerStay2D(Collider2D other) { TouchPlayer(other); }

        void TouchPlayer(Collider2D other)
        {
            if (IsDead) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            if (stompable && player.IsStompingOn(col))
            {
                Hurt(1, HitKind.Stomp);
                player.Bounce(10f);
                return;
            }
            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.TakeHit(transform.position, contactDamage);
        }

        /// <summary>Is there floor one step ahead, and no wall in the way?</summary>
        protected bool CanStep(int dir, float reach = 0.45f)
        {
            Vector2 p = rb != null ? rb.position : (Vector2)transform.position;
            bool wall = Physics2D.Raycast(p + new Vector2(0f, 0.3f), new Vector2(dir, 0f), reach, groundMask);
            bool floor = Physics2D.Raycast(p + new Vector2(dir * reach, 0.2f), Vector2.down, 0.7f, groundMask);
            return !wall && floor;
        }
    }
}

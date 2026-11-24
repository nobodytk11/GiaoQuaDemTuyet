using System;
using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    public enum HitKind { Stomp, Snowball, Other }

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

    /// <summary>Walks back and forth, turning at walls and edges.</summary>
    public class EnemyPatrol : Enemy
    {
        public float speed = 1.6f;
        public float patrolRange = 0f;   // 0 = unlimited, otherwise stay within home.x +- range
        public int Direction { get; private set; } = -1;

        void FixedUpdate()
        {
            if (IsDead) return;
            bool outOfRange = patrolRange > 0f && Mathf.Abs(rb.position.x - home.x) > patrolRange
                              && Mathf.Sign(rb.position.x - home.x) == Direction;
            if (!CanStep(Direction) || outOfRange) Direction = -Direction;
            rb.linearVelocity = new Vector2(Direction * speed, rb.linearVelocity.y);
            if (sprite != null) sprite.flipX = Direction > 0;
        }
    }

    /// <summary>Flies left and right with a gentle bob. Not affected by gravity.</summary>
    public class EnemyFlyer : Enemy
    {
        public float range = 3f;
        public float speed = 2f;
        public float bobHeight = 0.4f;
        float phase;

        protected override void Awake()
        {
            base.Awake();
            if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; }
        }

        void FixedUpdate()
        {
            if (IsDead) return;
            phase += Time.fixedDeltaTime * speed / Mathf.Max(0.5f, range);
            float x = home.x + Mathf.Sin(phase) * range;
            float y = home.y + Mathf.Sin(phase * 3f) * bobHeight;
            float dir = Mathf.Cos(phase);
            rb.MovePosition(new Vector2(x, y));
            if (sprite != null) sprite.flipX = dir > 0;
        }
    }

    /// <summary>
    /// Finite state machine: Idle, Alert (a short warning), Chase, Return.
    /// Cannot be stomped; two snowballs defeat it.
    /// </summary>
    public class EnemyChaser : Enemy
    {
        public enum State { Idle, Alert, Chase, Return }

        public float sightRange = 7f;
        public float loseRange = 11f;
        public float chaseSpeed = 3.4f;
        public float returnSpeed = 2f;
        public float alertSeconds = 0.5f;

        public State Current { get; private set; } = State.Idle;
        float alertUntil;

        protected override void Awake()
        {
            base.Awake();
            stompable = false;
            hp = Mathf.Max(hp, 2);
        }

        void FixedUpdate()
        {
            if (IsDead) return;
            var player = PlayerController.Current;
            float dist = player != null ? Mathf.Abs(player.transform.position.x - rb.position.x) : float.MaxValue;
            float dy = player != null ? Mathf.Abs(player.transform.position.y - rb.position.y) : float.MaxValue;
            bool alive = player != null && !(player.GetComponent<PlayerHealth>()?.IsDead ?? false);

            switch (Current)
            {
                case State.Idle:
                    Walk(0f);
                    if (alive && dist < sightRange && dy < 3f) { Current = State.Alert; alertUntil = Time.time + alertSeconds; Tint(new Color(1f, 0.7f, 0.7f)); }
                    break;
                case State.Alert:
                    Walk(0f);
                    if (!alive || dist > loseRange) { Current = State.Return; Tint(Color.white); }
                    else if (Time.time >= alertUntil) Current = State.Chase;
                    break;
                case State.Chase:
                    if (!alive || dist > loseRange) { Current = State.Return; Tint(Color.white); break; }
                    int dir = player.transform.position.x >= rb.position.x ? 1 : -1;
                    Walk(CanStep(dir) ? dir * chaseSpeed : 0f);
                    break;
                case State.Return:
                    float dx = home.x - rb.position.x;
                    if (Mathf.Abs(dx) < 0.2f) { Current = State.Idle; Walk(0f); break; }
                    int back = dx > 0 ? 1 : -1;
                    Walk(CanStep(back) ? back * returnSpeed : 0f);
                    if (alive && dist < sightRange && dy < 3f) { Current = State.Alert; alertUntil = Time.time + alertSeconds; }
                    break;
            }
        }

        void Walk(float vx)
        {
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
            if (sprite != null && Mathf.Abs(vx) > 0.1f) sprite.flipX = vx > 0;
        }

        void Tint(Color c) { if (sprite != null) sprite.color = c; }
    }
}

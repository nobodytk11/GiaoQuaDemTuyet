using System;
using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
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

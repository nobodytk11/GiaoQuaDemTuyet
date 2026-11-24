using System;
using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
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
}

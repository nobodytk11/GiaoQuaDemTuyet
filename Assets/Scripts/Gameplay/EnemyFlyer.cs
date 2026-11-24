using System;
using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
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
}

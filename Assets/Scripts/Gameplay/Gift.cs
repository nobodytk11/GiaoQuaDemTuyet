using System;
using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>The present. Rests at its home spot on the seesaw; if it ends up somewhere else it goes home again after a while.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Gift : MonoBehaviour
    {
        public float idleSecondsBeforeReset = 2.5f;
        public float fallLimitY = -30f;

        public Rigidbody2D Body { get; private set; }
        public Vector2 Home { get; set; }
        public bool Delivered { get; private set; }
        public bool AtHome => ((Vector2)transform.position - Home).sqrMagnitude < 0.25f * 0.25f;
        public bool InFlight { get; private set; }
        float idleTime;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Home = transform.position;
        }

        void FixedUpdate()
        {
            if (Delivered) return;
            if (transform.position.y < fallLimitY) { ResetHome(); return; }
            bool still = Body.linearVelocity.sqrMagnitude < 0.04f;
            if (InFlight && still && !AtHome) InFlight = false;
            if (still && !AtHome && !InFlight) idleTime += Time.fixedDeltaTime; else idleTime = 0f;
            if (idleTime > idleSecondsBeforeReset) ResetHome();
        }

        public void Launch(Vector2 velocity)
        {
            InFlight = true;
            idleTime = 0f;
            Body.linearVelocity = velocity;
            Body.angularVelocity = -180f;
        }

        public void ResetHome()
        {
            if (Delivered) return;
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
            Body.position = Home;
            transform.position = Home;
            transform.rotation = Quaternion.identity;
            InFlight = false;
            idleTime = 0f;
            Wake?.Invoke();
        }

        public event Action Wake;

        public void Deliver()
        {
            Delivered = true;
            Body.linearVelocity = Vector2.zero;
            Body.simulated = false;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }
    }
}

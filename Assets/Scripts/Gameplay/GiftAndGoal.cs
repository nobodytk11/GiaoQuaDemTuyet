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

    /// <summary>
    /// The seesaw: when the hero lands on the pad hard enough, the gift is thrown along a fixed arc.
    /// The level builder works out the arc so it reaches the chimney; wind can still push it off.
    /// </summary>
    public class Seesaw : MonoBehaviour
    {
        public Gift gift;
        public Transform plankVisual;
        public Vector2 launchVelocity = new Vector2(6f, 10f);
        public float minImpact = 6f;
        public float restAngle = -12f;
        public float flippedAngle = 12f;

        public int LaunchCount { get; private set; }
        public int WeakHitCount { get; private set; }
        public event Action<float> WeakHit;     // impact that was too small
        public event Action Launched;

        void Start()
        {
            if (gift != null) gift.Wake += ResetPlank;
            ResetPlank();
        }

        void OnDestroy() { if (gift != null) gift.Wake -= ResetPlank; }

        void ResetPlank()
        {
            if (plankVisual != null) plankVisual.localRotation = Quaternion.Euler(0f, 0f, restAngle);
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            var hero = c.collider.GetComponentInParent<PlayerController>();
            if (hero == null || gift == null || gift.Delivered) return;
            if (gift.InFlight || !gift.AtHome) return;
            // how hard the hero came down: its speed just before this physics step
            float impact = Mathf.Max(-hero.PreStepVelocity.y, Mathf.Abs(c.relativeVelocity.y));
            if (impact < minImpact)
            {
                WeakHitCount++;
                GameAudio.Play("ui_move", 0.6f);
                WeakHit?.Invoke(impact);
                return;
            }
            LaunchCount++;
            gift.Launch(launchVelocity);
            GameAudio.Play("seesaw_launch");
            if (plankVisual != null) StartCoroutine(Tilt());
            Launched?.Invoke();
        }

        IEnumerator Tilt()
        {
            for (float t = 0f; t < 0.15f; t += Time.deltaTime)
            {
                plankVisual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(restAngle, flippedAngle, t / 0.15f));
                yield return null;
            }
            plankVisual.localRotation = Quaternion.Euler(0f, 0f, flippedAngle);
        }

        /// <summary>The speed that makes a ballistic arc at the given angle reach (dx, dy) from the launch point.</summary>
        public static Vector2 SolveLaunch(float dx, float dy, float angleDegrees, float gravity)
        {
            float a = angleDegrees * Mathf.Deg2Rad;
            float denom = 2f * Mathf.Pow(Mathf.Cos(a), 2f) * (dx * Mathf.Tan(a) - dy);
            if (denom <= 0f) return Vector2.zero;
            float v = Mathf.Sqrt(gravity * dx * dx / denom);
            return new Vector2(Mathf.Cos(a) * v * Mathf.Sign(dx), Mathf.Sin(a) * v);
        }
    }

    /// <summary>The chimney's open top. A gift that falls in wins the level.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZone : MonoBehaviour
    {
        public static event Action GiftDelivered;
        public bool Reached { get; private set; }

        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Reached) return;
            var gift = other.GetComponentInParent<Gift>();
            if (gift == null) return;
            Reached = true;
            gift.Deliver();
            GameAudio.Play("gift_delivered");
            GiftDelivered?.Invoke();
            var level = LevelController.Instance;
            if (level != null) level.Win();
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
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
}

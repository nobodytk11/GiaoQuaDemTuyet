using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Moves between two points. The hero standing on it is carried along.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        public Vector2 offset = new Vector2(3f, 0f);
        public float speed = 1.5f;
        public float pause = 0.3f;

        Rigidbody2D rb;
        Vector2 a, b;
        bool towardB = true;
        float resumeAt;

        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            a = transform.position;
            b = a + offset;
        }

        void FixedUpdate()
        {
            if (Time.time < resumeAt) { rb.linearVelocity = Vector2.zero; return; }
            Vector2 target = towardB ? b : a;
            Vector2 to = target - rb.position;
            float step = speed * Time.fixedDeltaTime;
            if (to.magnitude <= step)
            {
                rb.position = target;
                rb.linearVelocity = Vector2.zero;
                towardB = !towardB;
                resumeAt = Time.time + pause;
            }
            else rb.linearVelocity = to.normalized * speed;
        }

        void OnDrawGizmosSelected()
        {
            Vector2 start = Application.isPlaying ? a : (Vector2)transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, start + offset);
        }
    }
}

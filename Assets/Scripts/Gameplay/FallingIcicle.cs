using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Hangs still until the hero walks under it, then falls.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FallingIcicle : MonoBehaviour
    {
        public float triggerHalfWidth = 0.9f;
        public float triggerDepth = 8f;
        public int damage = 1;
        Rigidbody2D rb;
        bool falling;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        void FixedUpdate()
        {
            if (falling) return;
            var p = PlayerController.Current;
            if (p == null) return;
            float dx = Mathf.Abs(p.transform.position.x - rb.position.x);
            float below = rb.position.y - p.transform.position.y;
            if (dx < triggerHalfWidth && below > 0f && below < triggerDepth)
            {
                falling = true;
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 3f;
            }
        }

        void OnCollisionEnter2D(Collision2D c) { Hit(c.collider); }
        void OnTriggerEnter2D(Collider2D other) { Hit(other); }

        void Hit(Collider2D other)
        {
            if (!falling) return;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeHit(transform.position, damage);
            if (health != null || other.gameObject.layer == LayerMask.NameToLayer("Ground")) Destroy(gameObject);
        }
    }
}

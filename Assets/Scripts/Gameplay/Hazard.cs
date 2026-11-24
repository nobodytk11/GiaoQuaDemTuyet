using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Spikes and similar: hurts the hero while touching.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        public int damage = 1;
        void Awake() { GetComponent<Collider2D>().isTrigger = true; }
        void OnTriggerEnter2D(Collider2D other) { Hit(other); }
        void OnTriggerStay2D(Collider2D other) { Hit(other); }

        void Hit(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeHit(transform.position, damage);
        }
    }
}

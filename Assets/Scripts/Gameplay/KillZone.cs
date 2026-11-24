using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>A trigger below the level. The hero loses, the gift goes home, enemies disappear.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) { health.Kill(); return; }
            var gift = other.GetComponentInParent<Gift>();
            if (gift != null) { gift.ResetHome(); return; }
            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null) Destroy(enemy.gameObject);
        }
    }
}

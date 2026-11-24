using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Low gravity: higher jumps and slow falls while the hero is inside.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GravityZone : MonoBehaviour
    {
        [Range(0.1f, 1f)] public float gravityScale = 0.35f;
        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.GravityZoneScale = Mathf.Min(player.GravityZoneScale, gravityScale);
        }
    }
}

using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Wind: pushes the hero (added speed) and the gift (a force). Can blow in gusts.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class WindField : MonoBehaviour
    {
        public Vector2 wind = new Vector2(3f, 0f);
        public float period = 0f;            // 0 = always on
        public float onSeconds = 2f;         // gust length when period > 0
        public float giftForce = 14f;

        public bool Active => period <= 0f || (Time.time % period) < onSeconds;

        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!Active) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) { player.ExternalVelocity += wind; return; }
            var gift = other.GetComponentInParent<Gift>();
            // the wind only carries the gift while it flies; a gift waiting on the seesaw stays put
            if (gift != null && gift.Body != null && gift.InFlight) gift.Body.AddForce(wind.normalized * giftForce * gift.Body.mass);
        }
    }
}

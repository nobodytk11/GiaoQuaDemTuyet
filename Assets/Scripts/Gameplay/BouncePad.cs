using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Spring: throws the hero up when landed on from above.</summary>
    public class BouncePad : MonoBehaviour
    {
        public float bounceSpeed = 17f;
        public Sprite upSprite, downSprite;

        void OnCollisionEnter2D(Collision2D c)
        {
            var player = c.collider.GetComponentInParent<PlayerController>();
            if (player == null || player.Velocity.y > 1f) return;
            if (player.transform.position.y < c.otherCollider.bounds.max.y - 0.3f) return;   // landed from above only
            player.Bounce(bounceSpeed);
            GameAudio.Play("powerup", 0.7f);
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && downSprite != null && upSprite != null) StartCoroutine(Pop(sr));
        }

        IEnumerator Pop(SpriteRenderer sr)
        {
            sr.sprite = downSprite;
            yield return new WaitForSeconds(0.2f);
            sr.sprite = upSprite;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>A "?" box: bump it from below and it pays out once.</summary>
    public class BonusBox : MonoBehaviour
    {
        public GameObject content;           // inactive child (a Collectible); activated when opened
        public int coinReward = 1;
        public Sprite emptySprite;
        public bool Opened { get; private set; }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (Opened) return;
            var player = c.collider.GetComponentInParent<PlayerController>();
            if (player == null) return;
            bool fromBelow = player.transform.position.y + 0.9f <= c.otherCollider.bounds.min.y + 0.35f && player.Velocity.y >= -0.5f;
            if (!fromBelow) return;
            Open();
        }

        public void Open()
        {
            if (Opened) return;
            Opened = true;
            SaveService.CountBox();
            GameAudio.Play("bonus_box");
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && emptySprite != null) sr.sprite = emptySprite;
            if (content != null)
            {
                content.SetActive(true);
                content.transform.position = transform.position + Vector3.up * 1f;
            }
            else
            {
                GameSession.CoinsThisRun += coinReward;
                SaveService.AddCoins(coinReward);
            }
        }
    }
}

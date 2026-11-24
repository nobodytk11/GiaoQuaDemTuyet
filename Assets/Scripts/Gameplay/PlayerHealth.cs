using System;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Hearts, damage with a short invulnerable time, shield, and death.</summary>
    public class PlayerHealth : MonoBehaviour
    {
        public int maxHearts = 3;
        public float invulnerableSeconds = 1.5f;
        public Vector2 knockback = new Vector2(5f, 7f);

        public int Hearts { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        public event Action<int> HeartsChanged;
        public event Action<Vector2> Damaged;
        public event Action Died;

        float invulnerableUntil;
        PlayerController controller;
        PlayerPowerUps powerUps;
        SpriteRenderer sprite;

        void Awake()
        {
            Hearts = maxHearts;
            controller = GetComponent<PlayerController>();
            powerUps = GetComponent<PlayerPowerUps>();
            sprite = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (sprite == null || IsDead) return;
            // blink while invulnerable
            sprite.enabled = !IsInvulnerable || Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
        }

        /// <summary>Returns true when the hit counted.</summary>
        public bool TakeHit(Vector2 sourcePosition, int amount = 1)
        {
            if (IsDead || IsInvulnerable) return false;
            float dir = transform.position.x >= sourcePosition.x ? 1f : -1f;
            if (powerUps != null && powerUps.ConsumeShield())
            {
                invulnerableUntil = Time.time + 0.8f;
                GameAudio.Play("shield");
                controller?.Knockback(new Vector2(dir * knockback.x * 0.6f, knockback.y * 0.6f), 0.15f);
                return false;
            }
            Hearts = Mathf.Max(0, Hearts - amount);
            invulnerableUntil = Time.time + invulnerableSeconds;
            GameAudio.Play("hurt");
            Damaged?.Invoke(sourcePosition);
            HeartsChanged?.Invoke(Hearts);
            controller?.Knockback(new Vector2(dir * knockback.x, knockback.y));
            if (Hearts == 0) Die();
            return true;
        }

        public void Kill()
        {
            if (IsDead) return;
            Hearts = 0;
            HeartsChanged?.Invoke(0);
            Die();
        }

        void Die()
        {
            IsDead = true;
            GameSession.DiedThisRun = true;
            GameAudio.Play("player_die");
            if (controller != null) controller.Stop();
            if (sprite != null) sprite.enabled = true;
            Died?.Invoke();
        }

        public bool Heal(int n = 1)
        {
            if (IsDead || Hearts >= maxHearts) return false;
            Hearts = Mathf.Min(maxHearts, Hearts + n);
            HeartsChanged?.Invoke(Hearts);
            GameAudio.Play("heal");
            return true;
        }

        public void ResetFull()
        {
            Hearts = maxHearts;
            IsDead = false;
            invulnerableUntil = Time.time + 1.2f;
            if (controller != null) controller.Controllable = true;
            HeartsChanged?.Invoke(Hearts);
        }
    }
}

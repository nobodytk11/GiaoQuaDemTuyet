using System;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Shield (blocks one hit) and the boots boost (higher jump for a while).</summary>
    public class PlayerPowerUps : MonoBehaviour
    {
        public float bootsSeconds = 12f;
        public float bootsJumpBonus = 0.2f;

        public bool HasShield { get; private set; }
        public float BootsUntil { get; private set; }
        public bool BootsActive => Time.time < BootsUntil;
        public float JumpBoostMultiplier => BootsActive ? 1f + bootsJumpBonus : 1f;
        public event Action Changed;

        public void GiveShield() { HasShield = true; GameAudio.Play("powerup"); Changed?.Invoke(); }
        public void GiveBoots() { BootsUntil = Time.time + bootsSeconds; GameAudio.Play("powerup"); Changed?.Invoke(); }

        public bool ConsumeShield()
        {
            if (!HasShield) return false;
            HasShield = false;
            Changed?.Invoke();
            return true;
        }

        public void ClearAll() { HasShield = false; BootsUntil = 0f; Changed?.Invoke(); }
    }
}

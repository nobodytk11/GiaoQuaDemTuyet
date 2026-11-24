using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Throws snowballs with J. At most three flying at once.</summary>
    public class PlayerShooter : MonoBehaviour
    {
        public Sprite snowballSprite;
        public float cooldown = 0.35f;
        public int maxActive = 3;
        public Vector2 muzzle = new Vector2(0.45f, 0.8f);
        public Vector2 speed = new Vector2(11f, 3.5f);

        PlayerController controller;
        float nextAllowed;
        readonly List<Snowball> active = new List<Snowball>();

        void Awake() { controller = GetComponent<PlayerController>(); }

        void Update()
        {
            if (controller == null || controller.Input == null || !controller.Controllable) return;
            if (controller.Input.ThrowDown) TryThrow();
        }

        public bool TryThrow()
        {
            active.RemoveAll(s => s == null);
            if (Time.time < nextAllowed || active.Count >= maxActive) return false;
            nextAllowed = Time.time + cooldown;
            int dir = controller != null ? controller.Facing : 1;
            Vector2 pos = (Vector2)transform.position + new Vector2(muzzle.x * dir, muzzle.y);
            var vel = new Vector2(speed.x * dir, speed.y) + (controller != null ? new Vector2(controller.Velocity.x * 0.3f, 0f) : Vector2.zero);
            active.Add(Snowball.Spawn(pos, vel, snowballSprite));
            GameAudio.Play("snowball_throw");
            return true;
        }
    }
}

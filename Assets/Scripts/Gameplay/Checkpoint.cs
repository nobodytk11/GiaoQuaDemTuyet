using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Saves the hero's position when touched.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        public Sprite offSprite, onSprite;
        public bool Active { get; private set; }

        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Active || other.GetComponentInParent<PlayerController>() == null) return;
            Active = true;
            var level = LevelController.Instance;
            if (level != null) level.SetCheckpoint(transform.position);
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && onSprite != null) sr.sprite = onSprite;
            GameAudio.Play("checkpoint");
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>The chimney's open top. A gift that falls in wins the level.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZone : MonoBehaviour
    {
        public static event Action GiftDelivered;
        public bool Reached { get; private set; }

        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Reached) return;
            var gift = other.GetComponentInParent<Gift>();
            if (gift == null) return;
            Reached = true;
            gift.Deliver();
            GameAudio.Play("gift_delivered");
            GiftDelivered?.Invoke();
            var level = LevelController.Instance;
            if (level != null) level.Win();
        }
    }
}

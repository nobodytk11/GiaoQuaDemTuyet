using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        public CollectibleKind kind = CollectibleKind.Coin;
        public int amount = 1;
        public bool Collected { get; private set; }

        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
        void OnTriggerStay2D(Collider2D other) { TryCollect(other); }   // a heart picked up later, once the hero is hurt

        void TryCollect(Collider2D other)
        {
            if (Collected) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            switch (kind)
            {
                case CollectibleKind.Coin:
                    GameSession.CoinsThisRun += amount;
                    SaveService.AddCoins(amount);
                    GameAudio.Play("coin");
                    break;
                case CollectibleKind.Heart:
                    var health = player.GetComponent<PlayerHealth>();
                    if (health == null || !health.Heal(1)) return;     // full hearts: leave it
                    break;
                case CollectibleKind.Boots: player.GetComponent<PlayerPowerUps>()?.GiveBoots(); break;
                case CollectibleKind.Shield: player.GetComponent<PlayerPowerUps>()?.GiveShield(); break;
            }
            Collected = true;
            Destroy(gameObject);
        }
    }
}

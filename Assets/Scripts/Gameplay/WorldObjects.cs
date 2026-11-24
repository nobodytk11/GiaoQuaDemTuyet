using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Put on a ground collider: the hero accelerates and stops slowly here.</summary>
    public class IceSurface : MonoBehaviour
    {
        [Range(0.02f, 1f)] public float frictionScale = 0.15f;
    }

    /// <summary>Moves between two points. The hero standing on it is carried along.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        public Vector2 offset = new Vector2(3f, 0f);
        public float speed = 1.5f;
        public float pause = 0.3f;

        Rigidbody2D rb;
        Vector2 a, b;
        bool towardB = true;
        float resumeAt;

        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            a = transform.position;
            b = a + offset;
        }

        void FixedUpdate()
        {
            if (Time.time < resumeAt) { rb.linearVelocity = Vector2.zero; return; }
            Vector2 target = towardB ? b : a;
            Vector2 to = target - rb.position;
            float step = speed * Time.fixedDeltaTime;
            if (to.magnitude <= step)
            {
                rb.position = target;
                rb.linearVelocity = Vector2.zero;
                towardB = !towardB;
                resumeAt = Time.time + pause;
            }
            else rb.linearVelocity = to.normalized * speed;
        }

        void OnDrawGizmosSelected()
        {
            Vector2 start = Application.isPlaying ? a : (Vector2)transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, start + offset);
        }
    }

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
            if (gift != null && gift.Body != null) gift.Body.AddForce(wind.normalized * giftForce * gift.Body.mass);
        }
    }

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

    /// <summary>Spikes and similar: hurts the hero while touching.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        public int damage = 1;
        void Awake() { GetComponent<Collider2D>().isTrigger = true; }
        void OnTriggerEnter2D(Collider2D other) { Hit(other); }
        void OnTriggerStay2D(Collider2D other) { Hit(other); }

        void Hit(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeHit(transform.position, damage);
        }
    }

    /// <summary>A trigger below the level. The hero loses, the gift goes home, enemies disappear.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) { health.Kill(); return; }
            var gift = other.GetComponentInParent<Gift>();
            if (gift != null) { gift.ResetHome(); return; }
            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null) Destroy(enemy.gameObject);
        }
    }

    public enum CollectibleKind { Coin, Heart, Boots, Shield }

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

    /// <summary>A platform that crumbles about a second after the hero stands on it, then comes back.</summary>
    public class MeltingPlatform : MonoBehaviour
    {
        public float meltDelay = 1f;
        public float returnAfter = 3f;
        bool running;

        void OnCollisionEnter2D(Collision2D c)
        {
            if (running || c.collider.GetComponentInParent<PlayerController>() == null) return;
            var hero = c.collider.GetComponentInParent<PlayerController>();
            if (hero.transform.position.y < c.otherCollider.bounds.max.y - 0.2f) return;   // only when standing on top
            StartCoroutine(Melt());
        }

        IEnumerator Melt()
        {
            running = true;
            var sr = GetComponentInChildren<SpriteRenderer>();
            var col = GetComponent<Collider2D>();
            Vector3 home = transform.position;
            for (float t = 0f; t < meltDelay; t += Time.deltaTime)
            {
                transform.position = home + new Vector3(Mathf.Sin(t * 60f) * 0.04f, 0f, 0f);
                yield return null;
            }
            transform.position = home;
            if (col != null) col.enabled = false;
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(returnAfter);
            if (col != null) col.enabled = true;
            if (sr != null) sr.enabled = true;
            running = false;
        }
    }

    /// <summary>Hangs still until the hero walks under it, then falls.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FallingIcicle : MonoBehaviour
    {
        public float triggerHalfWidth = 0.9f;
        public float triggerDepth = 8f;
        public int damage = 1;
        Rigidbody2D rb;
        bool falling;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        void FixedUpdate()
        {
            if (falling) return;
            var p = PlayerController.Current;
            if (p == null) return;
            float dx = Mathf.Abs(p.transform.position.x - rb.position.x);
            float below = rb.position.y - p.transform.position.y;
            if (dx < triggerHalfWidth && below > 0f && below < triggerDepth)
            {
                falling = true;
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 3f;
            }
        }

        void OnCollisionEnter2D(Collision2D c) { Hit(c.collider); }
        void OnTriggerEnter2D(Collider2D other) { Hit(other); }

        void Hit(Collider2D other)
        {
            if (!falling) return;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeHit(transform.position, damage);
            if (health != null || other.gameObject.layer == LayerMask.NameToLayer("Ground")) Destroy(gameObject);
        }
    }
}

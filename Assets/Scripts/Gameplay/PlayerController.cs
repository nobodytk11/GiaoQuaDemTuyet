using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>
    /// Side-view hero driven by forces. Sideways: a force pushes the velocity toward the wanted speed (strong on ground,
    /// weak in the air, weak on ice). Jump: an impulse. Wind, moving platforms and knock-back are added on top
    /// instead of being overwritten, so ice, wind and platforms all work together.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Current { get; private set; }

        [Header("Run")]
        public float maxSpeed = 6.5f;
        public float groundAccel = 55f;
        public float airAccel = 30f;
        public float duckSpeedScale = 0.4f;

        [Header("Jump (heights in world units, 1 unit = 1 tile)")]
        public float jumpHeight = 2.3f;
        public float baseGravityScale = 3f;
        public float fallMultiplier = 1.3f;
        public float jumpCutMultiplier = 2.6f;
        public float coyoteTime = 0.10f;
        public float jumpBuffer = 0.12f;
        public float maxFallSpeed = 24f;

        [Header("Skin")]
        public bool useSaveForSkin = true;

        public IPlayerInput Input { get; set; } = new CombinedInput();
        public bool Grounded { get; private set; }
        public bool Ducking { get; private set; }
        public bool Controllable { get; set; } = true;
        public int Facing { get; private set; } = 1;
        public Rigidbody2D Body => rb;
        public Vector2 Velocity => rb.linearVelocity;
        /// <summary>Velocity at the start of the last physics step, i.e. just before any contact in that step.</summary>
        public Vector2 PreStepVelocity { get; private set; }

        /// <summary>Set by wind zones every physics step; added to the wanted sideways speed.</summary>
        public Vector2 ExternalVelocity;
        /// <summary>Set by gravity zones every physics step (1 = normal).</summary>
        public float GravityZoneScale = 1f;

        public float JumpMultiplier
        {
            get
            {
                float m = useSaveForSkin ? SaveService.JumpMultiplier() : 1f;
                if (powerUps != null) m *= powerUps.JumpBoostMultiplier;
                return m;
            }
        }

        Rigidbody2D rb;
        BoxCollider2D box;
        Animator animator;
        SpriteRenderer sprite;
        PlayerPowerUps powerUps;
        LayerMask groundMask;
        readonly Collider2D[] hits = new Collider2D[8];
        float coyoteTimer, bufferTimer, knockbackTimer;
        Vector2 standSize, standOffset;
        bool jumpedUp, bouncing;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            animator = GetComponent<Animator>();
            sprite = GetComponent<SpriteRenderer>();
            powerUps = GetComponent<PlayerPowerUps>();
            groundMask = LayerMask.GetMask("Ground");
            rb.freezeRotation = true;
            rb.gravityScale = baseGravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (box != null) { standSize = box.size; standOffset = box.offset; }
            Current = this;
        }

        void Start()
        {
            if (useSaveForSkin) rb.mass = ShopCatalog.Skin(SaveService.Data.skin).Mass;
        }

        void OnDestroy() { if (Current == this) Current = null; }

        /// <summary>Asks for a jump from inside the physics step (used by the autoplay bot).</summary>
        public void QueueJump() { if (Controllable) bufferTimer = jumpBuffer; }

        void Update()
        {
            if (Input == null) return;
            if (Controllable && Input.JumpDown) bufferTimer = jumpBuffer;
            if (Controllable && Mathf.Abs(Input.Move) > 0.1f)
            {
                Facing = Input.Move > 0 ? 1 : -1;
                if (sprite != null) sprite.flipX = Facing < 0;
            }
            VirtualInput.EndFrame();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            PreStepVelocity = rb.linearVelocity;
            Vector2 platformVelocity = Vector2.zero;
            float friction = 1f;
            Grounded = CheckGround(out platformVelocity, out friction);

            coyoteTimer = Grounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - dt);
            bufferTimer = Mathf.Max(0f, bufferTimer - dt);
            knockbackTimer = Mathf.Max(0f, knockbackTimer - dt);

            bool wantDuck = Controllable && Grounded && Input != null && Input.DuckHeld;
            SetDuck(wantDuck);

            // ---- sideways: force toward the wanted speed ----
            float move = Controllable && Input != null ? Input.Move : 0f;
            float wanted = move * maxSpeed * (Ducking ? duckSpeedScale : 1f);
            float target = platformVelocity.x + wanted + ExternalVelocity.x;
            float accel = (Grounded ? groundAccel * friction : airAccel) * (knockbackTimer > 0f ? 0.2f : 1f);
            float dv = Mathf.Clamp(target - rb.linearVelocity.x, -accel * dt, accel * dt);
            rb.AddForce(new Vector2(rb.mass * dv / dt, 0f));

            // ---- jump: an impulse ----
            if (Controllable && bufferTimer > 0f && coyoteTimer > 0f && !Ducking)
            {
                // In a low-gravity zone the same jump goes up to 1.8 times higher and floats longer.
                float z = GravityZoneScale;
                float boost = 1f + 0.8f * (1f - z) / 0.65f;
                float g = Mathf.Abs(Physics2D.gravity.y) * baseGravityScale * z;
                float v0 = Mathf.Sqrt(2f * g * jumpHeight * boost) * JumpMultiplier;
                float dvy = v0 - rb.linearVelocity.y;
                rb.AddForce(new Vector2(0f, rb.mass * dvy), ForceMode2D.Impulse);
                bufferTimer = 0f;
                coyoteTimer = 0f;
                jumpedUp = true;
                GameAudio.Play("jump");
            }

            // ---- gravity: short hop when the button is released, snappier fall ----
            float scale = baseGravityScale * GravityZoneScale;
            Vector2 v = rb.linearVelocity;
            if (v.y < -0.1f) { scale *= fallMultiplier; jumpedUp = false; bouncing = false; }
            else if (v.y > 0.1f && !bouncing && (Input == null || !Input.JumpHeld || !Controllable)) scale *= jumpCutMultiplier;
            rb.gravityScale = scale;
            if (v.y < -maxFallSpeed) rb.linearVelocity = new Vector2(v.x, -maxFallSpeed);

            UpdateAnimator(platformVelocity);
            ExternalVelocity = Vector2.zero;
            GravityZoneScale = 1f;
        }

        bool CheckGround(out Vector2 platformVelocity, out float friction)
        {
            platformVelocity = Vector2.zero;
            friction = 1f;
            Vector2 size = new Vector2((box != null ? standSize.x : 0.6f) * 0.9f, 0.1f);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = groundMask, useTriggers = false };
            int n = Physics2D.OverlapBox(rb.position + new Vector2(0f, -0.04f), size, 0f, filter, hits);
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var c = hits[i];
                if (c.isTrigger || c.attachedRigidbody == rb) continue;
                found = true;
                var plat = c.GetComponentInParent<MovingPlatform>();
                if (plat != null) platformVelocity = plat.Velocity;
                var ice = c.GetComponentInParent<IceSurface>();
                if (ice != null) friction = Mathf.Min(friction, ice.frictionScale);
            }
            return found;
        }

        void SetDuck(bool duck)
        {
            if (box == null || duck == Ducking) return;
            if (!duck && !HasHeadroom()) return;
            Ducking = duck;
            box.size = duck ? new Vector2(standSize.x, standSize.y * 0.65f) : standSize;
            box.offset = duck ? new Vector2(standOffset.x, standOffset.y - standSize.y * 0.175f) : standOffset;
        }

        bool HasHeadroom()
        {
            // the space between the crouching top and the standing top must be free
            float crouchTop = standOffset.y + standSize.y * 0.5f - standSize.y * 0.35f;
            float standTop = standOffset.y + standSize.y * 0.5f;
            var pos = rb.position + new Vector2(0f, (crouchTop + standTop) / 2f);
            var size = new Vector2(standSize.x * 0.9f, Mathf.Max(0.05f, standTop - crouchTop - 0.04f));
            return Physics2D.OverlapBox(pos, size, 0f, groundMask) == null;
        }

        void UpdateAnimator(Vector2 platformVelocity)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x - platformVelocity.x));
            animator.SetBool("Grounded", Grounded);
            animator.SetBool("Ducking", Ducking);
        }

        // ---------- called by other scripts ----------
        public void Knockback(Vector2 velocity, float seconds = 0.3f)
        {
            rb.linearVelocity = velocity / Mathf.Sqrt(rb.mass);
            knockbackTimer = seconds;
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Hurt");
        }

        public void Bounce(float speed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, speed);
            coyoteTimer = 0f;
            bouncing = true;                 // a spring or stomp throws the hero up whether or not jump is held
        }

        public bool IsStompingOn(Collider2D enemy)
        {
            return rb.position.y >= enemy.bounds.center.y - 0.05f && rb.linearVelocity.y <= 0.5f;
        }

        public void Teleport(Vector2 feetPosition)
        {
            rb.position = feetPosition;
            transform.position = feetPosition;
            rb.linearVelocity = Vector2.zero;
            knockbackTimer = 0f;
            coyoteTimer = bufferTimer = 0f;
        }

        public void Stop()
        {
            Controllable = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }
}

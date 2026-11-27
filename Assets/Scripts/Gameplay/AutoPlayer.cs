using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>
    /// Plays the level by itself ("Autoplay"). It reads the same objects a player sees: ground, pits, enemies, moving platforms,
    /// the seesaw, springs and wind. It runs right, jumps over trouble, rides platforms, and delivers the gift;
    /// when it loses it retries from the checkpoint.
    /// It thinks in FixedUpdate, before the hero moves, so a slow phone that draws few frames still sees every pit in time.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AutoPlayer : MonoBehaviour, IPlayerInput
    {
        enum Stage { Run, WaitPlatform, Ride, SeesawJump, ToSpring, WaitCalm, SpringJump, Bounce, Delivered }

        float move;
        bool jumpHeld;
        public float Move => move;
        public bool JumpDown => false;     // jumps go straight to the hero's jump buffer, see Press()
        public bool JumpHeld => jumpHeld;
        public bool ThrowDown => false;
        public bool DuckHeld => false;

        PlayerController hero;
        LevelController level;
        Seesaw seesaw;
        BouncePad spring;
        MovingPlatform[] movers;
        WindField[] gusts;
        readonly Dictionary<MovingPlatform, float> homeX = new Dictionary<MovingPlatform, float>();
        readonly HashSet<MovingPlatform> crossed = new HashSet<MovingPlatform>();
        MovingPlatform current;
        bool boarded, crossGateDone;
        Stage stage = Stage.Run;
        float lostFor, wonFor, idleSince;
        int ground;

        public string StageName => stage.ToString();
        public string DebugInfo => $"stage={stage} platformX={(current != null ? current.transform.position.x : -1f):0.0} home={(current != null ? homeX[current] : -1f):0.0} grounded={(hero != null && hero.Grounded)} move={move} heroY={(hero != null ? hero.transform.position.y : 0f):0.00}";

        void Start()
        {
            hero = GetComponent<PlayerController>();
            level = LevelController.Instance;
            seesaw = FindFirstObjectByType<Seesaw>();
            movers = FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);
            gusts = FindObjectsByType<WindField>(FindObjectsSortMode.None);
            ground = LayerMask.GetMask("Ground");
            foreach (var m in movers) homeX[m] = m.transform.position.x;
            if (seesaw != null)
            {
                float best = float.MaxValue;
                foreach (var b in FindObjectsByType<BouncePad>(FindObjectsSortMode.None))
                {
                    float d = b.transform.position.x - seesaw.transform.position.x;
                    if (d > 0f && d < best) { best = d; spring = b; }
                }
            }
        }

        void Press() { hero.QueueJump(); jumpHeld = true; }

        void FixedUpdate()
        {
            if (hero == null || level == null) return;
            if (level.State == LevelState.Lost)
            {
                move = 0f; jumpHeld = false;
                lostFor += Time.fixedDeltaTime;
                if (lostFor > 1.4f) { lostFor = 0f; stage = Stage.Run; crossGateDone = false; level.Retry(); }
                return;
            }
            lostFor = 0f;
            if (level.State == LevelState.Won)
            {
                move = 0f; jumpHeld = false;
                wonFor += Time.fixedDeltaTime;
                if (wonFor > 3f) { wonFor = -999f; level.NextLevel(); }
                return;
            }
            if (level.State == LevelState.Paused) return;
            Think();
        }

        // ---------------------------------------------------------------- sensing
        bool PitAhead(Vector2 p) => !Physics2D.Raycast(p + new Vector2(0.75f, 0.5f), Vector2.down, 1.6f, ground);
        bool WallAhead(Vector2 p) => Physics2D.Raycast(p + new Vector2(0f, 0.08f), Vector2.right, 0.9f, ground);    // low enough to see a step of 0.1
        bool DangerAhead(Vector2 p) =>
            Physics2D.OverlapBox(p + new Vector2(2.1f, 0.5f), new Vector2(2.6f, 0.9f), 0f, LayerMask.GetMask("Hazard", "Enemy")) != null;

        float stuckX, stuckSince;

        void RunStep()
        {
            var p = (Vector2)hero.transform.position;
            move = 1f;
            if (!hero.Grounded) { jumpHeld = hero.Velocity.y > 0.5f; return; }
            if (PitAhead(p) || WallAhead(p) || DangerAhead(p)) { Press(); return; }
            // pushing against something it cannot see: hop
            if (Mathf.Abs(p.x - stuckX) > 0.25f) { stuckX = p.x; stuckSince = Time.time; }
            else if (Time.time - stuckSince > 1.2f) { stuckSince = Time.time; Press(); }
        }

        bool Calm()
        {
            foreach (var g in gusts)
            {
                if (g.period <= 0f) continue;
                float phase = Time.time % g.period;
                if (g.Active || phase < g.onSeconds + 0.1f || phase > g.onSeconds + 0.5f) return false;
            }
            return true;
        }

        /// <summary>Where to wait before the spring: on the ground just right of the seesaw pad, near the spring.</summary>
        float StandX() => Mathf.Max(seesaw.transform.position.x + 1.2f, spring.transform.position.x - 2.0f);

        bool HasGusts() { foreach (var g in gusts) if (g.period > 0f) return true; return false; }

        // ---------------------------------------------------------------- decisions
        void Think()
        {
            var p = (Vector2)hero.transform.position;
            bool grounded = hero.Grounded;

            switch (stage)
            {
                case Stage.Run:
                    if (seesaw != null && !seesaw.gift.Delivered && p.x > seesaw.transform.position.x - 5f && p.x < seesaw.transform.position.x + 6f)
                    {
                        stage = seesaw.minImpact <= 9f || spring == null ? Stage.SeesawJump : Stage.ToSpring;
                        break;
                    }
                    var next = NextPlatform(p);
                    if (next != null && grounded && PitAhead(p)) { current = next; boarded = false; stage = Stage.WaitPlatform; move = 0f; break; }
                    RunStep();
                    break;

                case Stage.WaitPlatform:
                    move = 0f;
                    if (current == null) { stage = Stage.Run; break; }
                    if (current.transform.position.x < homeX[current] + 0.9f && grounded)
                    {
                        move = 1f; Press(); stage = Stage.Ride;
                    }
                    break;

                case Stage.Ride:
                    if (current == null) { stage = Stage.Run; break; }
                    bool onIt = grounded && Mathf.Abs(p.x - current.transform.position.x) < 2.2f && p.y > current.transform.position.y;
                    if (onIt) boarded = true;
                    if (onIt && current.transform.position.x < homeX[current] + 3.2f) { move = 0f; jumpHeld = false; }
                    else if (onIt) { move = 1f; Press(); }
                    else if (grounded && boarded && p.x > current.transform.position.x + 2.3f) { crossed.Add(current); current = null; stage = Stage.Run; }
                    else if (!grounded)
                    {
                        // before landing on the platform steer toward its middle; after it, run for the far ground
                        move = boarded ? 1f : Mathf.Clamp((current.transform.position.x - p.x) * 3f, -1f, 1f);
                        jumpHeld = hero.Velocity.y > 0.5f;
                    }
                    else if (!boarded && p.x > current.transform.position.x + 2.3f) stage = Stage.Run;        // missed it: try the usual way
                    break;

                case Stage.SeesawJump:
                {
                    float padX = seesaw.transform.position.x;
                    if (seesaw.gift.Delivered) { stage = Stage.Delivered; break; }
                    if (!grounded)
                    {
                        move = Mathf.Clamp((padX - p.x) * 3f, -1f, 1f);
                        jumpHeld = hero.Velocity.y > 0.5f;
                        break;
                    }
                    bool onPad = Mathf.Abs(p.x - padX) < 0.8f;
                    if (onPad) { move = 0f; if (seesaw.gift.InFlight) idleSince = Time.time; else if (Time.time - idleSince > 3f) { stage = Stage.Run; } break; }
                    if (p.x < padX - 3.4f) { move = 1f; break; }                 // walk up to the seesaw
                    move = Mathf.Clamp((padX - p.x) * 3f, -1f, 1f);
                    Press();                                                      // hop onto the rest, then onto the pad
                    break;
                }

                case Stage.ToSpring:
                {
                    if (seesaw.gift.Delivered) { stage = Stage.Delivered; break; }
                    if (spring == null) { stage = Stage.SeesawJump; break; }
                    float standX = StandX();
                    if (p.x < seesaw.transform.position.x + 0.9f)                               // first get past the seesaw
                    {
                        if (HasGusts() && !crossGateDone)
                        {
                            // with gusts around, wait outside the wind for a calm moment, then cross in one go
                            float gateX = seesaw.transform.position.x - 6.5f;
                            if (p.x < gateX - 0.5f) { RunStep(); break; }
                            move = Mathf.Clamp((gateX - p.x) * 2.5f, -1f, 1f);
                            jumpHeld = false;
                            if (Calm()) crossGateDone = true;
                            break;
                        }
                        RunStep();
                        break;
                    }
                    // walk to the waiting spot with a small correction loop, so wind or momentum cannot make it overshoot
                    move = Mathf.Clamp((standX - p.x) * 2.5f, -1f, 1f);
                    jumpHeld = false;
                    if (grounded && Mathf.Abs(standX - p.x) < 0.3f && Mathf.Abs(hero.Velocity.x) < 1.2f)
                    {
                        move = 0f;
                        stage = HasGusts() ? Stage.WaitCalm : Stage.SpringJump;
                    }
                    break;
                }

                case Stage.WaitCalm:
                {
                    float standX = StandX();
                    move = Mathf.Clamp((standX - p.x) * 2.5f, -1f, 1f);       // hold the spot even while the wind pushes
                    if (Calm() && Mathf.Abs(standX - p.x) < 0.6f) stage = Stage.SpringJump;
                    break;
                }

                case Stage.SpringJump:
                {
                    float sx = spring.transform.position.x;
                    if (!grounded)
                    {
                        move = Mathf.Clamp((sx - p.x) * 3f, -1f, 1f);
                        jumpHeld = hero.Velocity.y > 0.5f;
                        if (hero.Velocity.y > 8f && p.y > spring.transform.position.y + 0.8f) stage = Stage.Bounce;
                        break;
                    }
                    move = Mathf.Clamp((sx - 1.2f - p.x) * 2f, 0f, 1f);
                    if (p.x >= sx - 1.7f) { move = 1f; Press(); }
                    break;
                }

                case Stage.Bounce:
                {
                    float padX = seesaw.transform.position.x;
                    move = Mathf.Clamp((padX - p.x) * 2.5f, -1f, 1f);
                    jumpHeld = false;
                    if (grounded)
                    {
                        move = 0f;
                        idleSince = Time.time;
                        stage = Stage.Delivered;
                    }
                    break;
                }

                case Stage.Delivered:
                    move = 0f;
                    // the gift may miss (wind): once it is back home, try again
                    if (seesaw != null && seesaw.gift.AtHome && !seesaw.gift.InFlight && Time.time - idleSince > 4f)
                    {
                        idleSince = Time.time;
                        crossGateDone = false;
                        stage = seesaw.minImpact <= 9f || spring == null ? Stage.SeesawJump : Stage.ToSpring;
                    }
                    else if (seesaw != null && seesaw.gift.InFlight) idleSince = Time.time;
                    break;
            }
        }

        MovingPlatform NextPlatform(Vector2 p)
        {
            MovingPlatform best = null;
            float bestD = 9f;
            foreach (var m in movers)
            {
                if (m == null || crossed.Contains(m)) continue;
                float d = homeX[m] - p.x;
                if (d > -1f && d < bestD) { bestD = d; best = m; }
            }
            return best;
        }
    }
}

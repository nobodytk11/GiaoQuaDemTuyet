using UnityEngine;
using UnityEngine.InputSystem;

namespace GiaoQua.Gameplay
{
    /// <summary>What the hero needs from a player: one number and four buttons. Tests replace this with a script.</summary>
    public interface IPlayerInput
    {
        float Move { get; }
        bool JumpDown { get; }
        bool JumpHeld { get; }
        bool ThrowDown { get; }
        bool DuckHeld { get; }
    }

    /// <summary>Written by the on-screen buttons (touch). Keyboard users never touch it.</summary>
    public static class VirtualInput
    {
        public static float Move;
        public static bool JumpDown, JumpHeld, ThrowDown, DuckHeld;
        public static void EndFrame() { JumpDown = false; ThrowDown = false; }
        public static void Clear() { Move = 0; JumpDown = JumpHeld = ThrowDown = DuckHeld = false; }
    }

    /// <summary>Keyboard (arrows or A/D, Space/W/Up, J, S/Down) merged with the on-screen buttons.</summary>
    public class CombinedInput : IPlayerInput
    {
        public float Move
        {
            get
            {
                var kb = Keyboard.current;
                float v = VirtualInput.Move;
                if (kb != null)
                {
                    if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) v -= 1f;
                    if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) v += 1f;
                }
                return Mathf.Clamp(v, -1f, 1f);
            }
        }

        public bool JumpDown
        {
            get
            {
                var kb = Keyboard.current;
                return VirtualInput.JumpDown || (kb != null &&
                    (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame));
            }
        }

        public bool JumpHeld
        {
            get
            {
                var kb = Keyboard.current;
                return VirtualInput.JumpHeld || (kb != null &&
                    (kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed));
            }
        }

        public bool ThrowDown
        {
            get
            {
                var kb = Keyboard.current;
                return VirtualInput.ThrowDown || (kb != null && kb.jKey.wasPressedThisFrame);
            }
        }

        public bool DuckHeld
        {
            get
            {
                var kb = Keyboard.current;
                return VirtualInput.DuckHeld || (kb != null && (kb.sKey.isPressed || kb.downArrowKey.isPressed));
            }
        }
    }

    /// <summary>Input driven from code, used by tests and by the autoplay bot.</summary>
    public class ScriptedInput : IPlayerInput
    {
        public float MoveValue;
        public bool JumpHeldValue, DuckValue;
        bool jumpPressed, throwPressed;

        public float Move => MoveValue;
        public bool JumpHeld => JumpHeldValue;
        public bool DuckHeld => DuckValue;
        public bool JumpDown { get { bool v = jumpPressed; jumpPressed = false; return v; } }
        public bool ThrowDown { get { bool v = throwPressed; throwPressed = false; return v; } }

        public void PressJump() { jumpPressed = true; JumpHeldValue = true; }
        public void ReleaseJump() { JumpHeldValue = false; }
        public void PressThrow() { throwPressed = true; }
    }
}

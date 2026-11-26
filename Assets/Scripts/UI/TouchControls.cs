using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GiaoQua.Gameplay;

namespace GiaoQua.UI
{
    /// <summary>On-screen buttons for phones and tablets. They write into VirtualInput, which the hero reads together with the keyboard.</summary>
    public class TouchControls : MonoBehaviour
    {
        public static bool ShouldShow => Application.isMobilePlatform || Touchscreen.current != null;
        GameObject root;

        public void Build(Transform canvas)
        {
            root = UIKit.Stretch(canvas, "TouchControls").gameObject;
            Hold(root.transform, "←", new Vector2(0f, 0f), new Vector2(40f, 40f), v => VirtualInput.Move = v ? -1f : (VirtualInput.Move < 0 ? 0f : VirtualInput.Move));
            Hold(root.transform, "→", new Vector2(0f, 0f), new Vector2(190f, 40f), v => VirtualInput.Move = v ? 1f : (VirtualInput.Move > 0 ? 0f : VirtualInput.Move));
            Hold(root.transform, "Nhảy", new Vector2(1f, 0f), new Vector2(-40f, 40f), v => { VirtualInput.JumpHeld = v; if (v) VirtualInput.JumpDown = true; }, new Vector2(150f, 130f), 30);
            Hold(root.transform, "Ném", new Vector2(1f, 0f), new Vector2(-210f, 40f), v => { if (v) VirtualInput.ThrowDown = true; }, new Vector2(120f, 110f), 28);
            Hold(root.transform, "↓", new Vector2(0f, 0f), new Vector2(340f, 40f), v => VirtualInput.DuckHeld = v);
            root.SetActive(ShouldShow);
        }

        void Update()
        {
            if (root != null && !root.activeSelf && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) root.SetActive(true);
        }

        static void Hold(Transform parent, string label, Vector2 anchor, Vector2 pos, System.Action<bool> onHold, Vector2? size = null, int font = 44)
        {
            var sz = size ?? new Vector2(130f, 130f);
            var b = UIKit.MakeButton(parent, label, ButtonStyle.Blue, anchor, new Vector2(pos.x * (anchor.x > 0 ? 1 : 1), pos.y), sz, null, font);
            var rt = (RectTransform)b.transform;
            rt.pivot = anchor;
            var trigger = b.gameObject.AddComponent<EventTrigger>();
            void Add(EventTriggerType type, bool value)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(_ => onHold(value));
                trigger.triggers.Add(entry);
            }
            Add(EventTriggerType.PointerDown, true);
            Add(EventTriggerType.PointerUp, false);
            Add(EventTriggerType.PointerExit, false);
            var img = b.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.7f);
        }
    }
}

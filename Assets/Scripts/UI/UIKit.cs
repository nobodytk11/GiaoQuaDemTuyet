using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GiaoQua.UI
{
    public enum ButtonStyle { Blue, Green, Red, Grey }

    /// <summary>Builds uGUI objects from code, so every screen looks the same and nothing depends on hand-made scenes.</summary>
    public static class UIKit
    {
        public static readonly Color Gold = new Color(1f, 0.86f, 0.3f);
        public static readonly Color Ink = new Color(0.08f, 0.13f, 0.27f);

        static Font DefaultFont()
        {
            var t = UITheme.Get();
            if (t != null && t.font != null) return t.font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        /// <summary>A rectangle placed by its anchor corner: (0,1) = top-left, (0.5,0.5) = centre, (1,0) = bottom-right...</summary>
        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color? tint = null)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            var theme = UITheme.Get();
            if (theme != null && theme.panel != null) { img.sprite = theme.panel; img.type = Image.Type.Sliced; }
            img.color = tint ?? Color.white;
            return img;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (sprite == null) img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Vector2 anchor, Vector2 pos, Vector2 boxSize,
                                 Color? color = null, TextAnchor align = TextAnchor.MiddleCenter, bool outline = true, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, "Text", anchor, pos, boxSize);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color ?? Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.03f, 0.06f, 0.15f, 0.9f);
                o.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return t;
        }

        public static Button MakeButton(Transform parent, string label, ButtonStyle style, Vector2 anchor, Vector2 pos, Vector2 size,
                                        UnityAction onClick, int fontSize = 30)
        {
            var rt = Rect(parent, "Button " + label, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            var theme = UITheme.Get();
            Sprite sprite = null;
            if (theme != null)
                switch (style)
                {
                    case ButtonStyle.Green: sprite = theme.btnGreen; break;
                    case ButtonStyle.Red: sprite = theme.btnRed; break;
                    case ButtonStyle.Grey: sprite = theme.btnGrey; break;
                    default: sprite = theme.btnBlue; break;
                }
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            else img.color = new Color(0.2f, 0.5f, 0.9f);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.selectedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.7f, 0.9f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            var text = Label(rt, label, fontSize, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), size - new Vector2(16f, 8f), Color.white);
            text.name = "Label";
            return button;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.transform.Find("Label");
            if (t != null) t.GetComponent<Text>().text = text;
        }

        /// <summary>Keyboard and controller users start on this button.</summary>
        public static void Focus(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        public static string Time(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";
    }
}

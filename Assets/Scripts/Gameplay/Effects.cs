using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Sprites made in code (circles and squares) for things that have no picture yet, like tests and snowballs.</summary>
    public static class ProceduralSprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Circle(Color color, int size = 32)
        {
            string key = "c" + color + size;
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01(r - d);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
                }
            tex.Apply();
            return cache[key] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public static Sprite Square(Color color)
        {
            string key = "s" + color;
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            return cache[key] = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }
    }
}

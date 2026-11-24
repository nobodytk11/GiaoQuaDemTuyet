using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>
    /// 2D lights are not used, so every sprite uses an unlit material (loaded from Resources/SpriteUnlit).
    /// Sprites created in code call Apply so they look the same as the ones placed by the level builder.
    /// </summary>
    public static class SpriteMaterial
    {
        static Material cached;
        static bool looked;

        public static Material Get()
        {
            if (!looked) { cached = Resources.Load<Material>("SpriteUnlit"); looked = true; }
            return cached;
        }

        public static void Apply(SpriteRenderer sr)
        {
            var m = Get();
            if (m != null && sr != null) sr.sharedMaterial = m;
        }
    }
}

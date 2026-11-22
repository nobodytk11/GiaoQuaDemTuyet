using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GiaoQua.Tests
{
    public class ArtImportTests
    {
        static Sprite[] SpritesIn(params string[] folders)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", folders))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (s != null) list.Add(s);
            }
            return list.ToArray();
        }

        [Test]
        public void Character_and_enemy_sprites_have_their_pivot_at_the_feet()
        {
            Sprite[] sprites = SpritesIn("Assets/Art/Kenney/PlatformerArtDeluxe/Player", "Assets/Art/Kenney/PlatformerArtDeluxe/Enemies");
            Assert.Greater(sprites.Length, 60, "characters and enemies were not imported");
            foreach (Sprite s in sprites)
            {
                Assert.AreEqual(0f, s.pivot.y, 0.01f, "pivot not at the bottom: " + AssetDatabase.GetAssetPath(s));
                Assert.AreEqual(s.rect.width / 2f, s.pivot.x, 0.6f, "pivot not centred: " + AssetDatabase.GetAssetPath(s));
            }
        }

        [Test]
        public void Tiles_are_exactly_one_world_unit_wide()
        {
            Sprite[] tiles = SpritesIn("Assets/Art/Kenney/PlatformerArtWinter/Tiles", "Assets/Art/Kenney/PlatformerArtBuildings/Tiles");
            Assert.Greater(tiles.Length, 150, "tiles were not imported");
            foreach (Sprite s in tiles)
                Assert.AreEqual(1f, s.bounds.size.x, 0.001f, AssetDatabase.GetAssetPath(s));
        }

        [Test]
        public void The_hero_is_about_one_and_a_third_tiles_tall()
        {
            var stand = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Kenney/PlatformerArtDeluxe/Player/p1_stand.png");
            Assert.IsNotNull(stand);
            Assert.AreEqual(92f / 70f, stand.bounds.size.y, 0.01f);
        }
    }
}

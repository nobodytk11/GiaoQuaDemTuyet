using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GiaoQua.Tests
{
    public class FontTests
    {
        const string FontPath = "Assets/Fonts/Baloo2/Baloo2-VariableFont_wght.ttf";
        const string Vietnamese =
            "aăâbcdđeêghiklmnoôơpqrstuưvxyáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ";

        [Test]
        public void Baloo2_is_imported_as_a_font()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.IsNotNull(font, "font missing or not imported: " + FontPath);
        }

        [Test]
        public void Baloo2_has_every_vietnamese_letter()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.IsNotNull(font);
            foreach (char c in Vietnamese + Vietnamese.ToUpperInvariant())
                Assert.IsTrue(font.HasCharacter(c), "missing glyph: " + c);
        }

        [Test]
        public void Licence_text_ships_next_to_the_font()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Fonts/Baloo2/OFL.txt"));
        }

        [Test]
        public void The_sparkle_shader_is_supported_and_used_by_the_coins()
        {
            var shader = Shader.Find("GiaoQua/SparkleSprite");
            Assert.IsNotNull(shader, "shader missing");
            Assert.IsTrue(shader.isSupported, "shader does not compile on this platform");
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sparkle.mat");
            Assert.IsNotNull(mat);
            Assert.AreEqual(shader, mat.shader);
        }
    }
}

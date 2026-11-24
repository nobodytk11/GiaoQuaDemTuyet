using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GiaoQua.EditorTools;

namespace GiaoQua.Tests
{
    public class HeroAnimationTests
    {
        static AnimationClip Clip(string skin, string name) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>($"{HeroAnimationBuilder.OutputFolder}/{skin}_{name}.anim");

        static Sprite[] SpritesOf(AnimationClip clip)
        {
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            return AnimationUtility.GetObjectReferenceCurve(clip, binding).Select(k => (Sprite)k.value).ToArray();
        }

        [Test]
        public void Every_skin_has_a_controller_with_the_five_states_and_four_parameters()
        {
            foreach (string skin in HeroAnimationBuilder.Skins)
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{HeroAnimationBuilder.OutputFolder}/{skin}.controller");
                Assert.IsNotNull(c, skin);
                var names = c.layers[0].stateMachine.states.Select(s => s.state.name).OrderBy(n => n).ToArray();
                CollectionAssert.AreEqual(new[] { "Air", "Duck", "Hurt", "Idle", "Walk" }, names, skin);
                var ps = c.parameters.ToDictionary(p => p.name, p => p.type);
                Assert.AreEqual(AnimatorControllerParameterType.Float, ps["Speed"]);
                Assert.AreEqual(AnimatorControllerParameterType.Bool, ps["Grounded"]);
                Assert.AreEqual(AnimatorControllerParameterType.Bool, ps["Ducking"]);
                Assert.AreEqual(AnimatorControllerParameterType.Trigger, ps["Hurt"]);
                Assert.AreEqual("Idle", c.layers[0].stateMachine.defaultState.name, skin);
            }
        }

        [Test]
        public void Walk_cycle_uses_all_eleven_frames_in_order_and_loops()
        {
            foreach (string skin in HeroAnimationBuilder.Skins)
            {
                var clip = Clip(skin, "Walk");
                Assert.IsNotNull(clip, skin);
                var names = SpritesOf(clip).Select(s => s.name).ToArray();
                Assert.AreEqual(12, names.Length, "11 frames plus the closing key");
                for (int i = 0; i < 11; i++) Assert.AreEqual($"{skin}_walk{i + 1:00}", names[i]);
                Assert.IsTrue(AnimationUtility.GetAnimationClipSettings(clip).loopTime, skin);
                Assert.AreEqual(11f / 12f, clip.length, 0.03f, skin); // Unity adds up to one 60 fps frame
            }
        }

        [Test]
        public void Single_frame_clips_show_the_right_picture()
        {
            foreach (string skin in HeroAnimationBuilder.Skins)
            {
                Assert.AreEqual($"{skin}_stand", SpritesOf(Clip(skin, "Idle"))[0].name);
                Assert.AreEqual($"{skin}_jump", SpritesOf(Clip(skin, "Air"))[0].name);
                Assert.AreEqual($"{skin}_duck", SpritesOf(Clip(skin, "Duck"))[0].name);
                Assert.AreEqual($"{skin}_hurt", SpritesOf(Clip(skin, "Hurt"))[0].name);
                Assert.Greater(Clip(skin, "Hurt").length, 0.3f, "hurt must last long enough to be seen");
                Assert.IsFalse(AnimationUtility.GetAnimationClipSettings(Clip(skin, "Hurt")).loopTime);
            }
        }
    }
}

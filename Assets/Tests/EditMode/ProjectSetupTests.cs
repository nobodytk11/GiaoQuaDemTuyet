using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GiaoQua.Tests
{
    public class ProjectSetupTests
    {
        [Test]
        public void Unity_version_is_the_one_agreed_in_class()
        {
            Assert.AreEqual("6000.3.16f1", Application.unityVersion);
        }

        [Test]
        public void Ground_layer_and_Gift_tag_exist()
        {
            Assert.AreEqual(6, LayerMask.NameToLayer("Ground"));
            Assert.Contains("Gift", UnityEditorInternal.InternalEditorUtility.tags);
        }

        [Test]
        public void Windows_build_uses_mono_and_a_window()
        {
            Assert.AreEqual(ScriptingImplementation.Mono2x,
                PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone));
            Assert.AreEqual(FullScreenMode.Windowed, PlayerSettings.fullScreenMode);
            Assert.AreEqual("Nhom06", PlayerSettings.companyName);
        }

        [Test]
        public void Every_scene_that_is_built_exists_on_disk()
        {
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                Assert.IsTrue(File.Exists(s.path), "Missing scene in build list: " + s.path);
            Assert.Greater(EditorBuildSettings.scenes.Length, 0, "Build list is empty");
        }
    }
}

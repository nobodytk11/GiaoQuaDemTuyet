using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    public class LevelSceneTests
    {
        static string[] LevelNames()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var l in LevelCatalog.Levels)
                if (File.Exists("Assets/Scenes/" + l.Scene + ".unity")) list.Add(l.Scene);
            return list.ToArray();
        }

        [Test]
        public void Every_level_map_passes_the_validator()
        {
            foreach (var l in LevelCatalog.Levels)
            {
                string path = "Assets/Levels/" + l.Scene + ".txt";
                if (!File.Exists(path)) continue;
                foreach (var issue in LevelValidator.Validate(LevelMap.Parse(File.ReadAllText(path))))
                    Assert.IsFalse(issue.IsError, l.Scene + ": " + issue);
            }
        }

        [Test]
        public void Level_scenes_have_no_missing_scripts()
        {
            var before = EditorSceneManager.GetActiveScene().path;
            foreach (string name in LevelNames())
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity", OpenSceneMode.Additive);
                int missing = 0;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                EditorSceneManager.CloseScene(scene, true);
                Assert.AreEqual(0, missing, name + " has components whose script is missing");
            }
        }

        [Test]
        public void The_map_parser_keeps_empty_rows_and_reads_settings()
        {
            var map = LevelMap.Parse("; comment\n@seesaw 7 55\n|    |\n| S  |\n|####|\n");
            Assert.AreEqual(3, map.Height);
            Assert.AreEqual(4, map.Width);
            Assert.AreEqual('S', map.At(1, 1));
            Assert.IsTrue(map.IsSolid(0, 2));
            Assert.AreEqual(7f, map.Get("seesaw").Value.Float(0));
            Assert.AreEqual(55f, map.Get("seesaw").Value.Float(1));
            Assert.AreEqual(new Vector2(1.5f, 1.5f), map.Center(1, 1));
        }

        [Test]
        public void Merged_rects_cover_every_solid_tile_exactly_once()
        {
            var map = LevelMap.Parse("|  ##  |\n|######|\n|######|\n");
            int tiles = 0;
            foreach (var r in map.MergedRects('#')) tiles += r.width * r.height;
            Assert.AreEqual(2 + 6 + 6, tiles);
            Assert.LessOrEqual(map.MergedRects('#').Count, 2, "a wide block and the bump on it");
        }

        [Test]
        public void The_validator_catches_a_missing_chimney_and_a_wide_pit()
        {
            string text = "|" + new string(' ', 30) + "|\n|" + new string(' ', 30) + "|\n|" + new string(' ', 30) + "|\n|" + new string(' ', 30) + "|\n|" + new string(' ', 30) + "|\n|" + new string(' ', 30) + "|\n|  S                         |\n|#####      ##############|\n";
            var issues = LevelValidator.Validate(LevelMap.Parse(text));
            Assert.IsTrue(issues.Exists(i => i.IsError && i.Message.Contains("ống khói")), "no chimney");
            Assert.IsTrue(issues.Exists(i => i.IsError && i.Message.Contains("Hố")), "pit of 6");
        }
    }
}

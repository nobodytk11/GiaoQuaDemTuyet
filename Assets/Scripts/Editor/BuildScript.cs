using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GiaoQua.EditorTools
{
    /// <summary>
    /// Builds the player from the command line. Scenes come from SceneOrder (first = start scene).
    /// Output goes to Builds/ (ignored by git).
    /// </summary>
    public static class BuildScript
    {
        // Scenes that exist on disk are added to the build in this order; missing ones are skipped.
        static readonly string[] SceneOrder =
        {
            "Assets/Scenes/Hello.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/LevelSelect.unity",
            "Assets/Scenes/Level1.unity",
            "Assets/Scenes/Level2.unity",
            "Assets/Scenes/Level3.unity",
            "Assets/Scenes/Ending.unity",
        };

        public static string[] ScenesToBuild()
        {
            // While the real menu does not exist yet, the hello scene is the start scene.
            bool hasMenu = File.Exists("Assets/Scenes/MainMenu.unity");
            return SceneOrder.Where(File.Exists).Where(p => hasMenu ? !p.EndsWith("Hello.unity") : true).ToArray();
        }

        [MenuItem("Tools/Giao Qua/Cập nhật danh sách scene của build")]
        public static void SyncBuildSettings()
        {
            EditorBuildSettings.scenes = ScenesToBuild().Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            Debug.Log("[GiaoQua] Build scenes: " + string.Join(", ", ScenesToBuild()));
        }

        [MenuItem("Tools/Giao Qua/Build Windows")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/PC/GiaoQuaDemTuyet.exe");
        }

        static void Build(BuildTarget target, string output)
        {
            SyncBuildSettings();
            string[] scenes = ScenesToBuild();
            if (scenes.Length == 0) Fail("No scene found to build.");

            string dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            Debug.Log($"[GiaoQua] Build {target}: {s.result}, {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s -> {s.outputPath}");
            if (s.result != BuildResult.Succeeded) Fail("Build failed: " + s.result);
            RemoveBurstDebugFolders(Path.GetDirectoryName(output));
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // Burst writes a debug-info folder next to the player. It is not needed to run the game.
        static void RemoveBurstDebugFolders(string buildDir)
        {
            foreach (string d in Directory.GetDirectories(buildDir, "*_BurstDebugInformation_DoNotShip"))
                Directory.Delete(d, true);
        }

        static void Fail(string message)
        {
            Debug.LogError("[GiaoQua] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw new Exception(message);
        }
    }
}

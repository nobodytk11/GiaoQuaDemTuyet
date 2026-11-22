using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GiaoQua.EditorTools
{
    /// <summary>Creates the smoke-test scene used for the very first Windows build.</summary>
    public static class HelloSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Hello.unity";

        [MenuItem("Tools/Giao Qua/Tạo scene Hello")]
        public static void Create()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.13f, 0.25f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("Hello").AddComponent<HelloScreen>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            BuildScript.SyncBuildSettings();
            Debug.Log("[GiaoQua] Hello scene saved.");
        }

        /// <summary>One call for the command line: settings, scene, build list.</summary>
        public static void Bootstrap()
        {
            ProjectSetup.Apply();
            Create();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}

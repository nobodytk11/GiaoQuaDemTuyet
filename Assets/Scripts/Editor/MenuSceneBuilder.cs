using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GiaoQua.UI;

namespace GiaoQua.EditorTools
{
    /// <summary>Builds the MainMenu and Ending scenes: a night sky with snow and the controller that draws the screens.</summary>
    public static class MenuSceneBuilder
    {
        public static void Build()
        {
            BuildScene("MainMenu", go => go.AddComponent<MenuController>());
            BuildScene("Ending", go => go.AddComponent<EndingController>());
            BuildScript.SyncBuildSettings();
        }

        static void BuildScene(string name, System.Action<GameObject> addController)
        {
            LevelBuilder.Prepare();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.114f, 0.227f);
            camGo.transform.position = new Vector3(0f, 5.2f, -10f);
            camGo.AddComponent<AudioListener>();

            LevelBuilder.BuildBackground();
            LevelBuilder.BuildSnow(cam);
            addController(new GameObject(name == "MainMenu" ? "Menu" : "Ending"));

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{name}.unity");
            Debug.Log("[GiaoQua] Built scene " + name);
        }
    }
}

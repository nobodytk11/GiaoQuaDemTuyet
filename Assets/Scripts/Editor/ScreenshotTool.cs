using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GiaoQua.Gameplay;

namespace GiaoQua.EditorTools
{
    /// <summary>
    /// Renders a level scene to PNG pictures with its camera, so a level can be looked at without opening the editor.
    /// Settings come from environment variables: SHOT_SCENE (scene name), SHOT_OUT (output folder),
    /// SHOT_POINTS ("x,y;x,y;..." camera centres; empty = the start), SHOT_W / SHOT_H (picture size).
    /// </summary>
    public static class ScreenshotTool
    {
        public static void CaptureFromEnvironment()
        {
            string sceneName = Environment.GetEnvironmentVariable("SHOT_SCENE") ?? "Level1";
            string outDir = Environment.GetEnvironmentVariable("SHOT_OUT") ?? "Builds/Shots";
            string points = Environment.GetEnvironmentVariable("SHOT_POINTS") ?? "";
            int w = int.Parse(Environment.GetEnvironmentVariable("SHOT_W") ?? "1280");
            int h = int.Parse(Environment.GetEnvironmentVariable("SHOT_H") ?? "720");
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene($"Assets/Scenes/{sceneName}.unity", OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam == null) throw new InvalidOperationException("no main camera in " + sceneName);
            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow != null) follow.enabled = false;

            var list = new System.Collections.Generic.List<Vector2>();
            foreach (string p in points.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var xy = p.Split(',');
                list.Add(new Vector2(float.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture),
                                     float.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture)));
            }
            if (list.Count == 0) list.Add(cam.transform.position);

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            int n = 0;
            foreach (var pos in list)
            {
                cam.transform.position = new Vector3(pos.x, pos.y, -10f);
                foreach (var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.SendMessage("LateUpdate");
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                string path = Path.Combine(outDir, $"{sceneName}_{n:00}.png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log($"[GiaoQua] Shot {path} at {pos}");
                n++;
            }
            cam.targetTexture = null;
            RenderTexture.active = null;
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}

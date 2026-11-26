using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using GiaoQua.Gameplay;

namespace GiaoQua.Tests
{
    /// <summary>
    /// Records a level played by the machine as numbered pictures (30 per second of game time), for the demo video.
    /// Only runs when REC_OUT (folder) and REC_LEVEL (scene name) are set, and needs a graphics run.
    /// </summary>
    [Category("Recording")]
    public class RecordingTests : PlayTestBase
    {
        [UnityTest, Timeout(3000000)]
        public IEnumerator Record_the_machine_playing_one_level()
        {
            string outDir = Environment.GetEnvironmentVariable("REC_OUT");
            string scene = Environment.GetEnvironmentVariable("REC_LEVEL");
            if (string.IsNullOrEmpty(outDir) || string.IsNullOrEmpty(scene)) Assert.Ignore("set REC_OUT and REC_LEVEL");
            Directory.CreateDirectory(outDir);
            int width = 1280, height = 720;

            Time.captureFramerate = 30;
            GameSession.StartAutoplay();
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return null; yield return null;

            var cam = Camera.main;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                canvas.sortingOrder += 100;
            }
            var level = LevelController.Instance;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            int frame = 0;
            float wonAt = -1f;
            while (frame < 30 * 60 * 6)                      // never more than six minutes of video
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, $"f{frame:000000}.jpg"), tex.EncodeToJPG(88));
                frame++;
                if (level.State == LevelState.Won && wonAt < 0f) wonAt = Time.time;
                if (wonAt >= 0f && Time.time - wonAt > 2.6f) break;      // stop before the machine moves on to the next level
                yield return null;
            }
            Time.captureFramerate = 0;
            Debug.Log($"[Rec] {scene}: {frame} frames, won={level.State == LevelState.Won}");
            Assert.AreEqual(LevelState.Won, level.State, "the machine should have finished the level");
        }
    }
}

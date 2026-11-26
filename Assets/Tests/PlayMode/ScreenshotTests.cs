using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using GiaoQua.Gameplay;
using GiaoQua.UI;

namespace GiaoQua.Tests
{
    /// <summary>Takes pictures of the real screens. Only runs when the SHOT_OUT folder is given (needs a graphics run).</summary>
    [Category("Screenshots")]
    public class ScreenshotTests : PlayTestBase
    {
        string outDir;

        [SetUp]
        public void Out()
        {
            outDir = Environment.GetEnvironmentVariable("SHOT_OUT");
            if (string.IsNullOrEmpty(outDir)) Assert.Ignore("set SHOT_OUT to take pictures");
            Directory.CreateDirectory(outDir);
        }

        IEnumerator Load(string scene)
        {
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            for (int i = 0; i < 4; i++) yield return null;
        }

        static IEnumerator Real(float seconds) { yield return new WaitForSecondsRealtime(seconds); }

        IEnumerator Shot(string name)
        {
            yield return null;
            var cam = Camera.main;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;       // so the camera picture includes the screens
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                canvas.sortingOrder += 100;
            }
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            Debug.Log($"[Shot] {name}");
            UnityEngine.Object.Destroy(tex);
            UnityEngine.Object.Destroy(rt);
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.sortingOrder -= 100;
        }

        static void Click(string label)
        {
            foreach (var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var t = b.transform.Find("Label");
                if (b.gameObject.activeInHierarchy && t != null && t.GetComponent<Text>().text == label && b.interactable) { b.onClick.Invoke(); return; }
            }
            Assert.Fail("no button " + label);
        }

        [UnityTest]
        public IEnumerator Menu_pages()
        {
            SaveService.AddCoins(120);
            SaveService.CompleteLevel(0, 2, 90, true);
            yield return Load("MainMenu");
            yield return Real(0.4f);
            yield return Shot("menu_main");
            Click("Chọn màn"); yield return Real(0.2f); yield return Shot("menu_levels");
            Click("Quay lại"); yield return null; Click("Cửa hàng"); yield return Real(0.2f); yield return Shot("menu_shop");
            Click("Quay lại"); yield return null; Click("Thành tích"); yield return Real(0.2f); yield return Shot("menu_achievements");
            Click("Quay lại"); yield return null; Click("Cài đặt"); yield return Real(0.2f); yield return Shot("menu_settings");
            yield return Load("Ending");
            yield return Real(0.3f);
            yield return Shot("ending");
        }

        [UnityTest]
        public IEnumerator Level_hud_and_panels()
        {
            yield return Load("Level1");
            var hero = PlayerController.Current;
            hero.Input = input;
            yield return Real(1.0f);
            yield return Shot("level1_start");
            hero.Teleport(new Vector2(50f, 2f));
            yield return Real(0.6f);
            hero.GetComponent<PlayerHealth>().TakeHit(hero.transform.position + Vector3.right);
            hero.GetComponent<PlayerPowerUps>().GiveShield();
            yield return Real(1.8f);
            yield return Shot("level1_playing");
            hero.Teleport(new Vector2(107.5f, 2f));
            yield return Real(0.8f);
            yield return Shot("level1_seesaw");
            LevelController.Instance.TogglePause();
            yield return Real(0.1f); yield return Shot("panel_pause");
            LevelController.Instance.TogglePause();
            hero.GetComponent<PlayerHealth>().Kill();
            yield return Real(0.1f); yield return Shot("panel_lose");
            LevelController.Instance.Retry();
            LevelController.Instance.Win();
            yield return Real(0.2f); yield return Shot("panel_win");
        }
    }
}

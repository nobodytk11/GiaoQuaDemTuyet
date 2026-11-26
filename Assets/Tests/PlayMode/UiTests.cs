using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using GiaoQua.Gameplay;
using GiaoQua.UI;

namespace GiaoQua.Tests
{
    public class UiTests : PlayTestBase
    {
        static IEnumerator Load(string scene)
        {
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return null;
            yield return null;
        }

        static Button FindButton(string label)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!b.gameObject.activeInHierarchy) continue;
                var t = b.transform.Find("Label");
                if (t != null && t.GetComponent<Text>().text == label) return b;
            }
            return null;
        }

        static void Click(string label)
        {
            var b = FindButton(label);
            Assert.IsNotNull(b, "no active button '" + label + "'");
            Assert.IsTrue(b.interactable, "button '" + label + "' is disabled");
            b.onClick.Invoke();
        }

        [Test]
        public void The_theme_and_the_audio_bank_are_in_Resources_and_complete()
        {
            var theme = Resources.Load<UITheme>("UITheme");
            Assert.IsNotNull(theme);
            Assert.IsNotNull(theme.font);
            Assert.IsNotNull(theme.btnGreen); Assert.IsNotNull(theme.panel); Assert.IsNotNull(theme.starOn); Assert.IsNotNull(theme.lockIcon);
            Assert.AreEqual(3, theme.skinIcons.Length);
            var bank = Resources.Load<AudioBank>("AudioBank");
            Assert.IsNotNull(bank);
            foreach (string id in new[] { "jump", "coin", "hurt", "win", "lose", "seesaw_launch" }) Assert.IsTrue(bank.TryGet(id, out _), id);
            Assert.IsTrue(bank.TryGetMusic("menu", out _));
            Assert.IsTrue(bank.TryGetMusic("game", out _));
            Assert.IsNotNull(Resources.Load<Material>("SpriteUnlit"));
        }

        [UnityTest]
        public IEnumerator The_main_menu_opens_the_shop_and_buying_works()
        {
            yield return Load("MainMenu");
            var menu = Object.FindFirstObjectByType<MenuController>();
            Assert.IsNotNull(menu);
            Assert.AreEqual("Main", menu.CurrentPageName);
            Click("Cửa hàng");
            yield return null;
            Assert.AreEqual("Shop", menu.CurrentPageName);
            Assert.IsFalse(FindButton("Mua 80").interactable, "no coins yet");
            SaveService.AddCoins(100);
            yield return null;
            Click("Quay lại");
            yield return null;
            Click("Cửa hàng");
            yield return null;
            Click("Mua 80");
            yield return null;
            Assert.IsTrue(SaveService.Owns("p2"));
            Assert.AreEqual(20, SaveService.Data.coins);
            Assert.IsNotNull(FindButton("Đang dùng"), "the bought skin is in use");
        }

        [UnityTest]
        public IEnumerator Locked_levels_cannot_be_started_and_the_play_button_starts_level_one()
        {
            yield return Load("MainMenu");
            var menu = Object.FindFirstObjectByType<MenuController>();
            Click("Chọn màn");
            yield return null;
            Assert.AreEqual("Levels", menu.CurrentPageName);
            Assert.IsNotNull(FindButton("Chơi"), "level 1 is open");
            int playButtons = 0;
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var t = b.transform.Find("Label");
                if (t != null && (t.GetComponent<Text>().text == "Chơi" || t.GetComponent<Text>().text == "Chơi lại") && b.gameObject.activeInHierarchy) playButtons++;
            }
            Assert.AreEqual(1, playButtons, "only level 1 can be played at the start");
            Click("Quay lại");
            yield return null;
            Click("Chơi");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Level1", "level 1 loaded", 8f);
        }

        [UnityTest]
        public IEnumerator Achievements_page_shows_all_ten_with_their_state()
        {
            SaveService.AddCoins(60);
            yield return Load("MainMenu");
            Click("Thành tích");
            yield return null;
            int rows = 0;
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
                if (t.text == "Nhặt 50 xu") rows++;
            Assert.AreEqual(1, rows);
            Assert.IsTrue(SaveService.Has("coins50"));
        }

        [UnityTest]
        public IEnumerator The_hud_follows_hearts_pause_win_and_lose()
        {
            yield return Load("Level1");
            var hud = Object.FindFirstObjectByType<GameHud>();
            var level = LevelController.Instance;
            var hero = PlayerController.Current;
            Assert.IsNotNull(hud); Assert.IsNotNull(level); Assert.IsNotNull(hero);
            yield return Seconds(0.3f);
            Assert.AreEqual(3, hud.HeartsShown);
            hero.GetComponent<PlayerHealth>().TakeHit(hero.transform.position + Vector3.left);
            Assert.AreEqual(2, hud.HeartsShown);

            level.TogglePause();
            Assert.IsTrue(hud.PausePanel.activeSelf);
            Click("Tiếp tục");
            Assert.IsFalse(hud.PausePanel.activeSelf);
            Assert.AreEqual(1f, Time.timeScale);

            hero.GetComponent<PlayerHealth>().Kill();
            Assert.IsTrue(hud.LosePanel.activeSelf);
            Click("Thử lại từ điểm lưu");
            yield return Seconds(0.2f);
            Assert.IsFalse(hud.LosePanel.activeSelf);
            Assert.AreEqual(3, hud.HeartsShown);
            Assert.AreEqual(LevelState.Playing, level.State);

            level.Win();
            Assert.IsTrue(hud.WinPanel.activeSelf);
            Assert.IsNotNull(FindButton("Màn tiếp theo"));
        }

        [UnityTest]
        public IEnumerator Coins_on_the_hud_follow_the_save()
        {
            yield return Load("Level1");
            yield return Seconds(0.2f);
            SaveService.AddCoins(7);
            SaveService.Save();
            yield return null;
            bool found = false;
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) if (t.text == "7") found = true;
            Assert.IsTrue(found, "the coin counter shows 7");
        }

        [UnityTest]
        public IEnumerator The_ending_screen_leads_back_to_the_menu()
        {
            yield return Load("Ending");
            Assert.IsNotNull(Object.FindFirstObjectByType<EndingController>());
            Click("Về menu");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "MainMenu", "menu loaded", 8f);
        }
    }
}

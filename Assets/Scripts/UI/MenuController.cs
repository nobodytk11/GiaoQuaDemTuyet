using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GiaoQua.Gameplay;

namespace GiaoQua.UI
{
    /// <summary>The main menu and its pages: levels, shop, achievements, settings. All drawn from code.</summary>
    public class MenuController : MonoBehaviour
    {
        enum Page { Main, Levels, Shop, Achievements, Settings }

        Canvas canvas;
        readonly Dictionary<Page, GameObject> pages = new Dictionary<Page, GameObject>();
        Text coinText;
        Button firstMainButton;
        bool confirmReset;

        public string CurrentPageName { get; private set; } = "Main";

        void Start()
        {
            Time.timeScale = 1f;
            GameSession.EndAutoplay();                     // coming back to the menu always leaves the demo
            canvas = UIKit.CreateCanvas("MenuCanvas", 5);
            canvas.transform.SetParent(transform, false);
            BuildCoinBar();
            BuildMain();
            Show(Page.Main);
            SaveService.Changed += RefreshCoins;
            GameAudio.PlayMusic("menu");
        }

        void OnDestroy() { SaveService.Changed -= RefreshCoins; }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && CurrentPageName != "Main") Show(Page.Main);
        }

        void RefreshCoins() { if (coinText != null) coinText.text = SaveService.Data.coins.ToString(); }

        void BuildCoinBar()
        {
            var theme = UITheme.Get();
            var bar = UIKit.Panel(canvas.transform, "CoinBar", new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(190f, 62f), new Color(1f, 1f, 1f, 0.85f));
            UIKit.Icon(bar.transform, "Coin", theme != null ? theme.coin : null, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(36f, 36f));
            coinText = UIKit.Label(bar.transform, "0", 32, new Vector2(0f, 0.5f), new Vector2(62f, 0f), new Vector2(110f, 50f), UIKit.Gold, TextAnchor.MiddleLeft);
            RefreshCoins();
        }

        GameObject NewPage(Page p)
        {
            var go = UIKit.Stretch(canvas.transform, p.ToString()).gameObject;
            pages[p] = go;
            return go;
        }

        void Show(Page p)
        {
            foreach (var kv in pages) kv.Value.SetActive(kv.Key == p);
            CurrentPageName = p.ToString();
            confirmReset = false;
            switch (p)
            {
                case Page.Levels: BuildLevels(); break;
                case Page.Shop: BuildShop(); break;
                case Page.Achievements: BuildAchievements(); break;
                case Page.Settings: BuildSettings(); break;
            }
            var first = pages[p].GetComponentInChildren<Button>();
            UIKit.Focus(first);
        }

        GameObject Rebuild(Page p)
        {
            if (pages.TryGetValue(p, out var old)) Destroy(old);
            return NewPage(p);
        }

        void BackButton(Transform parent)
        {
            UIKit.MakeButton(parent, "Quay lại", ButtonStyle.Grey, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(220f, 66f), () => Show(Page.Main), 28);
        }

        // ---------------------------------------------------------------- main
        void BuildMain()
        {
            var page = NewPage(Page.Main);
            UIKit.Label(page.transform, "Giao Quà Đêm Tuyết", 84, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1100f, 110f), UIKit.Gold, TextAnchor.MiddleCenter, true, FontStyle.Bold);
            UIKit.Label(page.transform, "Nhóm 6", 28, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(400f, 40f), Color.white);
            var theme = UITheme.Get();
            if (theme != null && theme.skinIcons != null && theme.skinIcons.Length > 0)
                UIKit.Icon(page.transform, "Hero", theme.skinIcons[Mathf.Max(0, System.Array.FindIndex(ShopCatalog.Skins, s => s.Id == SaveService.Data.skin))], new Vector2(0f, 0f), new Vector2(120f, 40f), new Vector2(150f, 200f));

            float y = -190f, step = 70f;
            Vector2 size = new Vector2(420f, 62f);
            firstMainButton = UIKit.MakeButton(page.transform, "Chơi", ButtonStyle.Green, new Vector2(0.5f, 1f), new Vector2(0f, y), size, PlayNext, 28);
            UIKit.MakeButton(page.transform, "Chọn màn", ButtonStyle.Blue, new Vector2(0.5f, 1f), new Vector2(0f, y - step), size, () => Show(Page.Levels), 28);
            UIKit.MakeButton(page.transform, "Cửa hàng", ButtonStyle.Blue, new Vector2(0.5f, 1f), new Vector2(0f, y - 2 * step), size, () => Show(Page.Shop), 28);
            UIKit.MakeButton(page.transform, "Thành tích", ButtonStyle.Blue, new Vector2(0.5f, 1f), new Vector2(0f, y - 3 * step), size, () => Show(Page.Achievements), 28);
            UIKit.MakeButton(page.transform, "Xem máy tự chơi", ButtonStyle.Blue, new Vector2(0.5f, 1f), new Vector2(0f, y - 4 * step), size, StartAutoplay, 28);
            UIKit.MakeButton(page.transform, "Cài đặt", ButtonStyle.Grey, new Vector2(0.5f, 1f), new Vector2(0f, y - 5 * step), size, () => Show(Page.Settings), 28);
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                UIKit.MakeButton(page.transform, "Thoát", ButtonStyle.Red, new Vector2(0.5f, 1f), new Vector2(0f, y - 6 * step), size, Quit, 28);
        }

        public void StartAutoplay()
        {
            GameSession.StartAutoplay();
            SceneManager.LoadScene(LevelCatalog.Levels[0].Scene);
        }

        public void PlayNext()
        {
            int index = Mathf.Clamp(SaveService.Data.unlockedLevels - 1, 0, LevelCatalog.Count - 1);
            StartLevel(index);
        }

        public void StartLevel(int index)
        {
            SceneManager.LoadScene(LevelCatalog.Levels[index].Scene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- levels
        void BuildLevels()
        {
            var page = Rebuild(Page.Levels);
            UIKit.Label(page.transform, "Chọn màn", 64, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 90f), Color.white);
            var theme = UITheme.Get();
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                int index = i;
                bool open = i < SaveService.Data.unlockedLevels;
                float x = -400f + i * 400f;
                var card = UIKit.Panel(page.transform, "Card" + i, new Vector2(0.5f, 0.5f), new Vector2(x, 20f), new Vector2(340f, 400f), new Color(1f, 1f, 1f, 0.92f));
                UIKit.Label(card.transform, LevelCatalog.Levels[i].Title, 52, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(300f, 70f), UIKit.Gold);
                UIKit.Label(card.transform, LevelCatalog.Levels[i].Subtitle, 34, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(300f, 50f), Color.white);
                for (int s = 0; s < 3; s++)
                {
                    bool on = SaveService.Data.stars[i] > s;
                    UIKit.Icon(card.transform, "Star" + s, theme != null ? (on ? theme.starOn : theme.starOff) : null, new Vector2(0.5f, 1f), new Vector2(-84f + s * 84f, -165f), new Vector2(70f, 70f));
                }
                if (open)
                    UIKit.MakeButton(card.transform, SaveService.Data.stars[i] > 0 ? "Chơi lại" : "Chơi", ButtonStyle.Green, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(240f, 70f), () => StartLevel(index));
                else
                {
                    UIKit.Icon(card.transform, "Lock", theme != null ? theme.lockIcon : null, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(70f, 80f));
                    UIKit.Label(card.transform, "Qua màn trước để mở", 22, new Vector2(0.5f, 0f), new Vector2(0f, 135f), new Vector2(300f, 40f), new Color(0.75f, 0.8f, 0.9f));
                }
            }
            BackButton(page.transform);
        }

        // ---------------------------------------------------------------- shop
        void BuildShop()
        {
            var page = Rebuild(Page.Shop);
            UIKit.Label(page.transform, "Cửa hàng", 64, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 90f), Color.white);
            UIKit.Label(page.transform, "Dùng xu nhặt được để mua nhân vật và giày", 26, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 40f), new Color(0.8f, 0.86f, 1f));
            var theme = UITheme.Get();

            // the three skins in a row; the boots as a fourth card
            int n = ShopCatalog.Skins.Length + 1;
            for (int i = 0; i < n; i++)
            {
                bool isBoots = i == ShopCatalog.Skins.Length;
                string id = isBoots ? ShopCatalog.Boots : ShopCatalog.Skins[i].Id;
                string name = isBoots ? "Giày nhảy cao" : ShopCatalog.Skins[i].Name;
                string note = isBoots ? "Nhảy cao hơn 15%" : ShopCatalog.Skins[i].Note;
                int price = 0;
                if (ShopCatalog.TryGetItem(id, out var item)) price = item.Price;
                float x = -480f + i * 320f;
                var card = UIKit.Panel(page.transform, "Card" + id, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(290f, 430f), new Color(1f, 1f, 1f, 0.92f));
                var icon = isBoots ? (theme != null ? theme.boots : null) : (theme != null && theme.skinIcons != null && i < theme.skinIcons.Length ? theme.skinIcons[i] : null);
                UIKit.Icon(card.transform, "Icon", icon, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(130f, 150f));
                UIKit.Label(card.transform, name, 32, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(270f, 46f), UIKit.Gold);
                UIKit.Label(card.transform, note, 22, new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(270f, 60f), Color.white);

                bool owned = SaveService.Owns(id);
                bool selected = !isBoots && SaveService.Data.skin == id;
                string captured = id;
                Button b;
                if (!owned)
                {
                    bool afford = SaveService.Data.coins >= price;
                    b = UIKit.MakeButton(card.transform, "Mua " + price, afford ? ButtonStyle.Green : ButtonStyle.Grey, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(230f, 66f), () => { SaveService.TryBuy(captured); GameAudio.Play("coin"); Show(Page.Shop); }, 28);
                    b.interactable = afford;
                }
                else if (isBoots)
                {
                    b = UIKit.MakeButton(card.transform, "Đã có", ButtonStyle.Grey, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(230f, 66f), null, 28);
                    b.interactable = false;
                }
                else if (selected)
                {
                    b = UIKit.MakeButton(card.transform, "Đang dùng", ButtonStyle.Grey, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(230f, 66f), null, 28);
                    b.interactable = false;
                }
                else b = UIKit.MakeButton(card.transform, "Dùng", ButtonStyle.Blue, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(230f, 66f), () => { SaveService.Select(captured); GameAudio.Play("ui_click"); Show(Page.Shop); }, 28);
            }
            BackButton(page.transform);
        }

        // ---------------------------------------------------------------- achievements
        void BuildAchievements()
        {
            var page = Rebuild(Page.Achievements);
            int got = SaveService.Data.achievements.Count;
            UIKit.Label(page.transform, $"Thành tích ({got}/{AchievementCatalog.All.Length})", 56, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(900f, 80f), Color.white);
            var theme = UITheme.Get();
            for (int i = 0; i < AchievementCatalog.All.Length; i++)
            {
                var a = AchievementCatalog.All[i];
                bool has = SaveService.Has(a.Id);
                int col = i % 2, row = i / 2;
                var rowPanel = UIKit.Panel(page.transform, "Row" + a.Id, new Vector2(0.5f, 1f), new Vector2(-330f + col * 660f, -110f - row * 96f), new Vector2(630f, 86f), has ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.6f, 0.65f, 0.8f, 0.75f));
                ((RectTransform)rowPanel.transform).pivot = new Vector2(0.5f, 1f);
                UIKit.Icon(rowPanel.transform, "State", theme != null ? (has ? theme.check : theme.lockIcon) : null, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(46f, 50f));
                UIKit.Label(rowPanel.transform, a.Title, 28, new Vector2(0f, 1f), new Vector2(80f, -8f), new Vector2(530f, 38f), has ? UIKit.Gold : new Color(0.85f, 0.88f, 0.95f), TextAnchor.MiddleLeft);
                UIKit.Label(rowPanel.transform, a.Description, 22, new Vector2(0f, 0f), new Vector2(80f, 8f), new Vector2(530f, 32f), Color.white, TextAnchor.MiddleLeft);
            }
            BackButton(page.transform);
        }

        // ---------------------------------------------------------------- settings
        void BuildSettings()
        {
            var page = Rebuild(Page.Settings);
            UIKit.Label(page.transform, "Cài đặt", 64, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 90f), Color.white);
            Slider("Nhạc nền", -170f, SaveService.Data.musicVolume, v => { SaveService.Data.musicVolume = v; GameAudio.ApplyVolumes(); }, page.transform);
            Slider("Hiệu ứng âm thanh", -270f, SaveService.Data.sfxVolume, v => SaveService.Data.sfxVolume = v, page.transform);
            Button reset = null;
            reset = UIKit.MakeButton(page.transform, "Xoá tiến trình", ButtonStyle.Red, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(420f, 70f), () =>
            {
                if (!confirmReset) { confirmReset = true; UIKit.SetLabel(reset, "Bấm lần nữa để xoá hết"); return; }
                SaveService.ResetAll();
                Show(Page.Main);
            }, 28);
            UIKit.MakeButton(page.transform, "Quay lại", ButtonStyle.Grey, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(220f, 66f), () => { SaveService.Save(); Show(Page.Main); }, 28);
        }

        void Slider(string label, float y, float value, UnityEngine.Events.UnityAction<float> onChange, Transform parent)
        {
            UIKit.Label(parent, label, 32, new Vector2(0.5f, 1f), new Vector2(-250f, y), new Vector2(360f, 50f), Color.white, TextAnchor.MiddleLeft);
            var rt = UIKit.Rect(parent, "Slider", new Vector2(0.5f, 1f), new Vector2(130f, y - 8f), new Vector2(360f, 36f));
            var slider = rt.gameObject.AddComponent<UnityEngine.UI.Slider>();
            var bg = UIKit.Rect(rt, "Background", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 14f)).gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.25f);
            var fillArea = UIKit.Stretch(rt, "Fill Area");
            fillArea.offsetMin = new Vector2(0f, 11f); fillArea.offsetMax = new Vector2(0f, -11f);
            var fill = UIKit.Stretch(fillArea, "Fill").gameObject.AddComponent<Image>();
            fill.color = UIKit.Gold;
            var handleArea = UIKit.Stretch(rt, "Handle Slide Area");
            var handle = UIKit.Rect(handleArea, "Handle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 36f)).gameObject.AddComponent<Image>();
            handle.color = Color.white;
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = (RectTransform)handle.transform;
            slider.targetGraphic = handle;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(onChange);
        }
    }
}

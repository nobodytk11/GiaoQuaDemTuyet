using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GiaoQua.Gameplay;

namespace GiaoQua.UI
{
    /// <summary>
    /// Everything drawn on top of a level: hearts, coins, power-ups, the level name, the pause button,
    /// pop-up messages, and the pause / win / lose panels.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        Canvas canvas;
        LevelController level;
        PlayerHealth health;
        PlayerPowerUps power;
        readonly List<Image> hearts = new List<Image>();
        Text coinText;
        Image shieldIcon, bootsIcon;
        Text toast;
        float toastUntil;
        readonly Queue<string> toastQueue = new Queue<string>();

        GameObject pausePanel, winPanel, losePanel;
        Image[] winStars;
        Text winSummary, winAchievements, winTip;
        Button winNext;

        public GameObject PausePanel => pausePanel;
        public GameObject WinPanel => winPanel;
        public GameObject LosePanel => losePanel;
        public int HeartsShown { get; private set; }

        void Start()
        {
            level = LevelController.Instance;
            var pc = PlayerController.Current;
            if (pc != null) { health = pc.GetComponent<PlayerHealth>(); power = pc.GetComponent<PlayerPowerUps>(); }
            canvas = UIKit.CreateCanvas("HUD", 10);
            canvas.transform.SetParent(transform, false);
            BuildTopBar();
            BuildToast();
            BuildPause();
            BuildWin();
            BuildLose();
            if (GameSession.Autoplay)
                UIKit.Label(canvas.transform, "Máy đang tự chơi · nhấn Esc rồi chọn Về menu để thoát", 24, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(900f, 40f), UIKit.Gold);
            gameObject.AddComponent<TouchControls>().Build(canvas.transform);        // shown on phones, or once a touch is seen

            if (health != null) { health.HeartsChanged += OnHearts; OnHearts(health.Hearts); }
            if (power != null) { power.Changed += OnPower; OnPower(); }
            if (level != null) level.StateChanged += OnState;
            SaveService.Changed += RefreshCoins;
            SaveService.AchievementUnlocked += OnAchievement;
            var seesaw = FindFirstObjectByType<Seesaw>();
            if (seesaw != null) seesaw.WeakHit += OnWeakHit;
            RefreshCoins();
            GameAudio.PlayMusic("game");
            if (level != null) Toast(StoryCatalog.Intro(level.levelIndex), 6f);
        }

        void OnDestroy()
        {
            if (health != null) health.HeartsChanged -= OnHearts;
            if (power != null) power.Changed -= OnPower;
            if (level != null) level.StateChanged -= OnState;
            SaveService.Changed -= RefreshCoins;
            SaveService.AchievementUnlocked -= OnAchievement;
        }

        // ---------------------------------------------------------------- layout
        void BuildTopBar()
        {
            var bar = UIKit.Panel(canvas.transform, "TopBar", new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(430f, 70f), new Color(1f, 1f, 1f, 0.85f));
            var theme = UITheme.Get();
            int max = health != null ? health.maxHearts : 3;
            for (int i = 0; i < max; i++)
                hearts.Add(UIKit.Icon(bar.transform, "Heart" + i, theme != null ? theme.heartFull : null, new Vector2(0f, 0.5f), new Vector2(18f + i * 46f, 0f), new Vector2(42f, 36f)));
            UIKit.Icon(bar.transform, "CoinIcon", theme != null ? theme.coin : null, new Vector2(0f, 0.5f), new Vector2(172f, 0f), new Vector2(36f, 36f));
            coinText = UIKit.Label(bar.transform, "0", 32, new Vector2(0f, 0.5f), new Vector2(212f, 0f), new Vector2(110f, 50f), UIKit.Gold, TextAnchor.MiddleLeft);
            shieldIcon = UIKit.Icon(bar.transform, "Shield", theme != null ? theme.shield : null, new Vector2(1f, 0.5f), new Vector2(-62f, 0f), new Vector2(40f, 40f));
            bootsIcon = UIKit.Icon(bar.transform, "Boots", theme != null ? theme.boots : null, new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(40f, 40f));
            shieldIcon.gameObject.SetActive(false);
            bootsIcon.gameObject.SetActive(false);

            string title = level != null ? $"{LevelCatalog.Levels[Mathf.Clamp(level.levelIndex, 0, LevelCatalog.Count - 1)].Title} · {LevelCatalog.Levels[Mathf.Clamp(level.levelIndex, 0, LevelCatalog.Count - 1)].Subtitle}" : "";
            UIKit.Label(canvas.transform, title, 28, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(420f, 44f), Color.white);
            UIKit.MakeButton(canvas.transform, "II", ButtonStyle.Grey, new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(76f, 64f), () => level?.TogglePause(), 30);
        }

        void BuildToast()
        {
            toast = UIKit.Label(canvas.transform, "", 27, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(940f, 110f), UIKit.Gold);
            toast.gameObject.SetActive(false);
        }

        GameObject BuildPanelShell(string name, string title, Color titleColor, float height)
        {
            var dim = UIKit.Stretch(canvas.transform, name);
            var bg = dim.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            var panel = UIKit.Panel(dim, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(620f, height));
            UIKit.Label(panel.transform, title, 52, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(560f, 70f), titleColor);
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        void BuildPause()
        {
            pausePanel = BuildPanelShell("PausePanel", "Tạm dừng", Color.white, 420f);
            var p = pausePanel.transform.Find("Panel");
            UIKit.MakeButton(p, "Tiếp tục", ButtonStyle.Green, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(380f, 76f), () => level?.TogglePause());
            UIKit.MakeButton(p, "Chơi lại màn", ButtonStyle.Blue, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(380f, 76f), () => level?.RestartLevel());
            UIKit.MakeButton(p, "Về menu", ButtonStyle.Red, new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(380f, 76f), () => level?.ToMenu());
        }

        void BuildLose()
        {
            losePanel = BuildPanelShell("LosePanel", "Thua rồi!", new Color(1f, 0.55f, 0.5f), 420f);
            var p = losePanel.transform.Find("Panel");
            UIKit.Label(p, "Đừng nản, thử lại nhé", 28, new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(520f, 40f), Color.white);
            UIKit.MakeButton(p, "Thử lại từ điểm lưu", ButtonStyle.Green, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(420f, 76f), () => level?.Retry());
            UIKit.MakeButton(p, "Chơi lại từ đầu", ButtonStyle.Blue, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(420f, 76f), () => level?.RestartLevel());
            UIKit.MakeButton(p, "Về menu", ButtonStyle.Red, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(420f, 76f), () => level?.ToMenu());
        }

        void BuildWin()
        {
            winPanel = BuildPanelShell("WinPanel", "Giao quà thành công!", UIKit.Gold, 640f);
            var p = winPanel.transform.Find("Panel");
            var theme = UITheme.Get();
            winStars = new Image[3];
            for (int i = 0; i < 3; i++)
                winStars[i] = UIKit.Icon(p, "Star" + i, theme != null ? theme.starOff : null, new Vector2(0.5f, 1f), new Vector2(-110f + i * 110f, -100f), new Vector2(96f, 96f));
            winSummary = UIKit.Label(p, "", 28, new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(560f, 44f), Color.white);
            winAchievements = UIKit.Label(p, "", 24, new Vector2(0.5f, 1f), new Vector2(0f, -255f), new Vector2(560f, 90f), UIKit.Gold);
            winTip = UIKit.Label(p, "", 22, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(540f, 70f), new Color(0.75f, 0.88f, 1f));
            winNext = UIKit.MakeButton(p, "Màn tiếp theo", ButtonStyle.Green, new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(420f, 70f), () => level?.NextLevel());
            UIKit.MakeButton(p, "Chơi lại", ButtonStyle.Blue, new Vector2(0.5f, 1f), new Vector2(-105f, -515f), new Vector2(200f, 64f), () => level?.RestartLevel(), 26);
            UIKit.MakeButton(p, "Về menu", ButtonStyle.Red, new Vector2(0.5f, 1f), new Vector2(105f, -515f), new Vector2(200f, 64f), () => level?.ToMenu(), 26);
        }

        // ---------------------------------------------------------------- events
        void OnHearts(int count)
        {
            HeartsShown = count;
            var theme = UITheme.Get();
            for (int i = 0; i < hearts.Count; i++)
            {
                bool full = i < count;
                if (theme != null) hearts[i].sprite = full ? theme.heartFull : theme.heartEmpty;
                hearts[i].color = full || theme == null ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                if (theme == null) hearts[i].color = full ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        void OnPower()
        {
            if (power == null) return;
            shieldIcon.gameObject.SetActive(power.HasShield);
            bootsIcon.gameObject.SetActive(power.BootsActive);
        }

        void RefreshCoins() { if (coinText != null) coinText.text = SaveService.Data.coins.ToString(); }

        void OnAchievement(AchievementInfo info) { Toast("Thành tích mới: " + info.Title); }
        void OnWeakHit(float impact) { Toast("Nhảy từ chỗ cao hơn xuống đầu ván!", 2.5f); }

        public void Toast(string message, float seconds = 3f)
        {
            toastQueue.Enqueue(message + "\n" + seconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        void OnState(LevelState state)
        {
            pausePanel.SetActive(state == LevelState.Paused);
            losePanel.SetActive(state == LevelState.Lost);
            winPanel.SetActive(state == LevelState.Won);
            if (state == LevelState.Won) FillWin(level.Result);
            if (state == LevelState.Paused) UIKit.Focus(pausePanel.GetComponentInChildren<Button>());
            if (state == LevelState.Lost) UIKit.Focus(losePanel.GetComponentInChildren<Button>());
            if (state == LevelState.Won) UIKit.Focus(winNext);
        }

        void FillWin(LevelResult r)
        {
            var theme = UITheme.Get();
            for (int i = 0; i < 3; i++)
            {
                if (theme != null) winStars[i].sprite = i < r.Stars ? theme.starOn : theme.starOff;
                winStars[i].color = theme != null ? Color.white : (i < r.Stars ? UIKit.Gold : new Color(0.4f, 0.4f, 0.4f));
            }
            winSummary.text = $"Xu: {r.Coins}/{r.CoinsTotal}   ·   Thời gian: {UIKit.Time(r.Seconds)}";
            var lines = new List<string>();
            if (r.NewAchievements != null) foreach (var a in r.NewAchievements) lines.Add("Thành tích mới: " + a.Title);
            winAchievements.text = string.Join("\n", lines);
            winTip.text = StoryCatalog.Tip(level.levelIndex);
            UIKit.SetLabel(winNext, LevelCatalog.IsLast(level.levelIndex) ? "Hoàn thành!" : "Màn tiếp theo");
        }

        void Update()
        {
            if (toast != null)
            {
                if (toast.gameObject.activeSelf && Time.unscaledTime > toastUntil) toast.gameObject.SetActive(false);
                if (!toast.gameObject.activeSelf && toastQueue.Count > 0)
                {
                    var parts = toastQueue.Dequeue().Split('\n');
                    toast.text = parts[0];
                    toastUntil = Time.unscaledTime + float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    toast.gameObject.SetActive(true);
                }
            }
            if (power != null && power.BootsActive != bootsIcon.gameObject.activeSelf) OnPower();
        }
    }
}

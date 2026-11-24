using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GiaoQua.Gameplay
{
    public enum LevelState { Playing, Paused, Won, Lost }

    public struct LevelResult
    {
        public int Stars, Coins, CoinsTotal, Seconds;
        public bool Died;
        public List<AchievementInfo> NewAchievements;
    }

    /// <summary>One per level scene: tracks win, lose, pause, checkpoints and moving on to the next scene.</summary>
    public class LevelController : MonoBehaviour
    {
        public static LevelController Instance { get; private set; }

        public int levelIndex;
        public Vector2 spawnPoint;
        public bool allowPauseKey = true;

        public LevelState State { get; private set; } = LevelState.Playing;
        public LevelResult Result { get; private set; }
        public event Action<LevelState> StateChanged;

        PlayerController player;
        PlayerHealth health;
        Gift gift;
        Vector2 checkpoint;
        bool hasCheckpoint;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            GameSession.BeginLevel(levelIndex);
            VirtualInput.Clear();
        }

        void Start()
        {
            player = PlayerController.Current;
            if (player != null)
            {
                health = player.GetComponent<PlayerHealth>();
                if (health != null) health.Died += OnPlayerDied;
            }
            gift = FindFirstObjectByType<Gift>();
            int coins = 0;
            foreach (var c in FindObjectsByType<Collectible>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.kind == CollectibleKind.Coin) coins += c.amount;
            foreach (var b in FindObjectsByType<BonusBox>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.content == null) coins += b.coinReward;
            GameSession.CoinsInLevel = coins;
        }

        void OnDestroy()
        {
            if (health != null) health.Died -= OnPlayerDied;
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (!allowPauseKey) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) TogglePause();
        }

        void SetState(LevelState s)
        {
            if (State == s) return;
            State = s;
            StateChanged?.Invoke(s);
        }

        public void SetCheckpoint(Vector2 position) { checkpoint = position; hasCheckpoint = true; }
        public Vector2 RespawnPoint => hasCheckpoint ? checkpoint : spawnPoint;

        public void TogglePause()
        {
            if (State == LevelState.Playing) { Time.timeScale = 0f; SetState(LevelState.Paused); GameAudio.Play("pause"); }
            else if (State == LevelState.Paused) { Time.timeScale = 1f; SetState(LevelState.Playing); }
        }

        public void Win()
        {
            if (State != LevelState.Playing) return;
            if (player != null) player.Stop();
            var achievements = new List<AchievementInfo>();
            void H(AchievementInfo a) => achievements.Add(a);
            SaveService.AchievementUnlocked += H;
            int seconds = GameSession.Seconds;
            int stars = SaveService.StarsFor(GameSession.CoinsThisRun, GameSession.CoinsInLevel, GameSession.DiedThisRun);
            SaveService.CompleteLevel(levelIndex, stars, seconds, GameSession.DiedThisRun);
            SaveService.AchievementUnlocked -= H;
            Result = new LevelResult
            {
                Stars = stars, Coins = GameSession.CoinsThisRun, CoinsTotal = GameSession.CoinsInLevel,
                Seconds = seconds, Died = GameSession.DiedThisRun, NewAchievements = achievements,
            };
            SetState(LevelState.Won);
        }

        void OnPlayerDied() { if (State == LevelState.Playing) SetState(LevelState.Lost); }

        /// <summary>After losing: full hearts, back to the last checkpoint (or the start).</summary>
        public void Retry()
        {
            if (player == null) return;
            if (health != null) health.ResetFull();
            player.GetComponent<PlayerPowerUps>()?.ClearAll();
            player.Teleport(RespawnPoint);
            player.Controllable = true;
            if (gift != null) gift.ResetHome();
            Time.timeScale = 1f;
            SetState(LevelState.Playing);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void NextLevel()
        {
            Time.timeScale = 1f;
            if (LevelCatalog.IsLast(levelIndex)) SceneManager.LoadScene(LevelCatalog.EndingScene);
            else SceneManager.LoadScene(LevelCatalog.Levels[levelIndex + 1].Scene);
        }

        public void ToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(LevelCatalog.MenuScene);
        }
    }
}

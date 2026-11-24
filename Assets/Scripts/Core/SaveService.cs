using System;
using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua
{
    /// <summary>Loads, changes and saves SaveData. All progress rules live here so they can be tested without a scene.</summary>
    public static class SaveService
    {
        const string Key = "giaoqua.save.v1";
        static IKeyValueStore store = new PlayerPrefsStore();
        static SaveData data;

        public static event Action<AchievementInfo> AchievementUnlocked;
        public static event Action Changed;

        public static void UseStore(IKeyValueStore s) { store = s; data = null; }

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            string json = store.Get(Key);
            try { data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json); }
            catch (Exception) { data = new SaveData(); }
            if (data == null) data = new SaveData();
            if (data.stars == null || data.stars.Length < LevelCatalog.Count) Array.Resize(ref data.stars, LevelCatalog.Count);
            if (data.owned == null) data.owned = new List<string>();
            if (!data.owned.Contains(ShopCatalog.DefaultSkin)) data.owned.Add(ShopCatalog.DefaultSkin);
            if (data.achievements == null) data.achievements = new List<string>();
            data.unlockedLevels = Mathf.Clamp(data.unlockedLevels, 1, LevelCatalog.Count);
        }

        public static void Save()
        {
            store.Set(Key, JsonUtility.ToJson(Data));
            Changed?.Invoke();
        }

        public static void ResetAll()
        {
            store.Delete(Key);
            data = new SaveData();
            Changed?.Invoke();
        }

        // ---- coins and shop ----
        public static void AddCoins(int n)
        {
            if (n <= 0) return;
            Data.coins += n;
            Data.totalCoins += n;
            CheckAchievements();
        }

        public static bool Owns(string id) => Data.owned.Contains(id);

        public static bool TryBuy(string id)
        {
            if (!ShopCatalog.TryGetItem(id, out var item) || Owns(id) || Data.coins < item.Price) return false;
            Data.coins -= item.Price;
            Data.owned.Add(id);
            if (item.IsSkin) Data.skin = id;
            CheckAchievements(shopped: true);
            Save();
            return true;
        }

        public static bool Select(string skinId)
        {
            if (!Owns(skinId)) return false;
            foreach (var s in ShopCatalog.Skins)
                if (s.Id == skinId) { Data.skin = skinId; Save(); return true; }
            return false;
        }

        public static float JumpMultiplier()
        {
            float m = ShopCatalog.Skin(Data.skin).JumpScale;
            if (Owns(ShopCatalog.Boots)) m *= 1f + ShopCatalog.BootsJumpBonus;
            return m;
        }

        // ---- levels ----
        /// <summary>Stars: 1 for finishing, +1 for coins, +1 for no deaths and speed (see StarRule).</summary>
        public static void CompleteLevel(int index, int stars, int seconds, bool diedThisRun)
        {
            if (index < 0 || index >= LevelCatalog.Count) return;
            Data.stars[index] = Mathf.Max(Data.stars[index], Mathf.Clamp(stars, 1, 3));
            Data.unlockedLevels = Mathf.Max(Data.unlockedLevels, Mathf.Min(index + 2, LevelCatalog.Count));
            Data.levelsCompleted++;
            if (seconds > 0 && (Data.bestTimeSeconds == 0 || seconds < Data.bestTimeSeconds)) Data.bestTimeSeconds = seconds;
            if (index == 0 && !diedThisRun) Data.noDeathLevel1 = true;
            CheckAchievements();
            Save();
        }

        public static int StarsFor(int coinsCollected, int coinsTotal, bool diedThisRun)
        {
            int stars = 1;
            if (coinsTotal > 0 && coinsCollected * 10 >= coinsTotal * 7) stars++;   // 70% of the coins
            if (!diedThisRun) stars++;
            return stars;
        }

        // ---- counters ----
        public static void CountStomp() { Data.totalStomps++; CheckAchievements(); }
        public static void CountSnowballHit() { Data.totalSnowballHits++; CheckAchievements(); }
        public static void CountBox() { Data.totalBoxes++; CheckAchievements(); }

        // ---- achievements ----
        public static bool Has(string id) => Data.achievements.Contains(id);

        public static void CheckAchievements(bool shopped = false)
        {
            var d = Data;
            Grant("coins50", d.totalCoins >= 50);
            Grant("coins200", d.totalCoins >= 200);
            Grant("first_delivery", d.levelsCompleted >= 1);
            Grant("no_death_1", d.noDeathLevel1);
            Grant("stomp10", d.totalStomps >= 10);
            Grant("snowball5", d.totalSnowballHits >= 5);
            Grant("fast", d.bestTimeSeconds > 0 && d.bestTimeSeconds < 60);
            Grant("all_levels", AllLevelsDone());
            Grant("shopper", shopped);
            Grant("boxes10", d.totalBoxes >= 10);
        }

        static bool AllLevelsDone()
        {
            foreach (int s in Data.stars) if (s <= 0) return false;
            return Data.stars.Length >= LevelCatalog.Count;
        }

        static void Grant(string id, bool condition)
        {
            if (!condition || Data.achievements.Contains(id)) return;
            Data.achievements.Add(id);
            if (AchievementCatalog.TryGet(id, out var info)) AchievementUnlocked?.Invoke(info);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GiaoQua.Tests
{
    public class SaveServiceTests
    {
        MemoryStore mem;

        [SetUp]
        public void SetUp()
        {
            mem = new MemoryStore();
            SaveService.UseStore(mem);
        }

        [TearDown]
        public void TearDown() => SaveService.UseStore(new PlayerPrefsStore());

        [Test]
        public void A_new_save_starts_with_level_one_open_and_the_default_skin()
        {
            Assert.AreEqual(1, SaveService.Data.unlockedLevels);
            Assert.AreEqual(0, SaveService.Data.coins);
            Assert.AreEqual("p1", SaveService.Data.skin);
            Assert.IsTrue(SaveService.Owns("p1"));
        }

        [Test]
        public void Progress_survives_saving_and_loading_again()
        {
            SaveService.AddCoins(70);
            SaveService.CompleteLevel(0, 3, 90, false);
            SaveService.TryBuy("boots");
            SaveService.Save();

            SaveService.UseStore(mem);              // forget the in-memory copy, read the text back
            Assert.AreEqual(10, SaveService.Data.coins);
            Assert.AreEqual(2, SaveService.Data.unlockedLevels);
            Assert.AreEqual(3, SaveService.Data.stars[0]);
            Assert.IsTrue(SaveService.Owns("boots"));
        }

        [Test]
        public void Broken_save_text_falls_back_to_a_fresh_save()
        {
            mem.Set("giaoqua.save.v1", "{ not json");
            Assert.AreEqual(1, SaveService.Data.unlockedLevels);
            mem.Set("giaoqua.save.v1", "");
            SaveService.UseStore(mem);
            Assert.AreEqual(0, SaveService.Data.coins);
        }

        [Test]
        public void Buying_needs_enough_coins_and_works_only_once()
        {
            SaveService.AddCoins(79);
            Assert.IsFalse(SaveService.TryBuy("p2"), "79 < 80");
            SaveService.AddCoins(1);
            Assert.IsTrue(SaveService.TryBuy("p2"));
            Assert.AreEqual(0, SaveService.Data.coins);
            Assert.AreEqual("p2", SaveService.Data.skin, "a bought skin is selected");
            Assert.IsFalse(SaveService.TryBuy("p2"), "already owned");
            Assert.IsFalse(SaveService.TryBuy("nope"));
        }

        [Test]
        public void Only_owned_skins_can_be_selected()
        {
            Assert.IsFalse(SaveService.Select("p3"));
            SaveService.AddCoins(100);
            SaveService.TryBuy("p3");
            Assert.IsTrue(SaveService.Select("p1"));
            Assert.AreEqual("p1", SaveService.Data.skin);
        }

        [Test]
        public void Boots_and_skin_change_the_jump_multiplier()
        {
            Assert.AreEqual(1f, SaveService.JumpMultiplier(), 0.001f);
            SaveService.AddCoins(160);
            SaveService.TryBuy("boots");
            Assert.AreEqual(1.15f, SaveService.JumpMultiplier(), 0.001f);
            SaveService.TryBuy("p3");
            Assert.AreEqual(1.15f * 1.15f, SaveService.JumpMultiplier(), 0.001f);
        }

        [Test]
        public void Finishing_a_level_opens_the_next_and_keeps_the_best_stars()
        {
            SaveService.CompleteLevel(0, 2, 100, true);
            Assert.AreEqual(2, SaveService.Data.unlockedLevels);
            SaveService.CompleteLevel(0, 1, 100, true);
            Assert.AreEqual(2, SaveService.Data.stars[0], "a worse run does not lower stars");
            SaveService.CompleteLevel(2, 3, 100, false);
            Assert.AreEqual(3, SaveService.Data.unlockedLevels, "never more than the number of levels");
        }

        [Test]
        public void Stars_follow_the_rule_coins_and_no_death()
        {
            Assert.AreEqual(1, SaveService.StarsFor(0, 10, true));
            Assert.AreEqual(2, SaveService.StarsFor(7, 10, true));
            Assert.AreEqual(2, SaveService.StarsFor(0, 10, false));
            Assert.AreEqual(3, SaveService.StarsFor(10, 10, false));
            Assert.AreEqual(2, SaveService.StarsFor(0, 0, false), "no coins in the level counts as not missed");
        }

        [Test]
        public void Achievements_unlock_once_and_raise_an_event()
        {
            var seen = new System.Collections.Generic.List<string>();
            void H(AchievementInfo a) => seen.Add(a.Id);
            SaveService.AchievementUnlocked += H;
            try
            {
                SaveService.AddCoins(30);
                Assert.IsFalse(SaveService.Has("coins50"));
                SaveService.AddCoins(20);
                SaveService.AddCoins(20);
                Assert.IsTrue(SaveService.Has("coins50"));
                Assert.AreEqual(1, seen.Count(x => x == "coins50"), "only one event");

                for (int i = 0; i < 10; i++) SaveService.CountStomp();
                Assert.IsTrue(SaveService.Has("stomp10"));
                SaveService.CompleteLevel(0, 3, 45, false);
                Assert.IsTrue(SaveService.Has("first_delivery"));
                Assert.IsTrue(SaveService.Has("fast"));
                Assert.IsTrue(SaveService.Has("no_death_1"));
            }
            finally { SaveService.AchievementUnlocked -= H; }
        }

        [Test]
        public void Every_achievement_in_the_catalog_can_be_unlocked()
        {
            SaveService.AddCoins(1000);
            for (int i = 0; i < 12; i++) { SaveService.CountStomp(); SaveService.CountSnowballHit(); SaveService.CountBox(); }
            for (int level = 0; level < LevelCatalog.Count; level++) SaveService.CompleteLevel(level, 3, 40, false);
            SaveService.TryBuy("boots");
            foreach (var a in AchievementCatalog.All)
                Assert.IsTrue(SaveService.Has(a.Id), "never unlocked: " + a.Id);
        }

        [Test]
        public void Level_and_skin_tables_are_consistent()
        {
            Assert.AreEqual(3, LevelCatalog.Count);
            foreach (var item in ShopCatalog.Items) Assert.Greater(item.Price, 0, item.Id);
            foreach (var s in ShopCatalog.Skins) Assert.Greater(s.Mass, 0f, s.Id);
            Assert.IsTrue(LevelCatalog.IsLast(2));
            Assert.IsFalse(LevelCatalog.IsLast(0));
        }
    }
}

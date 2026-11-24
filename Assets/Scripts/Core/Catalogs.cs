using System.Collections.Generic;

namespace GiaoQua
{
    public struct LevelInfo
    {
        public string Scene, Title, Subtitle;
        public LevelInfo(string scene, string title, string subtitle) { Scene = scene; Title = title; Subtitle = subtitle; }
    }

    public static class LevelCatalog
    {
        public const string MenuScene = "MainMenu";
        public const string EndingScene = "Ending";
        public static readonly LevelInfo[] Levels =
        {
            new LevelInfo("Level1", "Màn 1", "Làm quen"),
            new LevelInfo("Level2", "Màn 2", "Băng trơn"),
            new LevelInfo("Level3", "Màn 3", "Bão tuyết"),
        };
        public static int Count => Levels.Length;
        public static bool IsLast(int index) => index >= Levels.Length - 1;
    }

    /// <summary>Numbers that differ between hero skins. Mass and jump change how the physics feels.</summary>
    public struct SkinInfo
    {
        public string Id, Name, Note;
        public float Mass, JumpScale;
        public int Price;
        public SkinInfo(string id, string name, string note, float mass, float jump, int price)
        { Id = id; Name = name; Note = note; Mass = mass; JumpScale = jump; Price = price; }
    }

    public struct ShopItem
    {
        public string Id, Name, Note;
        public int Price;
        public bool IsSkin;
        public ShopItem(string id, string name, string note, int price, bool isSkin)
        { Id = id; Name = name; Note = note; Price = price; IsSkin = isSkin; }
    }

    public static class ShopCatalog
    {
        public const string DefaultSkin = "p1";
        public const string Boots = "boots";
        public const float BootsJumpBonus = 0.15f;

        public static readonly SkinInfo[] Skins =
        {
            new SkinInfo("p1", "Bạn Xanh", "Cân bằng", 1.0f, 1.00f, 0),
            new SkinInfo("p2", "Bạn Chắc", "Nặng, nhảy thấp", 1.5f, 0.88f, 80),
            new SkinInfo("p3", "Bạn Nhẹ", "Nhẹ, nhảy cao", 0.75f, 1.15f, 100),
        };

        public static readonly ShopItem[] Items =
        {
            new ShopItem("p2", "Bạn Chắc", "Nặng, nhảy thấp, đẩy mạnh ván", 80, true),
            new ShopItem("p3", "Bạn Nhẹ", "Nhẹ, nhảy cao", 100, true),
            new ShopItem(Boots, "Giày nhảy cao", "Nhảy cao hơn 15%", 60, false),
        };

        public static SkinInfo Skin(string id)
        {
            foreach (var s in Skins) if (s.Id == id) return s;
            return Skins[0];
        }

        public static bool TryGetItem(string id, out ShopItem item)
        {
            foreach (var i in Items) if (i.Id == id) { item = i; return true; }
            item = default;
            return false;
        }
    }

    public struct AchievementInfo
    {
        public string Id, Title, Description;
        public AchievementInfo(string id, string title, string desc) { Id = id; Title = title; Description = desc; }
    }

    public static class AchievementCatalog
    {
        public static readonly AchievementInfo[] All =
        {
            new AchievementInfo("coins50", "Nhặt 50 xu", "Nhặt tổng cộng 50 xu"),
            new AchievementInfo("coins200", "Nhà giàu tuyết", "Nhặt tổng cộng 200 xu"),
            new AchievementInfo("first_delivery", "Giao quà đầu tiên", "Hoàn thành một màn"),
            new AchievementInfo("no_death_1", "Màn 1 không ngã", "Qua Màn 1 mà không thua lần nào"),
            new AchievementInfo("stomp10", "Đạp đầu", "Đạp 10 kẻ thù"),
            new AchievementInfo("snowball5", "Xạ thủ tuyết", "Ném trúng 5 kẻ thù bằng bóng tuyết"),
            new AchievementInfo("fast", "Giao hàng nhanh", "Hoàn thành một màn dưới 60 giây"),
            new AchievementInfo("all_levels", "Cả đêm tuyết", "Hoàn thành cả 3 màn"),
            new AchievementInfo("shopper", "Mua sắm", "Mua một món ở cửa hàng"),
            new AchievementInfo("boxes10", "Đập hộp", "Đập 10 hộp bí ẩn"),
        };

        public static bool TryGet(string id, out AchievementInfo info)
        {
            foreach (var a in All) if (a.Id == id) { info = a; return true; }
            info = default;
            return false;
        }
    }
}

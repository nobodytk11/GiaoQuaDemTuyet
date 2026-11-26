namespace GiaoQua
{
    /// <summary>The story text shown at the start of each level and the environment tip on the win panel.</summary>
    public static class StoryCatalog
    {
        static readonly string[] Intros =
        {
            "Đêm Giáng Sinh, băng ở Bắc Cực đang tan dần vì khói bụi và rác thải. Hãy giúp chú bé tuyết giao món quà đầu tiên!",
            "Sương mù ô nhiễm làm mặt băng trơn trượt. Cẩn thận cột băng rơi, hố sâu và đừng trượt quá đà!",
            "Bão tuyết nổi lên, gió thổi lệch cả món quà. Hãy chờ lúc gió lặng để giao món quà cuối cùng!",
        };

        static readonly string[] Tips =
        {
            "Bạn có biết? Bỏ rác đúng chỗ và phân loại rác giúp giữ môi trường sạch hơn.",
            "Bạn có biết? Đi bộ, đạp xe hoặc đi xe buýt giúp giảm khói bụi trong không khí.",
            "Bạn có biết? Tiết kiệm điện và dùng lại đồ cũ giúp Trái Đất bớt nóng lên.",
        };

        public const string EndingMessage = "Cảm ơn bạn đã giúp băng không tan. Hãy bảo vệ môi trường mỗi ngày nhé!";

        public static string Intro(int level) => Intros[System.Math.Max(0, System.Math.Min(level, Intros.Length - 1))];
        public static string Tip(int level) => Tips[System.Math.Max(0, System.Math.Min(level, Tips.Length - 1))];
    }
}

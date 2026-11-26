using UnityEngine;

namespace GiaoQua.UI
{
    /// <summary>The pictures and the font every screen uses. Built by an editor tool into Resources/UITheme.</summary>
    public class UITheme : ScriptableObject
    {
        public Font font;
        public Sprite panel, btnBlue, btnGreen, btnRed, btnGrey;
        public Sprite heartFull, heartEmpty, coin, starOn, starOff, lockIcon, check, boots, shield;
        public Sprite[] skinIcons;

        static UITheme cached;
        public static UITheme Get()
        {
            if (cached == null) cached = Resources.Load<UITheme>("UITheme");
            return cached;
        }
    }
}

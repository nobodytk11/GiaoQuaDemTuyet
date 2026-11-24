using System;
using System.Collections.Generic;

namespace GiaoQua
{
    /// <summary>Everything that must survive closing the game. Stored as one JSON text.</summary>
    [Serializable]
    public class SaveData
    {
        public int coins;
        public int unlockedLevels = 1;                  // how many levels can be chosen (1..3)
        public int[] stars = new int[3];                // best stars per level, 0 = not finished
        public List<string> owned = new List<string> { ShopCatalog.DefaultSkin };
        public string skin = ShopCatalog.DefaultSkin;
        public List<string> achievements = new List<string>();

        // lifetime counters used by achievements
        public int totalCoins;
        public int totalStomps;
        public int totalSnowballHits;
        public int totalBoxes;
        public int levelsCompleted;
        public int bestTimeSeconds;                     // fastest finished level, 0 = none
        public bool noDeathLevel1;                      // finished level 1 without dying
        public float musicVolume = 0.6f;
        public float sfxVolume = 0.9f;
    }
}

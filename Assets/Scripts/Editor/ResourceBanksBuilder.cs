using System.IO;
using UnityEditor;
using UnityEngine;
using GiaoQua.Gameplay;
using GiaoQua.UI;

namespace GiaoQua.EditorTools
{
    /// <summary>Creates Resources/UITheme and Resources/AudioBank from the imported pictures, font and sounds.</summary>
    public static class ResourceBanksBuilder
    {
        const string Own = "Assets/Art/Own/";
        const string Deluxe = "Assets/Art/Kenney/PlatformerArtDeluxe/";

        static T Load<T>(string path) where T : Object
        {
            var o = AssetDatabase.LoadAssetAtPath<T>(path);
            if (o == null) throw new FileNotFoundException("asset not found: " + path);
            return o;
        }

        static T Fresh<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        public static void Build()
        {
            Directory.CreateDirectory("Assets/Resources");
            var theme = Fresh<UITheme>("Assets/Resources/UITheme.asset");
            theme.font = Load<Font>("Assets/Fonts/Baloo2/Baloo2-VariableFont_wght.ttf");
            theme.panel = Load<Sprite>(Own + "panel.png");
            theme.btnBlue = Load<Sprite>(Own + "btn_blue.png");
            theme.btnGreen = Load<Sprite>(Own + "btn_green.png");
            theme.btnRed = Load<Sprite>(Own + "btn_red.png");
            theme.btnGrey = Load<Sprite>(Own + "btn_grey.png");
            theme.heartFull = Load<Sprite>(Deluxe + "HUD/hud_heartFull.png");
            theme.heartEmpty = Load<Sprite>(Deluxe + "HUD/hud_heartEmpty.png");
            theme.coin = Load<Sprite>(Deluxe + "HUD/hud_coins.png");
            theme.starOn = Load<Sprite>(Own + "ui_star_on.png");
            theme.starOff = Load<Sprite>(Own + "ui_star_off.png");
            theme.lockIcon = Load<Sprite>(Own + "ui_lock.png");
            theme.check = Load<Sprite>(Own + "ui_check.png");
            theme.boots = Load<Sprite>(Own + "boots.png");
            theme.shield = Load<Sprite>(Own + "shield.png");
            theme.skinIcons = new[]
            {
                Load<Sprite>(Deluxe + "Player/p1_front.png"), Load<Sprite>(Deluxe + "Player/p2_front.png"), Load<Sprite>(Deluxe + "Player/p3_front.png"),
            };
            EditorUtility.SetDirty(theme);

            var bank = Fresh<AudioBank>("Assets/Resources/AudioBank.asset");
            bank.sfx.Clear();
            foreach (string file in Directory.GetFiles("Assets/Audio/Sfx", "*.wav"))
            {
                string id = Path.GetFileNameWithoutExtension(file);
                bank.sfx.Add(new AudioBank.Entry { id = id, clip = AssetDatabase.LoadAssetAtPath<AudioClip>(file.Replace('\\', '/')) });
            }
            bank.music.Clear();
            bank.music.Add(new AudioBank.Entry { id = "menu", clip = Load<AudioClip>("Assets/Audio/Music/menu_christmas-synths.ogg") });
            bank.music.Add(new AudioBank.Entry { id = "game", clip = Load<AudioClip>("Assets/Audio/Music/game_snow-theme.ogg") });
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GiaoQua] UITheme and AudioBank built ({bank.sfx.Count} sounds, {bank.music.Count} songs).");
        }
    }
}

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GiaoQua.Tests
{
    public class AudioTests
    {
        static readonly string[] Sfx =
        {
            "jump", "land", "coin", "hurt", "stomp", "snowball_throw", "snowball_hit", "powerup", "shield", "heal",
            "checkpoint", "seesaw_launch", "gift_delivered", "win", "lose", "player_die", "boss_hit",
            "ui_click", "ui_move", "pause", "bonus_box"
        };

        [Test]
        public void Every_named_sound_effect_is_imported_and_has_length()
        {
            foreach (string name in Sfx)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Sfx/" + name + ".wav");
                Assert.IsNotNull(clip, "missing sound: " + name);
                Assert.Greater(clip.length, 0.02f, "empty sound: " + name);
                Assert.Less(clip.length, 8f, "sound too long for an effect: " + name);
            }
        }

        [Test]
        public void Both_music_loops_are_imported_and_long_enough_to_loop()
        {
            foreach (string name in new[] { "menu_christmas-synths", "game_snow-theme" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/" + name + ".ogg");
                Assert.IsNotNull(clip, "missing music: " + name);
                Assert.Greater(clip.length, 10f, "music too short: " + name);
            }
        }
    }
}

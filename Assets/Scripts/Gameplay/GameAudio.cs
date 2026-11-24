using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Sound effects and music by name. The clips come from Resources/AudioBank (built by an editor tool).</summary>
    public static class GameAudio
    {
        static AudioBank bank;
        static AudioSource musicSource;
        static readonly List<AudioSource> pool = new List<AudioSource>();
        static GameObject host;

        static AudioBank Bank
        {
            get
            {
                if (bank == null) bank = Resources.Load<AudioBank>("AudioBank");
                return bank;
            }
        }

        static void EnsureHost()
        {
            if (host != null) return;
            host = new GameObject("GameAudio");
            Object.DontDestroyOnLoad(host);
            musicSource = host.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            for (int i = 0; i < 8; i++)
            {
                var s = host.AddComponent<AudioSource>();
                s.playOnAwake = false;
                pool.Add(s);
            }
        }

        public static int PlayedCount { get; private set; }
        public static string LastPlayed { get; private set; }

        public static void Play(string id, float volume = 1f)
        {
            LastPlayed = id;
            PlayedCount++;
            var b = Bank;
            if (b == null || !b.TryGet(id, out var clip)) return;
            EnsureHost();
            foreach (var s in pool)
            {
                if (s.isPlaying) continue;
                s.PlayOneShot(clip, volume * SaveService.Data.sfxVolume);
                return;
            }
            pool[0].PlayOneShot(clip, volume * SaveService.Data.sfxVolume);
        }

        public static void PlayMusic(string id)
        {
            var b = Bank;
            if (b == null || !b.TryGetMusic(id, out var clip)) return;
            EnsureHost();
            if (musicSource.clip == clip && musicSource.isPlaying) { musicSource.volume = SaveService.Data.musicVolume; return; }
            musicSource.clip = clip;
            musicSource.volume = SaveService.Data.musicVolume;
            musicSource.Play();
        }

        public static void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = SaveService.Data.musicVolume;
        }
    }

    [CreateAssetMenu(menuName = "Giao Qua/Audio Bank")]
    public class AudioBank : ScriptableObject
    {
        [System.Serializable] public struct Entry { public string id; public AudioClip clip; }
        public List<Entry> sfx = new List<Entry>();
        public List<Entry> music = new List<Entry>();

        public bool TryGet(string id, out AudioClip clip) => Find(sfx, id, out clip);
        public bool TryGetMusic(string id, out AudioClip clip) => Find(music, id, out clip);

        static bool Find(List<Entry> list, string id, out AudioClip clip)
        {
            foreach (var e in list) if (e.id == id) { clip = e.clip; return clip != null; }
            clip = null;
            return false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua.Gameplay
{
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

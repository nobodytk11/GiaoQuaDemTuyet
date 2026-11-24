using System.Collections.Generic;
using UnityEngine;

namespace GiaoQua
{
    /// <summary>Where the save text is kept. PlayerPrefs works on every platform including WebGL.</summary>
    public interface IKeyValueStore
    {
        string Get(string key);
        void Set(string key, string value);
        void Delete(string key);
    }

    public class PlayerPrefsStore : IKeyValueStore
    {
        public string Get(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        public void Set(string key, string value) { PlayerPrefs.SetString(key, value); PlayerPrefs.Save(); }
        public void Delete(string key) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
    }

    /// <summary>Used by tests so they never touch the real save.</summary>
    public class MemoryStore : IKeyValueStore
    {
        readonly Dictionary<string, string> map = new Dictionary<string, string>();
        public string Get(string key) => map.TryGetValue(key, out var v) ? v : null;
        public void Set(string key, string value) => map[key] = value;
        public void Delete(string key) => map.Remove(key);
    }
}

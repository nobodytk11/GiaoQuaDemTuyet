using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Cycles through frames, for the two-picture walk and fly animations of the enemies.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 6f;
        SpriteRenderer sr;

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            int i = Mathf.FloorToInt(Time.time * fps + GetInstanceID() % 7) % frames.Length;
            if (i < 0) i += frames.Length;
            sr.sprite = frames[i];
        }
    }
}

using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Applies the chosen hero skin (picture set and animation) when the level starts.</summary>
    public class PlayerSkin : MonoBehaviour
    {
        public string[] ids = { "p1", "p2", "p3" };
        public RuntimeAnimatorController[] controllers;
        public Sprite[] idleSprites;

        void Start()
        {
            string wanted = SaveService.Data.skin;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] != wanted) continue;
                var animator = GetComponent<Animator>();
                if (animator != null && i < controllers.Length) animator.runtimeAnimatorController = controllers[i];
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null && i < idleSprites.Length) sr.sprite = idleSprites[i];
                return;
            }
        }
    }
}

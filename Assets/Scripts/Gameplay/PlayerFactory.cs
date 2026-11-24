using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Builds the hero's physical parts in code. The editor tools and the tests both use it, so they always agree.</summary>
    public static class PlayerFactory
    {
        public static readonly Vector2 ColliderSize = new Vector2(0.62f, 1.2f);

        public static GameObject Create(Vector2 feet, Sprite sprite = null, RuntimeAnimatorController controller = null, bool useSave = false)
        {
            var go = new GameObject("Player") { layer = LayerMask.NameToLayer("Player"), tag = "Player" };
            go.transform.position = feet;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : ProceduralSprites.Square(new Color(0.3f, 0.8f, 0.5f));
            if (sprite == null) go.transform.localScale = new Vector3(ColliderSize.x, ColliderSize.y, 1f);
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;

            var box = go.AddComponent<BoxCollider2D>();
            if (sprite == null) { box.size = Vector2.one; box.offset = new Vector2(0f, 0.5f); }
            else { box.size = ColliderSize; box.offset = new Vector2(0f, ColliderSize.y / 2f); }
            box.sharedMaterial = new PhysicsMaterial2D("Hero") { friction = 0f, bounciness = 0f };

            if (controller != null)
            {
                var animator = go.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }

            go.AddComponent<PlayerPowerUps>();
            var pc = go.AddComponent<PlayerController>();
            pc.useSaveForSkin = useSave;
            go.AddComponent<PlayerHealth>();
            go.AddComponent<PlayerShooter>();
            return go;
        }
    }
}

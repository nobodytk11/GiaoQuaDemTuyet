using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>A background layer that scrolls slower than the camera. Three copies repeat sideways.</summary>
    public class ParallaxLayer : MonoBehaviour
    {
        public float factor = 0.2f;          // 0 = fixed to the screen, 1 = moves with the world
        public float tileWidth = 14.6f;      // world width of one copy
        public Vector2 offset;               // where the layer sits relative to the camera
        public Camera cam;
        Transform[] copies;

        void Init()
        {
            if (cam == null) cam = Camera.main;
            copies = new Transform[transform.childCount];
            for (int i = 0; i < copies.Length; i++) copies[i] = transform.GetChild(i);
        }

        void Start() { Init(); }

        void LateUpdate()
        {
            if (copies == null) Init();
            if (cam == null) { cam = Camera.main; if (cam == null) return; }
            Vector3 c = cam.transform.position;
            float scroll = -c.x * factor;
            float wrapped = scroll - Mathf.Floor(scroll / tileWidth) * tileWidth;   // 0..tileWidth
            for (int i = 0; i < copies.Length; i++)
                copies[i].position = new Vector3(c.x + wrapped - tileWidth * (i - 0.5f) - tileWidth * copies.Length / 2f + tileWidth,
                                                 c.y + offset.y, copies[i].position.z);
        }
    }
}

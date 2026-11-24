using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Follows the hero, looks a little ahead, and never shows outside the level bounds.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public Rect bounds = new Rect(-100, -20, 400, 60);   // world rectangle the view must stay inside
        public float smoothTime = 0.18f;
        public float lookAhead = 2f;
        public float heightOffset = 1.6f;
        public float orthographicSize = 5.2f;

        Camera cam;
        Vector3 velocity;
        float aheadX;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }

        void Start()
        {
            if (target == null && PlayerController.Current != null) target = PlayerController.Current.transform;
            if (target != null) SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            transform.position = Clamp(new Vector3(target.position.x, target.position.y + heightOffset, -10f));
            velocity = Vector3.zero;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                if (PlayerController.Current != null) target = PlayerController.Current.transform; else return;
            }
            var pc = PlayerController.Current;
            float wantAhead = pc != null ? pc.Facing * lookAhead : 0f;
            aheadX = Mathf.Lerp(aheadX, wantAhead, 2f * Time.deltaTime);
            var want = new Vector3(target.position.x + aheadX, target.position.y + heightOffset, -10f);
            transform.position = Clamp(Vector3.SmoothDamp(transform.position, want, ref velocity, smoothTime));
        }

        Vector3 Clamp(Vector3 p)
        {
            float halfH = orthographicSize;
            float halfW = orthographicSize * (cam != null ? cam.aspect : 16f / 9f);
            float x = bounds.width <= halfW * 2f ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
            float y = bounds.height <= halfH * 2f ? bounds.center.y : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
            return new Vector3(x, y, -10f);
        }
    }
}

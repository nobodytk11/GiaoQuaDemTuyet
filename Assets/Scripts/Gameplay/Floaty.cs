using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>Gentle up-and-down bobbing for coins and items.</summary>
    public class Floaty : MonoBehaviour
    {
        public float height = 0.12f;
        public float speed = 2.2f;
        Vector3 start;
        float phase;

        void Start() { start = transform.position; phase = start.x * 0.7f; }
        void Update() { transform.position = start + Vector3.up * (Mathf.Sin(Time.time * speed + phase) * height); }
    }
}

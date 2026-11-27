using UnityEngine;
using UnityEngine.UI;

namespace GiaoQua.UI
{
    /// <summary>
    /// Keeps the whole 720-unit height of the screens on tall-and-wide phone screens (so nothing is cut at the bottom),
    /// and the whole 1280-unit width on narrower screens.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasAspectMatch : MonoBehaviour
    {
        CanvasScaler scaler;

        void Awake() { scaler = GetComponent<CanvasScaler>(); Apply(); }
        void Update() { Apply(); }

        void Apply()
        {
            if (Screen.height <= 0) return;
            float aspect = (float)Screen.width / Screen.height;
            scaler.matchWidthOrHeight = aspect >= 16f / 9f ? 1f : 0f;
        }
    }
}

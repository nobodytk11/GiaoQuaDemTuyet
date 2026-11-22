using UnityEngine;
using UnityEngine.InputSystem;

namespace GiaoQua
{
    /// <summary>
    /// Smoke-test screen: shows version, platform, resolution, frame rate and which keys are held,
    /// and writes one line to the player log. Used to prove a build starts on another computer.
    /// </summary>
    public class HelloScreen : MonoBehaviour
    {
        float fps;
        float frameTimer;
        int frameCount;

        void Start()
        {
            Debug.Log($"[Hello] Giao Qua Dem Tuyet started. Unity {Application.unityVersion}, " +
                      $"platform {Application.platform}, {Screen.width}x{Screen.height}, " +
                      $"graphics {SystemInfo.graphicsDeviceType}");
        }

        void Update()
        {
            frameCount++;
            frameTimer += Time.unscaledDeltaTime;
            if (frameTimer >= 0.5f)
            {
                fps = frameCount / frameTimer;
                frameCount = 0;
                frameTimer = 0f;
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void OnGUI()
        {
            var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.06f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var body = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.035f), alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(1f, 0.85f, 0.3f);
            body.normal.textColor = Color.white;

            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0, h * 0.18f, w, h * 0.1f), "Giao Quà Đêm Tuyết", title);
            GUI.Label(new Rect(0, h * 0.30f, w, h * 0.07f), "Bản thử exe #0", body);

            Keyboard kb = Keyboard.current;
            string keys = kb == null ? "không thấy bàn phím" :
                $"A:{(kb.aKey.isPressed ? "x" : "-")}  D:{(kb.dKey.isPressed ? "x" : "-")}  Space:{(kb.spaceKey.isPressed ? "x" : "-")}";
            GUI.Label(new Rect(0, h * 0.42f, w, h * 0.07f), "Bấm A, D, Space để thử bàn phím: " + keys, body);
            GUI.Label(new Rect(0, h * 0.52f, w, h * 0.07f), $"Unity {Application.unityVersion}  ·  {Application.platform}  ·  {Screen.width}x{Screen.height}", body);
            GUI.Label(new Rect(0, h * 0.62f, w, h * 0.07f), $"{fps:0} khung hình/giây  ·  {SystemInfo.graphicsDeviceType}", body);
            GUI.Label(new Rect(0, h * 0.82f, w, h * 0.07f), "Esc để thoát", body);
        }
    }
}

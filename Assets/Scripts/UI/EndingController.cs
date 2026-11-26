using UnityEngine;
using UnityEngine.SceneManagement;
using GiaoQua.Gameplay;

namespace GiaoQua.UI
{
    /// <summary>The final screen after the last level: thanks, totals and credits.</summary>
    public class EndingController : MonoBehaviour
    {
        System.Collections.IEnumerator BackToMenuAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            GameSession.EndAutoplay();
            SceneManager.LoadScene(LevelCatalog.MenuScene);
        }

        void Start()
        {
            Time.timeScale = 1f;
            var canvas = UIKit.CreateCanvas("EndingCanvas", 5);
            canvas.transform.SetParent(transform, false);
            UIKit.Label(canvas.transform, "Chúc mừng!", 90, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 120f), UIKit.Gold, TextAnchor.MiddleCenter, true, FontStyle.Bold);
            UIKit.Label(canvas.transform, "Bạn đã giao hết quà trong đêm tuyết", 40, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 60f), Color.white);
            int stars = 0;
            foreach (int s in SaveService.Data.stars) stars += s;
            UIKit.Label(canvas.transform, $"Tổng số sao: {stars}/{LevelCatalog.Count * 3}      Xu đang có: {SaveService.Data.coins}", 30, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1000f, 50f), UIKit.Gold);
            UIKit.Label(canvas.transform,
                "Nhóm 6\nHuỳnh Thiên An (23120001) · Phạm Bảo Thắng (23120416)\n\n" +
                "Hình ảnh: Kenney (CC0) và hình do nhóm tự vẽ\n" +
                "Âm thanh: Juhani Junkala, 3xBlast, Cleyton Kauffman (CC0)\n" +
                "Chữ: Baloo 2 (SIL Open Font License)",
                26, new Vector2(0.5f, 1f), new Vector2(0f, -310f), new Vector2(1000f, 230f), Color.white);
            UIKit.Label(canvas.transform, StoryCatalog.EndingMessage, 28, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(1000f, 50f), new Color(0.75f, 0.88f, 1f));
            var back = UIKit.MakeButton(canvas.transform, "Về menu", ButtonStyle.Green, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 74f), () => SceneManager.LoadScene(LevelCatalog.MenuScene));
            UIKit.Focus(back);
            GameAudio.Play("win");
            GameAudio.PlayMusic("menu");
            if (GameSession.Autoplay) StartCoroutine(BackToMenuAfter(6f));
        }
    }
}

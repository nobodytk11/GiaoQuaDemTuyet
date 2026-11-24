using System.Collections;
using UnityEngine;

namespace GiaoQua.Gameplay
{
    /// <summary>A platform that crumbles about a second after the hero stands on it, then comes back.</summary>
    public class MeltingPlatform : MonoBehaviour
    {
        public float meltDelay = 1f;
        public float returnAfter = 3f;
        bool running;

        void OnCollisionEnter2D(Collision2D c)
        {
            if (running || c.collider.GetComponentInParent<PlayerController>() == null) return;
            var hero = c.collider.GetComponentInParent<PlayerController>();
            if (hero.transform.position.y < c.otherCollider.bounds.max.y - 0.2f) return;   // only when standing on top
            StartCoroutine(Melt());
        }

        IEnumerator Melt()
        {
            running = true;
            var sr = GetComponentInChildren<SpriteRenderer>();
            var col = GetComponent<Collider2D>();
            Vector3 home = transform.position;
            for (float t = 0f; t < meltDelay; t += Time.deltaTime)
            {
                transform.position = home + new Vector3(Mathf.Sin(t * 60f) * 0.04f, 0f, 0f);
                yield return null;
            }
            transform.position = home;
            if (col != null) col.enabled = false;
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(returnAfter);
            if (col != null) col.enabled = true;
            if (sr != null) sr.enabled = true;
            running = false;
        }
    }
}

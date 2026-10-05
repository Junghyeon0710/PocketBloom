using TMPro;
using UnityEngine;

namespace PocketBloom
{
    // 결과 값은 바꾸지 않고 표시만 연출한다. 동작 감소 설정이면 즉시 최종 상태를 표시한다.
    public sealed class BloomCelebration : MonoBehaviour
    {
        RectTransform card;
        TMP_Text score;
        CanvasGroup group;
        RectTransform[] awards;
        BloomGraphic[] petals;
        Vector2[] origins;
        Color[] colors;
        int finalScore;
        float started;

        public void Setup(RectTransform surface, TMP_Text label, int points, RectTransform[] flowers, BloomGraphic[] particles, bool reducedMotion)
        {
            card = surface; score = label; finalScore = points; awards = flowers; petals = particles;
            origins = new Vector2[petals.Length]; colors = new Color[petals.Length];
            for (int i = 0; i < petals.Length; i++) { origins[i] = petals[i].rectTransform.anchoredPosition; colors[i] = petals[i].color; }
            if (reducedMotion) { enabled = false; return; }
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; started = Time.unscaledTime;
            Paint(0);
        }
        void Update()
        {
            float elapsed = Time.unscaledTime - started;
            Paint(elapsed);
            if (elapsed >= 3) enabled = false;
        }
        void Paint(float elapsed)
        {
            float reveal = 1 - Mathf.Pow(1 - Mathf.Clamp01(elapsed / .32f), 3);
            card.localScale = Vector3.one * Mathf.Lerp(.94f, 1, reveal);
            group.alpha = reveal;
            float counter = 1 - Mathf.Pow(1 - Mathf.Clamp01(elapsed / .72f), 3);
            score.text = Mathf.RoundToInt(finalScore * counter).ToString("N0");
            for (int i = 0; i < awards.Length; i++)
            {
                float t = Mathf.Clamp01((elapsed - .22f - i * .12f) / .28f);
                awards[i].localScale = Vector3.one * Mathf.Lerp(.72f, 1, Mathf.SmoothStep(0, 1, t));
            }
            for (int i = 0; i < petals.Length; i++)
            {
                var r = petals[i].rectTransform;
                r.anchoredPosition = origins[i] + new Vector2(Mathf.Sin(elapsed * 2 + i) * 16, -elapsed * (70 + i % 4 * 18));
                r.localRotation = Quaternion.Euler(0, 0, elapsed * (i % 2 == 0 ? 38 : -32));
                var color = colors[i]; color.a *= Mathf.Clamp01((3 - elapsed) / .9f); petals[i].color = color;
            }
        }
    }
}

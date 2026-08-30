using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FloatingText : MonoBehaviour
    {
        public void Init(string text, Color color, float fontSize = 20f)
        {
            var label = GetComponent<TextMeshProUGUI>();
            label.text          = text;
            label.color         = color;
            label.fontSize      = fontSize;
            label.fontStyle     = FontStyles.Bold;
            label.alignment     = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            // Outline for legibility
            label.outlineWidth = 0.25f;
            label.outlineColor = new Color32(0, 0, 0, 200);

            StartCoroutine(FloatRoutine(label));
        }

        private IEnumerator FloatRoutine(TextMeshProUGUI label)
        {
            var rt = GetComponent<RectTransform>();
            const float duration = 1.7f;
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            Color startColor = label.color;

            // Pop scale on spawn
            rt.localScale = Vector3.zero;
            float popDur = 0.15f;
            float popE = 0f;
            while (popE < popDur)
            {
                popE += Time.deltaTime;
                float s = Mathf.LerpUnclamped(0f, 1.2f, popE / popDur);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            // Settle back
            float settleDur = 0.08f;
            float settleE = 0f;
            while (settleE < settleDur)
            {
                settleE += Time.deltaTime;
                float s = Mathf.Lerp(1.2f, 1f, settleE / settleDur);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;

            // Float up and fade
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Rise fast then slow
                float rise = Mathf.Sqrt(t) * 100f;
                rt.anchoredPosition = startPos + Vector2.up * rise;
                // Slight horizontal drift
                rt.anchoredPosition += new Vector2(Mathf.Sin(elapsed * 3f) * 6f, 0f);
                // Fade only in last 40%
                float alpha = t < 0.6f ? 1f : 1f - ((t - 0.6f) / 0.4f);
                label.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}

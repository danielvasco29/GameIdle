using System.Collections;
using TMPro;
using UnityEngine;

namespace GameIdle
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FloatingText : MonoBehaviour
    {
        public void Init(string text, Color color)
        {
            var label = GetComponent<TextMeshProUGUI>();
            label.text        = text;
            label.color       = color;
            label.fontSize    = 20;
            label.fontStyle   = TMPro.FontStyles.Bold;
            label.alignment   = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            StartCoroutine(FloatRoutine(label));
        }

        private IEnumerator FloatRoutine(TextMeshProUGUI label)
        {
            var rt = GetComponent<RectTransform>();
            const float duration = 1.5f;
            float elapsed = 0f;
            Vector2 startPos   = rt.anchoredPosition;
            Color   startColor = label.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = startPos + Vector2.up * (90f * t);
                label.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}

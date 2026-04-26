using System.Collections;
using TMPro;
using UnityEngine;

namespace GameIdle
{
    public class FloatingText : MonoBehaviour
    {
        [HideInInspector] public TextMeshProUGUI label;
        [HideInInspector] public RectTransform   rt;
        [HideInInspector] public CanvasGroup     cg;

        private const float Duration = 1.15f;
        private const float Rise     = 90f;

        public static void Spawn(Transform parent, Vector2 localPos, string text, Color color)
        {
            var go = new GameObject("FloatingText", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);

            var ft  = go.AddComponent<FloatingText>();
            ft.rt   = (RectTransform)go.transform;
            ft.rt.anchoredPosition = localPos;
            ft.rt.sizeDelta        = new Vector2(200, 48);

            ft.cg = go.GetComponent<CanvasGroup>();
            ft.cg.raycastTarget  = false;
            ft.cg.blocksRaycasts = false;

            var labelGo = new GameObject("L", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var lr = (RectTransform)labelGo.transform;
            lr.anchorMin  = Vector2.zero;
            lr.anchorMax  = Vector2.one;
            lr.sizeDelta  = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.color         = color;
            tmp.fontSize      = 26;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            ft.label = tmp;

            ft.StartCoroutine(ft.Animate());
        }

        private IEnumerator Animate()
        {
            Vector2 start = rt.anchoredPosition;
            Vector2 end   = start + Vector2.up * Rise;

            for (float t = 0; t < Duration; t += Time.deltaTime)
            {
                float p   = t / Duration;
                float pos = 1f - Mathf.Pow(1f - Mathf.Min(p * 1.5f, 1f), 2f);
                rt.anchoredPosition = Vector2.Lerp(start, end, pos);
                cg.alpha = p < 0.35f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.35f) / 0.65f);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

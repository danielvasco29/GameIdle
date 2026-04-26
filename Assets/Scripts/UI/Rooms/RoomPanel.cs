using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Generic room panel: full-bleed background art, themed title stripe,
    // and either a list of choice buttons OR a stats body with one CTA.
    public class RoomPanel : MonoBehaviour
    {
        public static RoomPanel Instance { get; private set; }

        public struct Choice
        {
            public string label;
            public string desc;
            public Color  color;
            public Action effect;
        }

        public class Config
        {
            public string  title;
            public Color   accent;
            public Sprite  background;
            public Choice[] choices;          // for CEO / Reunioes / Pesquisa
            public string   statsBody;        // for Relatorios — overrides choices
            public string   statsActionLabel;
            public Action   statsAction;
        }

        private PanelAnimator anim;
        private PanelAnimator Anim => anim != null ? anim : (anim = gameObject.AddComponent<PanelAnimator>());

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public static RoomPanel Create(Canvas canvas)
        {
            var go = new GameObject("RoomPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);

            var rt          = (RectTransform)go.transform;
            rt.anchorMin    = new Vector2(0.06f, 0.08f);
            rt.anchorMax    = new Vector2(0.94f, 0.92f);
            rt.sizeDelta    = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var bg          = go.GetComponent<Image>();
            bg.color        = new Color(0.04f, 0.04f, 0.10f, 0.97f);

            var panel       = go.AddComponent<RoomPanel>();
            go.SetActive(false);
            return panel;
        }

        public void Show(Config config)
        {
            BuildContent(config);
            Anim.Open();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildContent(Config c)
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            // Background art (slightly desaturated by tint)
            if (c.background != null)
            {
                var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(transform, false);
                var bgRt        = (RectTransform)bgGo.transform;
                bgRt.anchorMin  = Vector2.zero;
                bgRt.anchorMax  = Vector2.one;
                bgRt.sizeDelta  = Vector2.zero;
                var bgImg       = bgGo.GetComponent<Image>();
                bgImg.sprite    = c.background;
                bgImg.color     = new Color(0.85f, 0.85f, 0.95f, 1f);
                bgImg.preserveAspect = false;
                bgImg.raycastTarget  = false;
            }

            // Dark legibility overlay over the art
            var ovGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            ovGo.transform.SetParent(transform, false);
            var ovRt        = (RectTransform)ovGo.transform;
            ovRt.anchorMin  = Vector2.zero;
            ovRt.anchorMax  = Vector2.one;
            ovRt.sizeDelta  = Vector2.zero;
            var ovImg       = ovGo.GetComponent<Image>();
            ovImg.color     = new Color(0f, 0f, 0f, 0.55f);
            ovImg.raycastTarget = false;

            // Title stripe (themed colour band at top)
            var stripeGo = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripeGo.transform.SetParent(transform, false);
            var stRt        = (RectTransform)stripeGo.transform;
            stRt.anchorMin  = new Vector2(0f, 0.86f);
            stRt.anchorMax  = new Vector2(1f, 1f);
            stRt.sizeDelta  = Vector2.zero;
            var stImg       = stripeGo.GetComponent<Image>();
            stImg.color     = new Color(c.accent.r * 0.45f, c.accent.g * 0.45f, c.accent.b * 0.45f, 0.92f);
            stImg.raycastTarget = false;

            // Thin accent line under stripe
            var lineGo = new GameObject("Line", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(transform, false);
            var lnRt        = (RectTransform)lineGo.transform;
            lnRt.anchorMin  = new Vector2(0f, 0.855f);
            lnRt.anchorMax  = new Vector2(1f, 0.86f);
            lnRt.sizeDelta  = Vector2.zero;
            var lnImg       = lineGo.GetComponent<Image>();
            lnImg.color     = c.accent;
            lnImg.raycastTarget = false;

            // Title
            AddText(transform, "Title", c.title,
                new Vector2(0.04f, 0.86f), new Vector2(0.96f, 1f),
                26f, c.accent, FontStyles.Bold, TextAlignmentOptions.Center, outline: true);

            // Body
            if (c.choices != null && c.choices.Length > 0)
                BuildChoices(c.choices);
            else if (!string.IsNullOrEmpty(c.statsBody))
                BuildStats(c);

            BuildCloseButton();
        }

        private void BuildChoices(Choice[] choices)
        {
            int   n      = choices.Length;
            float top    = 0.80f;
            float bottom = 0.18f;
            float gap    = 0.02f;
            float each   = ((top - bottom) - gap * Mathf.Max(0, n - 1)) / n;

            for (int i = 0; i < n; i++)
            {
                float yMax = top - i * (each + gap);
                float yMin = yMax - each;
                BuildChoiceButton(i, choices[i], yMin, yMax);
            }
        }

        private void BuildChoiceButton(int idx, Choice choice, float yMin, float yMax)
        {
            var go      = new GameObject($"Choice{idx}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt      = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.05f, yMin);
            rt.anchorMax = new Vector2(0.95f, yMax);
            rt.sizeDelta = Vector2.zero;

            var img     = go.GetComponent<Image>();
            img.color   = new Color(choice.color.r, choice.color.g, choice.color.b, 0.92f);

            var btn     = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var captured = choice;
            btn.onClick.AddListener(() => { captured.effect?.Invoke(); Anim.Close(); });
            go.AddComponent<AnimatedButton>();

            // Left accent bar
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(go.transform, false);
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = new Vector2(0f, 0.08f);
            barRt.anchorMax = new Vector2(0.018f, 0.92f);
            barRt.sizeDelta = Vector2.zero;
            var barImg = bar.GetComponent<Image>();
            barImg.color = new Color(1f, 1f, 1f, 0.65f);
            barImg.raycastTarget = false;

            AddText(go.transform, "Label", choice.label,
                new Vector2(0.04f, 0.45f), new Vector2(0.62f, 1f),
                17f, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            AddText(go.transform, "Desc", choice.desc,
                new Vector2(0.62f, 0.05f), new Vector2(0.97f, 0.95f),
                12f, new Color(1f, 1f, 1f, 0.88f), FontStyles.Normal, TextAlignmentOptions.Center);
        }

        private void BuildStats(Config c)
        {
            var box = new GameObject("Stats", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(transform, false);
            var rt          = (RectTransform)box.transform;
            rt.anchorMin    = new Vector2(0.08f, 0.32f);
            rt.anchorMax    = new Vector2(0.92f, 0.82f);
            rt.sizeDelta    = Vector2.zero;
            var img         = box.GetComponent<Image>();
            img.color       = new Color(0f, 0f, 0f, 0.55f);
            img.raycastTarget = false;

            AddText(box.transform, "Body", c.statsBody,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
                17f, Color.white, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

            if (c.statsAction != null && !string.IsNullOrEmpty(c.statsActionLabel))
            {
                var bgo = new GameObject("Action", typeof(RectTransform), typeof(Image));
                bgo.transform.SetParent(transform, false);
                var brt = (RectTransform)bgo.transform;
                brt.anchorMin = new Vector2(0.28f, 0.19f);
                brt.anchorMax = new Vector2(0.72f, 0.28f);
                brt.sizeDelta = Vector2.zero;
                var bImg = bgo.GetComponent<Image>();
                bImg.color = new Color(c.accent.r, c.accent.g, c.accent.b, 0.92f);
                var bBtn = bgo.AddComponent<Button>();
                bBtn.targetGraphic = bImg;
                var capturedAction = c.statsAction;
                bBtn.onClick.AddListener(() => { capturedAction(); Anim.Close(); });
                bgo.AddComponent<AnimatedButton>();

                AddText(bgo.transform, "Lbl", c.statsActionLabel,
                    Vector2.zero, Vector2.one,
                    17f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, outline: true);
            }
        }

        private void BuildCloseButton()
        {
            var go = new GameObject("Close", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt          = (RectTransform)go.transform;
            rt.anchorMin    = new Vector2(0.40f, 0.045f);
            rt.anchorMax    = new Vector2(0.60f, 0.13f);
            rt.sizeDelta    = Vector2.zero;
            var img         = go.GetComponent<Image>();
            img.color       = new Color(0.20f, 0.20f, 0.25f, 0.95f);
            var btn         = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Anim.Close());
            go.AddComponent<AnimatedButton>();
            AddText(go.transform, "Lbl", "Fechar",
                Vector2.zero, Vector2.one,
                14f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void AddText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            float fontSize, Color color, FontStyles style, TextAlignmentOptions align,
            bool outline = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt          = (RectTransform)go.transform;
            rt.anchorMin    = anchorMin;
            rt.anchorMax    = anchorMax;
            rt.sizeDelta    = new Vector2(-8f, 0f);
            rt.anchoredPosition = Vector2.zero;

            var tmp         = go.AddComponent<TextMeshProUGUI>();
            tmp.text        = text;
            tmp.fontSize    = fontSize;
            tmp.color       = color;
            tmp.fontStyle   = outline ? (style | FontStyles.Bold) : style;
            tmp.alignment   = align;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            // Avoid tmp.outlineWidth here — it touches m_sharedMaterial
            // which isn't initialized for runtime-added TMPs and throws
            // ArgumentNullException ("source"), aborting the caller.
        }
    }
}

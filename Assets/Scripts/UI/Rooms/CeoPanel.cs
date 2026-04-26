using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class CeoPanel : MonoBehaviour
    {
        public static CeoPanel Instance { get; private set; }

        private PanelAnimator anim;
        private PanelAnimator Anim => anim != null ? anim : (anim = gameObject.AddComponent<PanelAnimator>());

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Called from UIManager to build the panel on the Canvas
        public static CeoPanel Create(Canvas canvas)
        {
            var go      = new GameObject("CeoPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);

            var rt          = (RectTransform)go.transform;
            rt.anchorMin    = new Vector2(0.1f, 0.15f);
            rt.anchorMax    = new Vector2(0.9f, 0.85f);
            rt.sizeDelta    = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var bg          = go.GetComponent<Image>();
            bg.color        = new Color(0.04f, 0.04f, 0.12f, 0.97f);

            var panel       = go.AddComponent<CeoPanel>();
            go.SetActive(false);
            return panel;
        }

        public void Show()
        {
            BuildContent();
            Anim.Open();
        }

        private void BuildContent()
        {
            // Clear stale children (except PanelAnimator's CanvasGroup managed externally)
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            // ── Title ───────────────────────────────────────────────────────
            AddText("TitleBar", "DECISÃO DO CEO",
                anchorMin: new Vector2(0f, 0.82f), anchorMax: Vector2.one,
                fontSize: 20f, color: new Color(1f, 0.85f, 0.1f),
                style: FontStyles.Bold, align: TextAlignmentOptions.Center);

            // Separator line
            var sep = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(transform, false);
            var sepRt       = (RectTransform)sep.transform;
            sepRt.anchorMin = new Vector2(0.03f, 0.80f);
            sepRt.anchorMax = new Vector2(0.97f, 0.81f);
            sepRt.sizeDelta = Vector2.zero;
            sep.GetComponent<Image>().color = new Color(1f, 0.85f, 0.1f, 0.4f);

            // ── Choice definitions ────────────────────────────────────────────
            var choices = new (string label, string desc, Color color, Action effect)[]
            {
                (
                    "Hackathon Interno",
                    "+100% produção\npor 25 segundos",
                    new Color(0.2f, 0.5f, 1.0f),
                    () => {
                        GameManager.Instance.ApplyEffect(new EventEffect {
                            eventId="ceo_hack", type=EffectType.ProductionModifier,
                            value=1.0f, duration=25f, timeRemaining=25f });
                        UIManager.Instance.ShowToast("Hackathon! +100% produção por 25s");
                    }
                ),
                (
                    "Rodada de Investimento",
                    "+3 minutos de receita\ninstantâneos",
                    new Color(0.2f, 0.75f, 0.35f),
                    () => {
                        double bonus = Math.Max(500.0, GameManager.Instance.MoneyPerSecond * 180.0);
                        GameManager.Instance.AddMoney(bonus);
                        UIManager.Instance.ShowToast($"Investimento! +${NumberFormatter.Format(bonus)}");
                    }
                ),
                (
                    "Aposta no Mercado  (!)",
                    "50% chance: triplicar dinheiro\nou perder 40%",
                    new Color(0.75f, 0.2f, 0.15f),
                    () => {
                        if (UnityEngine.Random.value >= 0.5f) {
                            double bonus = GameManager.Instance.Money * 2.0;
                            GameManager.Instance.AddMoney(bonus);
                            UIManager.Instance.ShowToast($"Jackpot! +${NumberFormatter.Format(bonus)}");
                        } else {
                            double loss = GameManager.Instance.Money * 0.4;
                            GameManager.Instance.SpendMoney(loss);
                            UIManager.Instance.ShowToast($"Mercado caiu! -${NumberFormatter.Format(loss)}");
                        }
                    }
                ),
            };

            float[] yMaxes = { 0.77f, 0.51f, 0.25f };

            for (int i = 0; i < choices.Length; i++)
            {
                int idx = i;
                var (label, desc, color, effect) = choices[i];
                float yMax = yMaxes[i];
                float yMin = yMax - 0.23f;

                var btnGo   = new GameObject($"Choice{i}", typeof(RectTransform), typeof(Image));
                btnGo.transform.SetParent(transform, false);

                var btnRt       = (RectTransform)btnGo.transform;
                btnRt.anchorMin = new Vector2(0.04f, yMin);
                btnRt.anchorMax = new Vector2(0.96f, yMax);
                btnRt.sizeDelta = Vector2.zero;

                var btnImg  = btnGo.GetComponent<Image>();
                btnImg.color = color;

                var btn     = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() => { effect(); Anim.Close(); });
                btnGo.AddComponent<AnimatedButton>();

                // Label (left side)
                AddTextTo(btnGo.transform, "BtnLabel", label,
                    anchorMin: new Vector2(0.02f, 0.5f), anchorMax: new Vector2(0.65f, 1f),
                    fontSize: 14f, color: Color.white,
                    style: FontStyles.Bold, align: TextAlignmentOptions.MidlineLeft);

                // Description (right side)
                AddTextTo(btnGo.transform, "BtnDesc", desc,
                    anchorMin: new Vector2(0.65f, 0f), anchorMax: Vector2.one,
                    fontSize: 11f, color: new Color(1f, 1f, 1f, 0.85f),
                    style: FontStyles.Normal, align: TextAlignmentOptions.Center);
            }

            // ── Close button ────────────────────────────────────────────────
            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image));
            closeGo.transform.SetParent(transform, false);
            var closeRt     = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(0.35f, 0.01f);
            closeRt.anchorMax = new Vector2(0.65f, 0.10f);
            closeRt.sizeDelta = Vector2.zero;
            var closeImg    = closeGo.GetComponent<Image>();
            closeImg.color  = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            var closeBtn    = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() => Anim.Close());
            closeGo.AddComponent<AnimatedButton>();
            AddTextTo(closeGo.transform, "CloseTxt", "Cancelar",
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                fontSize: 13f, color: Color.white,
                style: FontStyles.Normal, align: TextAlignmentOptions.Center);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void AddText(string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            float fontSize, Color color, FontStyles style, TextAlignmentOptions align)
        {
            AddTextTo(transform, name, text, anchorMin, anchorMax, fontSize, color, style, align);
        }

        private static void AddTextTo(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            float fontSize, Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go      = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt      = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = new Vector2(-8f, 0f);
            rt.anchoredPosition = Vector2.zero;

            var tmp         = go.AddComponent<TextMeshProUGUI>();
            tmp.text        = text;
            tmp.fontSize    = fontSize;
            tmp.color       = color;
            tmp.fontStyle   = style;
            tmp.alignment   = align;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}

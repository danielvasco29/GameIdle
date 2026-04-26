using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class RoomSystem : MonoBehaviour
    {
        public static RoomSystem Instance { get; private set; }

        private const float RoomCooldown = 60f;

        private static readonly string[] Names  = { "CEO", "Reuniões", "Pesquisa", "Relatórios" };
        private static readonly Color[]  Tints  = {
            new Color(0.6f, 0.3f, 1.0f),   // CEO – purple
            new Color(0.3f, 0.8f, 0.4f),   // Meetings – green
            new Color(0.2f, 0.6f, 1.0f),   // Research – blue
            new Color(1.0f, 0.7f, 0.2f),   // Reports – gold
        };

        private readonly float[]              timers   = new float[4];
        private readonly Button[]             buttons  = new Button[4];
        private readonly Image[]              overlays = new Image[4];
        private readonly Image[]              borders  = new Image[4];
        private readonly TextMeshProUGUI[]    labels   = new TextMeshProUGUI[4];

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Called from UIManager after scene texture is loaded
        public void BuildBar(Transform parent, Texture2D scene, int W, int H)
        {
            int botH  = Mathf.RoundToInt(H * 0.34f);
            int roomW = W / 4;

            // Bar container pinned to bottom of parent panel
            var barGo = new GameObject("RoomBar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(parent, false);

            var barRt           = (RectTransform)barGo.transform;
            barRt.anchorMin     = new Vector2(0f, 0f);
            barRt.anchorMax     = new Vector2(1f, 0f);
            barRt.pivot         = new Vector2(0.5f, 0f);
            barRt.sizeDelta     = new Vector2(0f, 110f);
            barRt.anchoredPosition = Vector2.zero;

            var barBg       = barGo.GetComponent<Image>();
            barBg.color     = new Color(0f, 0f, 0f, 0.75f);
            barBg.raycastTarget = false;

            for (int i = 0; i < 4; i++)
                BuildRoomButton(barGo.transform, i, scene, roomW, botH);
        }

        private void BuildRoomButton(Transform bar, int idx, Texture2D scene, int roomW, int botH)
        {
            var sprite = Sprite.Create(scene,
                new Rect(idx * roomW, 0, roomW, botH),
                new Vector2(0.5f, 0.5f));

            // Root button GO
            var go      = new GameObject($"Room{idx}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(bar, false);

            var rt          = (RectTransform)go.transform;
            rt.anchorMin    = new Vector2(idx / 4f,          0.04f);
            rt.anchorMax    = new Vector2((idx + 1) / 4f,    0.96f);
            rt.sizeDelta    = new Vector2(-6f, 0f);
            rt.anchoredPosition = Vector2.zero;

            var img         = go.GetComponent<Image>();
            img.sprite      = sprite;
            img.type        = Image.Type.Simple;
            img.preserveAspect = false;

            // Subtle tint overlay so image keeps its look but has room colour identity
            var tintGo  = new GameObject("Tint", typeof(RectTransform), typeof(Image));
            tintGo.transform.SetParent(go.transform, false);
            var tintRt  = (RectTransform)tintGo.transform;
            tintRt.anchorMin = Vector2.zero;
            tintRt.anchorMax = Vector2.one;
            tintRt.sizeDelta = Vector2.zero;
            var tintImg = tintGo.GetComponent<Image>();
            tintImg.color = new Color(Tints[idx].r, Tints[idx].g, Tints[idx].b, 0.18f);
            tintImg.raycastTarget = false;

            // Coloured border outline (thin strip just inside each edge)
            var borderGo    = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(go.transform, false);
            var borderRt    = (RectTransform)borderGo.transform;
            borderRt.anchorMin  = Vector2.zero;
            borderRt.anchorMax  = Vector2.one;
            borderRt.sizeDelta  = new Vector2(-4f, -4f);
            var borderImg   = borderGo.GetComponent<Image>();
            borderImg.color = new Color(Tints[idx].r, Tints[idx].g, Tints[idx].b, 0f);
            borderImg.raycastTarget = false;
            borders[idx] = borderImg;

            // Cooldown dark overlay
            var ovGo    = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            ovGo.transform.SetParent(go.transform, false);
            var ovRt    = (RectTransform)ovGo.transform;
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.sizeDelta = Vector2.zero;
            var ovImg   = ovGo.GetComponent<Image>();
            ovImg.color = Color.clear;
            ovImg.raycastTarget = false;
            overlays[idx] = ovImg;

            // Label area (bottom strip)
            var labelGo    = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt    = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0.38f);
            labelRt.sizeDelta = Vector2.zero;
            var lbg        = labelGo.AddComponent<Image>();
            lbg.color      = new Color(0f, 0f, 0f, 0.65f);
            lbg.raycastTarget = false;

            var textGo  = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(labelGo.transform, false);
            var textRt  = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            var tmp     = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text    = Names[idx];
            tmp.fontSize = 17f;
            tmp.color   = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;
            tmp.raycastTarget = false;
            labels[idx] = tmp;

            // Button
            var btn     = go.AddComponent<Button>();
            btn.targetGraphic = img;
            int capture = idx;
            btn.onClick.AddListener(() => OnClick(capture));
            go.AddComponent<AnimatedButton>();
            buttons[idx] = btn;
        }

        private void OnClick(int idx)
        {
            if (timers[idx] > 0f) return;

            switch (idx)
            {
                case 0: CeoPanel.Instance?.Show();          break;
                case 1: TriggerMeeting();                   break;
                case 2: TriggerResearch();                  break;
                case 3: TriggerReports();                   break;
            }

            timers[idx] = RoomCooldown;
        }

        private void Update()
        {
            // Pulse the border of any room that's available (timer == 0)
            float pulse = (Mathf.Sin(Time.time * 3.5f) + 1f) * 0.5f; // 0..1

            for (int i = 0; i < 4; i++)
            {
                if (timers[i] > 0f)
                {
                    timers[i] -= Time.deltaTime;

                    float t = Mathf.Clamp01(timers[i] / RoomCooldown);
                    overlays[i].color = new Color(0f, 0f, 0f, t * 0.72f);

                    if (labels[i] != null)
                        labels[i].text = timers[i] > 0f
                            ? $"{Mathf.CeilToInt(timers[i])}s"
                            : Names[i];

                    if (timers[i] <= 0f && borders[i] != null)
                        borders[i].color = new Color(Tints[i].r, Tints[i].g, Tints[i].b, 0f);
                }
                else if (borders[i] != null)
                {
                    // Available — gently pulse a coloured halo to invite a click
                    borders[i].color = new Color(Tints[i].r, Tints[i].g, Tints[i].b, 0.25f + pulse * 0.45f);
                }

                if (buttons[i] != null)
                    buttons[i].interactable = timers[i] <= 0f;

                if (timers[i] <= 0f && labels[i] != null && labels[i].text != Names[i])
                    labels[i].text = Names[i];
            }
        }

        // ── Room effects ─────────────────────────────────────────────────────

        private void TriggerMeeting()
        {
            var effect = new EventEffect
            {
                eventId       = "room_meeting",
                type          = EffectType.ProductionModifier,
                value         = 0.5f,
                duration      = 30f,
                timeRemaining = 30f,
                isPermanent   = false
            };
            GameManager.Instance.ApplyEffect(effect);
            UIManager.Instance.ShowToast("Reunião! +50% produção por 30s");
        }

        private void TriggerResearch()
        {
            double bonus = Math.Max(100.0, GameManager.Instance.MoneyPerSecond * 60.0);
            GameManager.Instance.AddMoney(bonus);
            UIManager.Instance.ShowToast($"Pesquisa concluída! +${NumberFormatter.Format(bonus)}");
        }

        private void TriggerReports()
        {
            double bonus = Math.Max(50.0, GameManager.Instance.MoneyPerSecond * 10.0);
            GameManager.Instance.AddMoney(bonus);
            UIManager.Instance.ShowToast(
                $"Relatório: ${NumberFormatter.Format(GameManager.Instance.TotalEarned)} ganhos total" +
                $"\nPrestígios: {GameManager.Instance.PrestigeCount}" +
                $"  +${NumberFormatter.Format(bonus)}");
        }
    }
}

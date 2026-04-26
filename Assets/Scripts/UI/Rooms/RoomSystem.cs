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
        private readonly Sprite[]             sprites  = new Sprite[4];

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

            // Cache room sprites — used by both the bar buttons and the full panel backgrounds
            for (int i = 0; i < 4; i++)
                sprites[i] = Sprite.Create(scene,
                    new Rect(i * roomW, 0, roomW, botH),
                    new Vector2(0.5f, 0.5f));

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
                BuildRoomButton(barGo.transform, i);
        }

        private void BuildRoomButton(Transform bar, int idx)
        {
            var go      = new GameObject($"Room{idx}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(bar, false);

            var rt          = (RectTransform)go.transform;
            rt.anchorMin    = new Vector2(idx / 4f,          0.04f);
            rt.anchorMax    = new Vector2((idx + 1) / 4f,    0.96f);
            rt.sizeDelta    = new Vector2(-6f, 0f);
            rt.anchoredPosition = Vector2.zero;

            var img         = go.GetComponent<Image>();
            img.sprite      = sprites[idx];
            img.type        = Image.Type.Simple;
            img.preserveAspect = false;

            // Subtle tint
            var tintGo  = new GameObject("Tint", typeof(RectTransform), typeof(Image));
            tintGo.transform.SetParent(go.transform, false);
            var tintRt  = (RectTransform)tintGo.transform;
            tintRt.anchorMin = Vector2.zero;
            tintRt.anchorMax = Vector2.one;
            tintRt.sizeDelta = Vector2.zero;
            var tintImg = tintGo.GetComponent<Image>();
            tintImg.color = new Color(Tints[idx].r, Tints[idx].g, Tints[idx].b, 0.18f);
            tintImg.raycastTarget = false;

            // Border halo (pulsed when available)
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

            // Cooldown overlay
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

            // Label strip
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
            if (RoomPanel.Instance == null) return;

            var config = BuildConfig(idx);
            if (config == null) return;

            config.background = sprites[idx];
            RoomPanel.Instance.Show(config);

            timers[idx] = RoomCooldown;
        }

        private void Update()
        {
            float pulse = (Mathf.Sin(Time.time * 3.5f) + 1f) * 0.5f;

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
                    borders[i].color = new Color(Tints[i].r, Tints[i].g, Tints[i].b, 0.25f + pulse * 0.45f);
                }

                if (buttons[i] != null)
                    buttons[i].interactable = timers[i] <= 0f;

                if (timers[i] <= 0f && labels[i] != null && labels[i].text != Names[i])
                    labels[i].text = Names[i];
            }
        }

        // ── Configs ─────────────────────────────────────────────────────────

        private RoomPanel.Config BuildConfig(int idx) => idx switch
        {
            0 => BuildCeoConfig(),
            1 => BuildMeetingConfig(),
            2 => BuildResearchConfig(),
            3 => BuildReportsConfig(),
            _ => null
        };

        private static RoomPanel.Config BuildCeoConfig()
        {
            return new RoomPanel.Config
            {
                title  = "DECISÃO DO CEO",
                accent = new Color(1f, 0.85f, 0.1f),
                choices = new[]
                {
                    new RoomPanel.Choice
                    {
                        label = "Hackathon Interno",
                        desc  = "+100% produção\npor 25s",
                        color = new Color(0.20f, 0.50f, 1.00f),
                        effect = () =>
                        {
                            GameManager.Instance.ApplyEffect(new EventEffect {
                                eventId="ceo_hack", type=EffectType.ProductionModifier,
                                value=1.0f, duration=25f, timeRemaining=25f });
                            UIManager.Instance.ShowToast("Hackathon! +100% produção por 25s");
                        }
                    },
                    new RoomPanel.Choice
                    {
                        label = "Rodada de Investimento",
                        desc  = "+3 minutos de receita\ninstantâneos",
                        color = new Color(0.20f, 0.75f, 0.35f),
                        effect = () =>
                        {
                            double bonus = Math.Max(500.0, GameManager.Instance.MoneyPerSecond * 180.0);
                            GameManager.Instance.AddMoney(bonus);
                            UIManager.Instance.ShowToast($"Investimento! +${NumberFormatter.Format(bonus)}");
                        }
                    },
                    new RoomPanel.Choice
                    {
                        label = "Aposta no Mercado  (!)",
                        desc  = "50% chance: triplicar dinheiro\nou perder 40%",
                        color = new Color(0.75f, 0.20f, 0.15f),
                        effect = () =>
                        {
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
                    },
                }
            };
        }

        private static RoomPanel.Config BuildMeetingConfig()
        {
            return new RoomPanel.Config
            {
                title  = "SALA DE REUNIÕES",
                accent = new Color(0.4f, 1f, 0.55f),
                choices = new[]
                {
                    new RoomPanel.Choice
                    {
                        label = "Reunião Relâmpago",
                        desc  = "+50% produção\npor 30s",
                        color = new Color(0.20f, 0.65f, 0.30f),
                        effect = () =>
                        {
                            GameManager.Instance.ApplyEffect(new EventEffect {
                                eventId="room_meeting", type=EffectType.ProductionModifier,
                                value=0.5f, duration=30f, timeRemaining=30f });
                            UIManager.Instance.ShowToast("Reunião! +50% produção por 30s");
                        }
                    },
                    new RoomPanel.Choice
                    {
                        label = "Brainstorm Estratégico",
                        desc  = "+25% produção\npor 90s (mais longo)",
                        color = new Color(0.30f, 0.50f, 0.85f),
                        effect = () =>
                        {
                            GameManager.Instance.ApplyEffect(new EventEffect {
                                eventId="room_brainstorm", type=EffectType.ProductionModifier,
                                value=0.25f, duration=90f, timeRemaining=90f });
                            UIManager.Instance.ShowToast("Brainstorm! +25% produção por 90s");
                        }
                    },
                    new RoomPanel.Choice
                    {
                        label = "All-Hands Motivacional",
                        desc  = "+15% multiplicador\npermanente até prestige",
                        color = new Color(0.85f, 0.55f, 0.25f),
                        effect = () =>
                        {
                            GameManager.Instance.ApplyEffect(new EventEffect {
                                eventId="room_allhands", type=EffectType.MultiplierModifier,
                                value=0.15f, duration=0f, timeRemaining=0f, isPermanent=true });
                            UIManager.Instance.ShowToast("All-Hands! +15% multiplicador permanente");
                        }
                    },
                }
            };
        }

        private static RoomPanel.Config BuildResearchConfig()
        {
            return new RoomPanel.Config
            {
                title  = "LABORATÓRIO DE PESQUISA",
                accent = new Color(0.35f, 0.75f, 1f),
                choices = new[]
                {
                    new RoomPanel.Choice
                    {
                        label = "Sprint de R&D",
                        desc  = "Bônus instantâneo\n= 60s de produção",
                        color = new Color(0.20f, 0.55f, 0.95f),
                        effect = () =>
                        {
                            double bonus = Math.Max(100.0, GameManager.Instance.MoneyPerSecond * 60.0);
                            GameManager.Instance.AddMoney(bonus);
                            UIManager.Instance.ShowToast($"Pesquisa concluída! +${NumberFormatter.Format(bonus)}");
                        }
                    },
                    new RoomPanel.Choice
                    {
                        label = "Patente Aprovada",
                        desc  = "+5% multiplicador\npermanente até prestige",
                        color = new Color(0.55f, 0.30f, 0.85f),
                        effect = () =>
                        {
                            GameManager.Instance.ApplyEffect(new EventEffect {
                                eventId="room_patent_" + UnityEngine.Random.Range(0, int.MaxValue),
                                type=EffectType.MultiplierModifier,
                                value=0.05f, duration=0f, timeRemaining=0f, isPermanent=true });
                            UIManager.Instance.ShowToast("Patente! +5% multiplicador permanente");
                        }
                    },
                }
            };
        }

        private static RoomPanel.Config BuildReportsConfig()
        {
            var gm = GameManager.Instance;

            string body =
                $"<b>Dinheiro atual:</b>     ${NumberFormatter.Format(gm.Money)}\n" +
                $"<b>Total ganho:</b>        ${NumberFormatter.Format(gm.TotalEarned)}\n" +
                $"<b>Produção/segundo:</b>   ${NumberFormatter.Format(gm.MoneyPerSecond)}/s\n" +
                $"<b>Multiplicador prestige:</b> x{gm.PrestigeMultiplier:F2}\n" +
                $"<b>Prestígios:</b>         {gm.PrestigeCount}\n" +
                $"<b>Próximo prestige em:</b> ${NumberFormatter.Format(gm.GetPrestigeRequirement())}";

            return new RoomPanel.Config
            {
                title            = "RELATÓRIOS DA EMPRESA",
                accent           = new Color(1f, 0.78f, 0.30f),
                statsBody        = body,
                statsActionLabel = "Coletar Bônus do Relatório",
                statsAction = () =>
                {
                    double bonus = Math.Max(50.0, GameManager.Instance.MoneyPerSecond * 10.0);
                    GameManager.Instance.AddMoney(bonus);
                    UIManager.Instance.ShowToast($"Relatório coletado! +${NumberFormatter.Format(bonus)}");
                }
            };
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Painel que lista todos os efeitos de eventos ativos (temporários e permanentes).
    public class ActiveEffectsPanel : MonoBehaviour
    {
        private readonly List<EffectRow> _rows = new();
        private GameObject _emptyLabel;

        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.85f);
        private static readonly Color NavyCard  = new(0.106f, 0.169f, 0.275f, 1f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GrayColor = new(0.35f, 0.40f, 0.50f, 1f);
        private static readonly Color GreenColor= new(0.30f, 0.90f, 0.45f, 1f);
        private static readonly Color BlueColor = new(0.35f, 0.65f, 1f, 1f);
        private static readonly Color RedColor  = new(1f, 0.35f, 0.35f, 1f);

        private const float PanelW  = 600f;
        private const float RowH    = 66f;
        private const float RowGap  = 8f;
        private const float TopPad  = 64f;
        private const float BotPad  = 70f;
        private const float SidePad = 18f;
        private const int   MaxRows = 8;

        private class EffectRow
        {
            public GameObject go;
            public Image bg;
            public Image barAccent;
            public TextMeshProUGUI nameText, valueText, timerText;
        }

        private void Awake() => BuildUI();

        private void BuildUI()
        {
            float panelH = TopPad + BotPad + MaxRows * RowH + (MaxRows - 1) * RowGap;
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(PanelW, panelH);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox(); bg.type = Image.Type.Sliced; bg.color = NavyDark;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // Título
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(PanelW - 2 * SidePad, 36f);
            trt.anchoredPosition = new Vector2(0f, -8f);
            var ttmp = titleGO.GetComponent<TextMeshProUGUI>();
            ttmp.text = "BÔNUS ATIVOS"; ttmp.fontSize = 24; ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = GoldColor; ttmp.alignment = TextAlignmentOptions.Center; ttmp.raycastTarget = false;
            if (font != null) ttmp.font = font;

            // Label vazio
            var emptyGO = new GameObject("Empty", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyGO.transform.SetParent(transform, false);
            var ert = emptyGO.GetComponent<RectTransform>();
            ert.anchorMin = new Vector2(0f, 0.3f); ert.anchorMax = new Vector2(1f, 0.7f);
            ert.offsetMin = new Vector2(SidePad, 0f); ert.offsetMax = new Vector2(-SidePad, 0f);
            var etmp = emptyGO.GetComponent<TextMeshProUGUI>();
            etmp.text = "Nenhum bônus ativo no momento.\nParticipe de eventos para ganhar bônus!";
            etmp.fontSize = 17; etmp.color = GrayColor; etmp.alignment = TextAlignmentOptions.Center;
            etmp.fontStyle = FontStyles.Bold; etmp.textWrappingMode = TextWrappingModes.Normal;
            etmp.raycastTarget = false;
            if (font != null) etmp.font = font;
            _emptyLabel = emptyGO;

            // Pré-cria MaxRows linhas (ocultas)
            for (int i = 0; i < MaxRows; i++)
            {
                float yTop = -TopPad - i * (RowH + RowGap);
                _rows.Add(BuildRow(i, font, yTop));
            }

            // Botão Fechar
            var closeGO = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var clrt = closeGO.GetComponent<RectTransform>();
            clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0f); clrt.pivot = new Vector2(0.5f, 0f);
            clrt.sizeDelta = new Vector2(PanelW - 2 * SidePad, 48f);
            clrt.anchoredPosition = new Vector2(0f, 12f);
            closeGO.GetComponent<Image>().sprite = UiSpriteFactory.RoundedBox();
            closeGO.GetComponent<Image>().type = Image.Type.Sliced;
            closeGO.GetComponent<Image>().color = GrayColor;
            closeGO.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            var cl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            cl.transform.SetParent(closeGO.transform, false);
            var cllrt = cl.GetComponent<RectTransform>();
            cllrt.anchorMin = Vector2.zero; cllrt.anchorMax = Vector2.one;
            cllrt.offsetMin = cllrt.offsetMax = Vector2.zero;
            var cltmp = cl.GetComponent<TextMeshProUGUI>();
            cltmp.text = "Fechar"; cltmp.fontSize = 18; cltmp.fontStyle = FontStyles.Bold;
            cltmp.color = Color.white; cltmp.alignment = TextAlignmentOptions.Center; cltmp.raycastTarget = false;
            if (font != null) cltmp.font = font;

            gameObject.SetActive(false);
        }

        private EffectRow BuildRow(int idx, TMP_FontAsset font, float yTop)
        {
            var row = new EffectRow();
            var go = new GameObject($"Row{idx}", typeof(RectTransform), typeof(Image));
            row.go = go;
            go.transform.SetParent(transform, false);
            var grt = go.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 1f); grt.pivot = new Vector2(0.5f, 1f);
            grt.sizeDelta = new Vector2(PanelW - 2 * SidePad, RowH);
            grt.anchoredPosition = new Vector2(0f, yTop);
            row.bg = go.GetComponent<Image>();
            row.bg.sprite = UiSpriteFactory.RoundedBox(); row.bg.type = Image.Type.Sliced; row.bg.color = NavyCard;
            row.bg.raycastTarget = false;

            // Barra colorida lateral esquerda
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(go.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.sizeDelta = new Vector2(4f, 0f);
            brt.anchoredPosition = new Vector2(8f, 0f);
            row.barAccent = bar.GetComponent<Image>();
            row.barAccent.color = GoldColor;
            row.barAccent.raycastTarget = false;

            // Nome do bônus
            row.nameText = MakeLabel(go.transform, "Name",
                new Vector2(0f, 0.5f), new Vector2(0.62f, 1f),
                new Vector2(24f, 0f), Vector2.zero,
                18f, Color.white, TextAlignmentOptions.BottomLeft, font);

            // Descrição / tipo
            row.valueText = MakeLabel(go.transform, "Val",
                new Vector2(0f, 0f), new Vector2(0.62f, 0.5f),
                new Vector2(24f, 0f), Vector2.zero,
                14f, GrayColor, TextAlignmentOptions.TopLeft, font);

            // Timer / "permanente"
            row.timerText = MakeLabel(go.transform, "Timer",
                new Vector2(0.62f, 0f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(-16f, 0f),
                16f, GoldColor, TextAlignmentOptions.Midline, font);

            go.SetActive(false);
            return row;

        }

        private TextMeshProUGUI MakeLabel(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            Vector2 oMin, Vector2 oMax, float size, Color color, TextAlignmentOptions align, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = size; tmp.color = color; tmp.fontStyle = FontStyles.Bold; tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap; tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;
            return tmp;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;
            RefreshTimers();
        }

        private void Refresh()
        {
            var effects = GameManager.Instance != null ? GameManager.Instance.GetActiveEffects() : null;
            int count = effects?.Count ?? 0;
            bool any = count > 0;

            // Ajusta a altura do painel ao número de bônus ativos (evita espaço vazio).
            // Sem bônus, reserva espaço para ~2 linhas para a mensagem caber.
            int rowsForHeight = Mathf.Clamp(any ? count : 2, 1, MaxRows);
            float panelH = TopPad + BotPad + rowsForHeight * RowH + (rowsForHeight - 1) * RowGap;
            var prt = (RectTransform)transform;
            prt.sizeDelta = new Vector2(PanelW, panelH);

            _emptyLabel.SetActive(!any);

            for (int i = 0; i < MaxRows; i++)
            {
                if (i < count)
                {
                    var e = effects[i];
                    _rows[i].go.SetActive(true);
                    _rows[i].nameText.text = GetEffectName(e);
                    _rows[i].valueText.text = GetEffectDesc(e);
                    _rows[i].timerText.text = GetTimerText(e);
                    _rows[i].timerText.color = e.isPermanent ? GoldColor : (e.value >= 0 ? GreenColor : RedColor);
                    _rows[i].barAccent.color = e.value >= 0 ? (e.isPermanent ? GoldColor : GreenColor) : RedColor;
                }
                else
                {
                    _rows[i].go.SetActive(false);
                }
            }
        }

        private void RefreshTimers()
        {
            var effects = GameManager.Instance != null ? GameManager.Instance.GetActiveEffects() : null;
            int count = effects?.Count ?? 0;
            for (int i = 0; i < Mathf.Min(count, MaxRows); i++)
            {
                _rows[i].timerText.text = GetTimerText(effects[i]);
            }
        }

        private static string GetEffectName(EventEffect e) => e.type switch
        {
            EffectType.ProductionModifier => "Bônus de Produção",
            EffectType.MultiplierModifier => "Multiplicador de Lucro",
            EffectType.MoneyBonus         => "Bônus de Dinheiro",
            _                             => "Efeito"
        };

        private static string GetEffectDesc(EventEffect e)
        {
            string sign = e.value >= 0 ? "+" : "";
            return $"{sign}{e.value * 100:F0}% em {GetEffectName(e).ToLower()}";
        }

        private static string GetTimerText(EventEffect e)
        {
            if (e.isPermanent) return "PERMANENTE";
            int secs = Mathf.CeilToInt(e.timeRemaining);
            if (secs >= 60)
            {
                int m = secs / 60, s = secs % 60;
                return $"{m}m {s:00}s";
            }
            return $"{secs}s";
        }
    }
}

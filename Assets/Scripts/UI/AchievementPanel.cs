using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class AchievementPanel : MonoBehaviour
    {
        private readonly Row[] _rows = new Row[AchievementManager.All.Length];

        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.95f);
        private static readonly Color NavyCard  = new(0.106f, 0.169f, 0.275f, 1f);
        private static readonly Color NavyReady = new(0.10f,  0.22f,  0.16f,  1f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GreenDone = new(0.35f, 0.95f, 0.55f, 1f);
        private static readonly Color TextSec   = new(0.78f, 0.84f, 0.92f, 1f);
        private static readonly Color GrayDim   = new(0.78f, 0.82f, 0.90f, 1f);

        private class Row
        {
            public Image bg;
            public TextMeshProUGUI nameText, descText, statusText;
        }

        private void Awake() => BuildUI();

        private void BuildUI()
        {
            int count = AchievementManager.All.Length;
            const float rowH    = 80f;   // mais alto → respiração
            const float rowGap  = 4f;
            const float topPad  = 60f;
            const float botPad  = 68f;
            const float sidePad = 16f;
            float panelH = topPad + botPad + count * rowH + (count - 1) * rowGap;
            // Limita a altura ao viewport (máximo 88% da tela) com scroll
            const float maxH = 860f;
            bool needsScroll = panelH > maxH;
            float actualH = needsScroll ? maxH : panelH;

            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, actualH);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox(); bg.type = Image.Type.Sliced; bg.color = new Color(0.086f, 0.137f, 0.220f, 0.85f);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // ── Título ────────────────────────────────────────────────────────
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.offsetMin = new Vector2(sidePad, 0f); trt.offsetMax = new Vector2(-sidePad, 0f);
            trt.sizeDelta = new Vector2(trt.sizeDelta.x, 40f);
            trt.anchoredPosition = new Vector2(0f, -12f);
            var ttmp = titleGO.GetComponent<TextMeshProUGUI>();
            ttmp.text = "CONQUISTAS"; ttmp.fontSize = 26; ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = GoldColor; ttmp.alignment = TextAlignmentOptions.Center; ttmp.raycastTarget = false;
            if (font != null) ttmp.font = font;

            // Separador dourado
            var sepGO = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sepGO.transform.SetParent(transform, false);
            var srt = sepGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(sidePad, 0f); srt.offsetMax = new Vector2(-sidePad, 0f);
            srt.sizeDelta = new Vector2(srt.sizeDelta.x, 2f);
            srt.anchoredPosition = new Vector2(0f, -54f);
            sepGO.GetComponent<Image>().color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.45f);
            sepGO.GetComponent<Image>().raycastTarget = false;

            // ── Botão Fechar ──────────────────────────────────────────────────
            var closeGO = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var clrt = closeGO.GetComponent<RectTransform>();
            clrt.anchorMin = new Vector2(0f, 0f); clrt.anchorMax = new Vector2(1f, 0f);
            clrt.pivot = new Vector2(0.5f, 0f);
            clrt.offsetMin = new Vector2(sidePad, 10f); clrt.offsetMax = new Vector2(-sidePad, 10f);
            clrt.sizeDelta = new Vector2(clrt.sizeDelta.x, 46f);
            var cImg = closeGO.GetComponent<Image>();
            cImg.sprite = UiSpriteFactory.RoundedBox(); cImg.type = Image.Type.Sliced;
            cImg.color = new Color(0.10f, 0.16f, 0.28f, 1f);
            closeGO.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            var cl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            cl.transform.SetParent(closeGO.transform, false);
            var cllrt = cl.GetComponent<RectTransform>();
            cllrt.anchorMin = Vector2.zero; cllrt.anchorMax = Vector2.one; cllrt.offsetMin = cllrt.offsetMax = Vector2.zero;
            var cltmp = cl.GetComponent<TextMeshProUGUI>();
            cltmp.text = "FECHAR"; cltmp.fontSize = 16; cltmp.fontStyle = FontStyles.Bold;
            cltmp.color = Color.white; cltmp.alignment = TextAlignmentOptions.Center; cltmp.raycastTarget = false;
            if (font != null) cltmp.font = font;

            // ── Área com scroll ───────────────────────────────────────────────
            float scrollTop    = 60f;
            float scrollBottom = 66f;

            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(transform, false);
            var scRT = scrollGO.GetComponent<RectTransform>();
            scRT.anchorMin = Vector2.zero; scRT.anchorMax = Vector2.one;
            scRT.offsetMin = new Vector2(sidePad, scrollBottom);
            scRT.offsetMax = new Vector2(-sidePad, -scrollTop);

            var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            vpGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            vpGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(vpGO.transform, false);
            var crt2 = contentGO.GetComponent<RectTransform>();
            crt2.anchorMin = new Vector2(0f, 1f); crt2.anchorMax = new Vector2(1f, 1f);
            crt2.pivot = new Vector2(0f, 1f);
            crt2.offsetMin = crt2.offsetMax = Vector2.zero;
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = rowGap; vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.viewport = vpRT; sr.content = crt2;
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 40f;

            for (int i = 0; i < count; i++)
                _rows[i] = BuildRow(contentGO.transform, AchievementManager.All[i], rowH, font);

            gameObject.SetActive(false);
        }

        private Row BuildRow(Transform parent, AchievementManager.Achievement a, float rowH, TMP_FontAsset font)
        {
            var row = new Row();
            var go = new GameObject(a.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = rowH;
            row.bg = go.GetComponent<Image>();
            row.bg.sprite = UiSpriteFactory.RoundedBox(); row.bg.type = Image.Type.Sliced;
            row.bg.color = NavyCard; row.bg.raycastTarget = false;

            // Barra de acento esquerda (2px)
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(go.transform, false);
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(0f, 1f);
            art.offsetMin = new Vector2(0f, 4f); art.offsetMax = new Vector2(4f, -4f);
            accent.GetComponent<Image>().color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.0f);
            accent.GetComponent<Image>().raycastTarget = false;
            // guardamos a referência no statusText hack: usamos nameText parent tag
            // Na verdade vamos guardar via closure — sem campo extra, apenas tinta no Open()

            // Coluna esquerda: nome + descrição
            row.nameText = MakeLabel(go.transform, "Name",
                new Vector2(0f, 0.5f), new Vector2(0.76f, 1f),
                new Vector2(16f, 0f), new Vector2(0f, -3f),
                21f, Color.white, TextAlignmentOptions.BottomLeft, font);
            row.descText = MakeLabel(go.transform, "Desc",
                new Vector2(0f, 0f), new Vector2(0.76f, 0.5f),
                new Vector2(16f, 3f), Vector2.zero,
                16f, TextSec, TextAlignmentOptions.TopLeft, font);

            // Coluna direita: status (gemas ou OK)
            row.statusText = MakeLabel(go.transform, "Status",
                new Vector2(0.74f, 0f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(-16f, 0f),
                22f, GoldColor, TextAlignmentOptions.Midline, font);

            row.nameText.text = a.name;
            row.descText.text = a.description;
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
            AchievementManager.MarkSeen();
            for (int i = 0; i < _rows.Length; i++)
            {
                var a = AchievementManager.All[i];
                bool unlocked = AchievementManager.IsUnlocked(a.id);
                _rows[i].bg.color = unlocked ? NavyReady : NavyCard;
                _rows[i].statusText.text  = unlocked ? "✓ OK" : $"+{a.gemReward} ◆";
                _rows[i].statusText.color = unlocked ? GreenDone : GrayDim;
                _rows[i].nameText.color   = unlocked ? Color.white : new Color(0.72f, 0.77f, 0.85f);
                _rows[i].descText.color   = unlocked ? new Color(0.70f, 0.90f, 0.74f) : TextSec;
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    /// <summary>
    /// Modal panel showing lifetime game statistics.
    /// All UI is built at runtime in C# — no prefabs required.
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        // ── Colors ────────────────────────────────────────────────────────────
        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.95f);
        private static readonly Color NavyRow0  = new(0.075f, 0.120f, 0.200f, 1f);
        private static readonly Color NavyRow1  = new(0.055f, 0.094f, 0.165f, 1f);
        private static readonly Color GoldColor = new(1f,     0.808f, 0.227f, 1f);
        private static readonly Color TextPrimary = new(0.933f, 0.953f, 0.980f, 1f);
        private static readonly Color TextSec     = new(0.624f, 0.698f, 0.788f, 1f);

        // ── Stat row value labels (repopulated on Open) ───────────────────────
        private TextMeshProUGUI _valTaps;
        private TextMeshProUGUI _valHires;
        private TextMeshProUGUI _valPrestiges;
        private TextMeshProUGUI _valKills;
        private TextMeshProUGUI _valBossKills;
        private TextMeshProUGUI _valCycle;
        private TextMeshProUGUI _valWave;
        private TextMeshProUGUI _valMoney;
        private TextMeshProUGUI _valGems;
        private TextMeshProUGUI _valAchievements;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            BuildUI();
            gameObject.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void Open()
        {
            gameObject.SetActive(true);
            RefreshStats();
        }

        public void Close() => gameObject.SetActive(false);

        public void RefreshStats()
        {
            var gm = GameManager.Instance;
            var cm = CombatManager.Instance;

            _valTaps.text         = gm != null ? gm.LifetimeTapCount.ToString("N0")      : "0";
            _valHires.text        = gm != null ? gm.LifetimeHireCount.ToString("N0")     : "0";
            _valPrestiges.text    = gm != null ? gm.LifetimePrestigeCount.ToString("N0") : "0";
            _valKills.text        = gm != null ? gm.LifetimeKillCount.ToString("N0")     : "0";
            _valBossKills.text    = gm != null ? gm.LifetimeBossKillCount.ToString("N0") : "0";
            _valCycle.text        = (cm?.Cycle ?? 1).ToString();
            _valWave.text         = (cm?.Wave  ?? 1).ToString();
            _valMoney.text        = gm != null ? "$" + NumberFormatter.Format(gm.TotalEarned) : "$0";
            _valGems.text         = gm != null ? gm.LifetimeGemsEarned.ToString("N0") : "0";

            int unlocked = AchievementManager.GetSaved().Count;
            int total    = AchievementManager.All.Length;
            _valAchievements.text = $"{unlocked} / {total}";
        }

        // ── UI Construction ───────────────────────────────────────────────────
        private void BuildUI()
        {
            // Root RectTransform — stretch anchors as specified
            var rt = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f);
            rt.anchorMax = new Vector2(0.95f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Panel background
            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox();
            bg.type   = Image.Type.Sliced;
            bg.color  = NavyDark;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            const float sidePad  = 16f;
            const float topPad   = 12f;
            const float titleH   = 36f;
            const float sepH     = 2f;
            const float closeH   = 44f;
            const float sepGap   = 6f;

            // ── Title ─────────────────────────────────────────────────────────
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot     = new Vector2(0.5f, 1f);
            trt.offsetMin = new Vector2(sidePad, 0f);
            trt.offsetMax = new Vector2(-sidePad, 0f);
            trt.sizeDelta = new Vector2(trt.sizeDelta.x, titleH);
            trt.anchoredPosition = new Vector2(0f, -topPad);

            var titleTxt = titleGO.GetComponent<TextMeshProUGUI>();
            titleTxt.font          = font;
            titleTxt.text          = "ESTATÍSTICAS";
            titleTxt.fontSize      = 20f;
            titleTxt.fontStyle     = FontStyles.Bold;
            titleTxt.color         = GoldColor;
            titleTxt.alignment     = TextAlignmentOptions.Center;

            // ── Gold separator ────────────────────────────────────────────────
            var sepGO = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            sepGO.transform.SetParent(transform, false);
            var srt = sepGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot     = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(sidePad, 0f);
            srt.offsetMax = new Vector2(-sidePad, 0f);
            srt.sizeDelta = new Vector2(srt.sizeDelta.x, sepH);
            srt.anchoredPosition = new Vector2(0f, -(topPad + titleH + sepGap));
            sepGO.GetComponent<Image>().color = GoldColor;

            float scrollTop = topPad + titleH + sepGap + sepH + sepGap;

            // ── Close button ─────────────────────────────────────────────────
            var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var crt = closeGO.GetComponent<RectTransform>();
            crt.anchorMin        = new Vector2(0f, 0f);
            crt.anchorMax        = new Vector2(1f, 0f);
            crt.pivot            = new Vector2(0.5f, 0f);
            crt.offsetMin        = new Vector2(sidePad,  12f);
            crt.offsetMax        = new Vector2(-sidePad, 12f);
            crt.sizeDelta        = new Vector2(crt.sizeDelta.x, closeH);
            crt.anchoredPosition = new Vector2(0f, 12f);

            var closeBtnImg = closeGO.GetComponent<Image>();
            closeBtnImg.sprite = UiSpriteFactory.RoundedBox();
            closeBtnImg.type   = Image.Type.Sliced;
            closeBtnImg.color  = GoldColor;

            var closeLblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLblGO.transform.SetParent(closeGO.transform, false);
            var clrt = closeLblGO.GetComponent<RectTransform>();
            clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
            clrt.offsetMin = Vector2.zero; clrt.offsetMax = Vector2.zero;
            var closeTxt = closeLblGO.GetComponent<TextMeshProUGUI>();
            closeTxt.font      = font;
            closeTxt.text      = "FECHAR";
            closeTxt.fontSize  = 16f;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.color     = new Color(0.055f, 0.094f, 0.165f, 1f);
            closeTxt.alignment = TextAlignmentOptions.Center;

            closeGO.GetComponent<Button>().onClick.AddListener(Close);

            float scrollBottom = closeH + 12f + 8f;

            // ── ScrollRect ────────────────────────────────────────────────────
            var scrollGO = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(transform, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(sidePad,  scrollBottom);
            scrollRT.offsetMax = new Vector2(-sidePad, -scrollTop);

            var scrollImg = scrollGO.GetComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0f); // transparent

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal      = false;
            scroll.vertical        = true;
            scroll.scrollSensitivity = 30f;

            // Viewport
            var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            vpGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            vpGO.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                                                       typeof(ContentSizeFitter));
            contentGO.transform.SetParent(vpGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing           = 0f;
            vlg.childAlignment    = TextAnchor.UpperCenter;
            vlg.childControlHeight  = false;
            vlg.childControlWidth   = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = true;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            var csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport  = vpRT;
            scroll.content   = contentRT;

            // ── Stat rows ──────────────────────────────────────────────────────
            (string label, string placeholder)[] rows =
            {
                ("Cliques totais",              "0"),
                ("Funcionários contratados",    "0"),
                ("Prestígios realizados",       "0"),
                ("Monstros derrotados",         "0"),
                ("Bosses derrotados",           "0"),
                ("Ciclo de combate atual",      "1"),
                ("Onda atual",                  "1"),
                ("Dinheiro ganho (total)",      "$0"),
                ("Gemas coletadas (total)",      "0"),
                ("Conquistas desbloqueadas",    "0 / 0"),
            };

            TextMeshProUGUI[] valLabels = new TextMeshProUGUI[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                var rowColor = (i % 2 == 0) ? NavyRow0 : NavyRow1;
                valLabels[i] = BuildRow(contentGO.transform, font, rows[i].label, rows[i].placeholder,
                                        rowColor, i);
            }

            _valTaps          = valLabels[0];
            _valHires         = valLabels[1];
            _valPrestiges     = valLabels[2];
            _valKills         = valLabels[3];
            _valBossKills     = valLabels[4];
            _valCycle         = valLabels[5];
            _valWave          = valLabels[6];
            _valMoney         = valLabels[7];
            _valGems          = valLabels[8];
            _valAchievements  = valLabels[9];
        }

        /// <summary>
        /// Creates a single stat row with left-aligned label and right-aligned value.
        /// Returns the value TextMeshProUGUI so it can be updated later.
        /// </summary>
        private TextMeshProUGUI BuildRow(Transform parent, TMP_FontAsset font,
                                         string label, string placeholder,
                                         Color bgColor, int index)
        {
            const float rowH    = 44f;
            const float padH    = 12f;

            var rowGO = new GameObject($"Row_{index}", typeof(RectTransform), typeof(Image),
                                                        typeof(LayoutElement));
            rowGO.transform.SetParent(parent, false);

            var rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.pivot = new Vector2(0f, 1f);

            var le = rowGO.GetComponent<LayoutElement>();
            le.preferredHeight = rowH;
            le.flexibleWidth   = 1f;

            var rowImg = rowGO.GetComponent<Image>();
            rowImg.color = bgColor;

            // HorizontalLayoutGroup inside the row
            var hlgGO = new GameObject("HLG", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            hlgGO.transform.SetParent(rowGO.transform, false);
            var hlgRT = hlgGO.GetComponent<RectTransform>();
            hlgRT.anchorMin = Vector2.zero;
            hlgRT.anchorMax = Vector2.one;
            hlgRT.offsetMin = new Vector2(padH, 0f);
            hlgRT.offsetMax = new Vector2(-padH, 0f);

            var hlg = hlgGO.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing              = 8f;
            hlg.childAlignment       = TextAnchor.MiddleLeft;
            hlg.childControlHeight   = true;
            hlg.childControlWidth    = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            // Label (left, flexible width)
            var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI),
                                               typeof(LayoutElement));
            lblGO.transform.SetParent(hlgGO.transform, false);
            var lblLE = lblGO.GetComponent<LayoutElement>();
            lblLE.flexibleWidth = 1f;

            var lblTxt = lblGO.GetComponent<TextMeshProUGUI>();
            lblTxt.font      = font;
            lblTxt.text      = label;
            lblTxt.fontSize  = 13f;
            lblTxt.color     = TextSec;
            lblTxt.alignment = TextAlignmentOptions.MidlineLeft;
            lblTxt.overflowMode = TextOverflowModes.Ellipsis;

            // Value (right, fixed preferred width)
            var valGO = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI),
                                               typeof(LayoutElement));
            valGO.transform.SetParent(hlgGO.transform, false);
            var valLE = valGO.GetComponent<LayoutElement>();
            valLE.preferredWidth = 160f;
            valLE.minWidth       = 80f;

            var valTxt = valGO.GetComponent<TextMeshProUGUI>();
            valTxt.font      = font;
            valTxt.text      = placeholder;
            valTxt.fontSize  = 13f;
            valTxt.fontStyle = FontStyles.Bold;
            valTxt.color     = TextPrimary;
            valTxt.alignment = TextAlignmentOptions.MidlineRight;

            return valTxt;
        }
    }
}

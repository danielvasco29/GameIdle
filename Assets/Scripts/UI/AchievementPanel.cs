using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Painel de conquistas — lista todas com estado bloqueado/desbloqueado.
    // Construído em runtime, dentro de um ScrollView simples.
    public class AchievementPanel : MonoBehaviour
    {
        private readonly Row[] _rows = new Row[AchievementManager.All.Length];

        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.98f);
        private static readonly Color NavyCard  = new(0.106f, 0.169f, 0.275f, 1f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GrayColor = new(0.35f, 0.40f, 0.50f, 1f);

        private class Row
        {
            public Image bg;
            public TextMeshProUGUI nameText, descText, statusText;
        }

        private void Awake() => BuildUI();

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 560f);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox(); bg.type = Image.Type.Sliced; bg.color = NavyDark;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // Título
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.92f); trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16f, 0f); trt.offsetMax = new Vector2(-16f, -8f);
            var ttmp = titleGO.GetComponent<TextMeshProUGUI>();
            ttmp.text = "CONQUISTAS"; ttmp.fontSize = 18; ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = GoldColor; ttmp.alignment = TextAlignmentOptions.Center; ttmp.raycastTarget = false;
            if (font != null) ttmp.font = font;

            // ScrollView
            var svGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            svGO.transform.SetParent(transform, false);
            var svrt = svGO.GetComponent<RectTransform>();
            svrt.anchorMin = new Vector2(0f, 0.10f); svrt.anchorMax = new Vector2(1f, 0.92f);
            svrt.offsetMin = new Vector2(12f, 4f); svrt.offsetMax = new Vector2(-12f, -4f);
            svGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            var scroll = svGO.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 20f;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(svGO.transform, false);
            var crt = contentGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f); crt.anchoredPosition = Vector2.zero;
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f; vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false; vlg.childControlHeight = true; vlg.childControlWidth = true;
            var csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = crt;

            for (int i = 0; i < AchievementManager.All.Length; i++)
                _rows[i] = BuildRow(contentGO.transform, AchievementManager.All[i], font);

            // Botão Fechar
            var closeGO = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var clrt = closeGO.GetComponent<RectTransform>();
            clrt.anchorMin = new Vector2(0f, 0f); clrt.anchorMax = new Vector2(1f, 0.10f);
            clrt.offsetMin = new Vector2(16f, 8f); clrt.offsetMax = new Vector2(-16f, -4f);
            closeGO.GetComponent<Image>().sprite = UiSpriteFactory.RoundedBox();
            closeGO.GetComponent<Image>().type = Image.Type.Sliced;
            closeGO.GetComponent<Image>().color = GrayColor;
            closeGO.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            var cl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            cl.transform.SetParent(closeGO.transform, false);
            var cllrt = cl.GetComponent<RectTransform>();
            cllrt.anchorMin = Vector2.zero; cllrt.anchorMax = Vector2.one; cllrt.offsetMin = cllrt.offsetMax = Vector2.zero;
            var cltmp = cl.GetComponent<TextMeshProUGUI>();
            cltmp.text = "Fechar"; cltmp.fontSize = 14; cltmp.fontStyle = FontStyles.Bold;
            cltmp.color = Color.white; cltmp.alignment = TextAlignmentOptions.Center; cltmp.raycastTarget = false;
            if (font != null) cltmp.font = font;

            gameObject.SetActive(false);
        }

        private Row BuildRow(Transform parent, AchievementManager.Achievement a, TMP_FontAsset font)
        {
            var row = new Row();
            var go = new GameObject(a.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = 56f;
            row.bg = go.GetComponent<Image>();
            row.bg.sprite = UiSpriteFactory.RoundedBox(); row.bg.type = Image.Type.Sliced; row.bg.color = NavyCard;
            row.bg.raycastTarget = false;

            row.nameText = MakeLabel(go.transform, "Name", new Vector2(0f, 0.5f), new Vector2(0.72f, 1f),
                new Vector2(10f, 0f), new Vector2(0f, -2f), 14f, Color.white, TextAlignmentOptions.BottomLeft, font);
            row.descText = MakeLabel(go.transform, "Desc", new Vector2(0f, 0f), new Vector2(0.72f, 0.5f),
                new Vector2(10f, 2f), Vector2.zero, 11f, new Color(0.62f, 0.70f, 0.79f), TextAlignmentOptions.TopLeft, font);
            row.statusText = MakeLabel(go.transform, "Status", new Vector2(0.72f, 0f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(-10f, 0f), 12f, GoldColor, TextAlignmentOptions.Midline, font);

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
            for (int i = 0; i < _rows.Length; i++)
            {
                var a = AchievementManager.All[i];
                bool unlocked = AchievementManager.IsUnlocked(a.id);
                _rows[i].bg.color = unlocked ? new Color(0.12f, 0.24f, 0.18f, 1f) : NavyCard;
                _rows[i].statusText.text = unlocked ? "OK" : $"+{a.gemReward}";
                _rows[i].statusText.color = unlocked ? new Color(0.4f, 1f, 0.6f) : GrayColor;
                _rows[i].nameText.color = unlocked ? Color.white : new Color(0.7f, 0.75f, 0.82f);
            }
        }
    }
}

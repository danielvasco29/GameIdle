using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Painel de conquistas — lista todas com estado bloqueado/desbloqueado.
    // Layout manual (sem ScrollRect) para evitar qualquer clipping de máscara.
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
            int count = AchievementManager.All.Length;
            const float rowH = 42f, rowGap = 5f, topPad = 50f, botPad = 56f, sidePad = 16f;
            float panelH = topPad + botPad + count * rowH + (count - 1) * rowGap;
            const float panelW = 560f;

            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(panelW, panelH);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox(); bg.type = Image.Type.Sliced; bg.color = NavyDark;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // Título — ancorado no topo (px do topo)
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(panelW - 2 * sidePad, 36f);
            trt.anchoredPosition = new Vector2(0f, -8f);
            var ttmp = titleGO.GetComponent<TextMeshProUGUI>();
            ttmp.text = "CONQUISTAS"; ttmp.fontSize = 18; ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = GoldColor; ttmp.alignment = TextAlignmentOptions.Center; ttmp.raycastTarget = false;
            if (font != null) ttmp.font = font;

            // Linhas — posicionadas manualmente a partir do topo
            for (int i = 0; i < count; i++)
            {
                float yTop = -topPad - i * (rowH + rowGap);
                _rows[i] = BuildRow(AchievementManager.All[i], panelW, sidePad, rowH, yTop, font);
            }

            // Botão Fechar — ancorado na base
            var closeGO = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var clrt = closeGO.GetComponent<RectTransform>();
            clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0f); clrt.pivot = new Vector2(0.5f, 0f);
            clrt.sizeDelta = new Vector2(panelW - 2 * sidePad, 40f);
            clrt.anchoredPosition = new Vector2(0f, 10f);
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

        private Row BuildRow(AchievementManager.Achievement a, float panelW, float sidePad, float rowH, float yTop, TMP_FontAsset font)
        {
            var row = new Row();
            var go = new GameObject(a.id, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var grt = go.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 1f); grt.pivot = new Vector2(0.5f, 1f);
            grt.sizeDelta = new Vector2(panelW - 2 * sidePad, rowH);
            grt.anchoredPosition = new Vector2(0f, yTop);
            row.bg = go.GetComponent<Image>();
            row.bg.sprite = UiSpriteFactory.RoundedBox(); row.bg.type = Image.Type.Sliced; row.bg.color = NavyCard;
            row.bg.raycastTarget = false;

            row.nameText = MakeLabel(go.transform, "Name", new Vector2(0f, 0.5f), new Vector2(0.74f, 1f),
                new Vector2(12f, 0f), new Vector2(0f, -1f), 13.5f, Color.white, TextAlignmentOptions.BottomLeft, font);
            row.descText = MakeLabel(go.transform, "Desc", new Vector2(0f, 0f), new Vector2(0.74f, 0.5f),
                new Vector2(12f, 1f), Vector2.zero, 10.5f, new Color(0.62f, 0.70f, 0.79f), TextAlignmentOptions.TopLeft, font);
            row.statusText = MakeLabel(go.transform, "Status", new Vector2(0.74f, 0f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(-12f, 0f), 12f, GoldColor, TextAlignmentOptions.Midline, font);

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
                _rows[i].bg.color = unlocked ? new Color(0.12f, 0.24f, 0.18f, 1f) : NavyCard;
                _rows[i].statusText.text = unlocked ? "OK" : $"+{a.gemReward}";
                _rows[i].statusText.color = unlocked ? new Color(0.4f, 1f, 0.6f) : GrayColor;
                _rows[i].nameText.color = unlocked ? Color.white : new Color(0.7f, 0.75f, 0.82f);
            }
        }
    }
}

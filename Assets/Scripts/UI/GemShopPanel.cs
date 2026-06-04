using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class GemShopPanel : MonoBehaviour
    {
        private static readonly Color Navy     = new(0.086f, 0.137f, 0.220f, 0.98f);
        private static readonly Color NavyCard = new(0.106f, 0.169f, 0.275f, 1f);
        private static readonly Color Gold     = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color Green    = new(0.247f, 0.749f, 0.353f, 1f);
        private static readonly Color GemCyan  = new(0.32f, 0.85f, 1f, 1f);
        private static readonly Color TextSec  = new(0.624f, 0.698f, 0.788f, 1f);

        private static Sprite Circle()  => UiSpriteFactory.Circle();
        private static Sprite Rounded() => UiSpriteFactory.RoundedBox();

        private TMP_FontAsset font;
        private TextMeshProUGUI gemBalanceText;

        private class Row
        {
            public int index;
            public TextMeshProUGUI effect;
            public TextMeshProUGUI level;
            public TextMeshProUGUI costLabel;
            public Button buyButton;
            public Image buyBg;
        }
        private readonly System.Collections.Generic.List<Row> rows = new();

        private void Awake()
        {
            font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                var any = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (any != null) font = any.font;
            }
            BuildUI();
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsUpdated += RefreshState;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsUpdated -= RefreshState;
        }

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.06f, 0.06f);
            rt.anchorMax = new Vector2(0.94f, 0.94f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var bg = gameObject.AddComponent<Image>();
            bg.sprite = Rounded(); bg.type = Image.Type.Sliced;
            bg.color = Navy;

            // Title
            var title = MakeText(transform, "Title", "LOJA DE GEMAS", 22, Gold, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(0f, -52f); trt.offsetMax = new Vector2(0f, -8f);

            // Gem balance
            gemBalanceText = MakeText(transform, "Balance", "", 16, GemCyan, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var grt = gemBalanceText.rectTransform;
            grt.anchorMin = new Vector2(0f, 1f); grt.anchorMax = new Vector2(1f, 1f);
            grt.offsetMin = new Vector2(0f, -74f); grt.offsetMax = new Vector2(0f, -52f);

            // Rows container
            float top = 84f;
            float rowH = 84f, gap = 8f;
            for (int i = 0; i < GemShop.Upgrades.Length; i++)
            {
                BuildRow(i, top + i * (rowH + gap), rowH);
            }

            // Close button
            var closeGO = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var crt = closeGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.3f, 0f); crt.anchorMax = new Vector2(0.7f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.sizeDelta = new Vector2(0f, 44f);
            crt.anchoredPosition = new Vector2(0f, 12f);
            var cImg = closeGO.GetComponent<Image>();
            cImg.sprite = Rounded(); cImg.type = Image.Type.Sliced;
            cImg.color = NavyCard;
            closeGO.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            var cl = MakeText(closeGO.transform, "L", "FECHAR", 15, Color.white, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var clr = cl.rectTransform; clr.anchorMin = Vector2.zero; clr.anchorMax = Vector2.one;
            clr.offsetMin = clr.offsetMax = Vector2.zero;
            cl.raycastTarget = false;
        }

        private void BuildRow(int index, float topOffset, float height)
        {
            var card = new GameObject($"Up{index}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(12f, -(topOffset + height));
            rt.offsetMax = new Vector2(-12f, -topOffset);
            var cImg = card.GetComponent<Image>();
            cImg.sprite = Rounded(); cImg.type = Image.Type.Sliced;
            cImg.color = NavyCard;

            var u = GemShop.Upgrades[index];

            // Name
            var name = MakeText(card.transform, "Name", u.name, 17, Color.white, FontStyles.Bold,
                TextAlignmentOptions.TopLeft);
            var nrt = name.rectTransform;
            nrt.anchorMin = new Vector2(0f, 0.55f); nrt.anchorMax = new Vector2(0.62f, 1f);
            nrt.offsetMin = new Vector2(14f, 0f); nrt.offsetMax = new Vector2(0f, -6f);

            // Description
            var desc = MakeText(card.transform, "Desc", u.description, 12, TextSec, FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            var drt = desc.rectTransform;
            drt.anchorMin = new Vector2(0f, 0.28f); drt.anchorMax = new Vector2(0.62f, 0.55f);
            drt.offsetMin = new Vector2(14f, 0f); drt.offsetMax = Vector2.zero;
            desc.textWrappingMode = TextWrappingModes.Normal;

            // Current effect + level
            var effect = MakeText(card.transform, "Effect", "", 12, Green, FontStyles.Bold,
                TextAlignmentOptions.BottomLeft);
            var ert = effect.rectTransform;
            ert.anchorMin = new Vector2(0f, 0f); ert.anchorMax = new Vector2(0.62f, 0.28f);
            ert.offsetMin = new Vector2(14f, 4f); ert.offsetMax = Vector2.zero;

            var level = MakeText(card.transform, "Lvl", "", 12, Gold, FontStyles.Bold,
                TextAlignmentOptions.TopRight);
            var lrt = level.rectTransform;
            lrt.anchorMin = new Vector2(0.62f, 0.55f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = new Vector2(-12f, -6f);

            // Buy button
            var buyGO = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            buyGO.transform.SetParent(card.transform, false);
            var brt = buyGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.62f, 0.1f); brt.anchorMax = new Vector2(1f, 0.5f);
            brt.offsetMin = new Vector2(6f, 0f); brt.offsetMax = new Vector2(-12f, 0f);
            var buyBg = buyGO.GetComponent<Image>();
            buyBg.sprite = Rounded(); buyBg.type = Image.Type.Sliced;
            buyBg.color = Green;
            var buyBtn = buyGO.GetComponent<Button>();
            int captured = index;
            buyBtn.onClick.AddListener(() => OnBuy(captured));

            // Small cyan diamond icon inside the button (drawn, not a glyph)
            var gemIcon = new GameObject("Gem", typeof(RectTransform), typeof(Image));
            gemIcon.transform.SetParent(buyGO.transform, false);
            var girt = gemIcon.GetComponent<RectTransform>();
            girt.anchorMin = girt.anchorMax = girt.pivot = new Vector2(0f, 0.5f);
            girt.anchoredPosition = new Vector2(12f, 0f);
            girt.sizeDelta = new Vector2(13f, 13f);
            girt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var giImg = gemIcon.GetComponent<Image>();
            giImg.sprite = UiSpriteFactory.Box(); giImg.type = Image.Type.Simple;
            giImg.color = GemCyan;
            giImg.raycastTarget = false;

            var costLabel = MakeText(buyGO.transform, "Cost", "", 14, Color.white, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var costRT = costLabel.rectTransform;
            costRT.anchorMin = Vector2.zero; costRT.anchorMax = Vector2.one;
            costRT.offsetMin = new Vector2(10f, 0f); costRT.offsetMax = Vector2.zero;
            costLabel.raycastTarget = false;

            rows.Add(new Row { index = index, effect = effect, level = level,
                               costLabel = costLabel, buyButton = buyBtn, buyBg = buyBg });
        }

        private void OnBuy(int index)
        {
            if (GemShop.Buy(index))
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayBuy();
                RefreshState();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast($"{GemShop.Upgrades[index].name} melhorado!", Green);
            }
            else
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowToast("Gemas insuficientes!", new Color(1f, 0.4f, 0.4f));
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshState();
        }

        private void RefreshState()
        {
            if (GameManager.Instance != null && gemBalanceText != null)
                gemBalanceText.text = $"Você tem {GameManager.Instance.Gems} gemas";

            foreach (var r in rows)
            {
                r.level.text  = GemShop.IsMaxed(r.index)
                    ? $"MÁX (Nv.{GemShop.GetLevel(r.index)})"
                    : $"Nv. {GemShop.GetLevel(r.index)}";
                r.effect.text = GemShop.GetEffectText(r.index);

                if (GemShop.IsMaxed(r.index))
                {
                    r.costLabel.text = "MÁX";
                    r.buyButton.interactable = false;
                    r.buyBg.color = new Color(0.3f, 0.34f, 0.4f, 1f);
                }
                else
                {
                    bool can = GemShop.CanBuy(r.index);
                    r.costLabel.text = $"{GemShop.GetCost(r.index)}";
                    r.buyButton.interactable = can;
                    r.buyBg.color = can ? Green : new Color(0.3f, 0.34f, 0.4f, 1f);
                }
            }
        }

        private TextMeshProUGUI MakeText(Transform parent, string goName, string text, int size,
                                         Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.fontStyle = style; tmp.alignment = align;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;
            return tmp;
        }
    }
}

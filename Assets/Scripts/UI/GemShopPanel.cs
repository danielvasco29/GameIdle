using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class GemShopPanel : MonoBehaviour
    {
        private static readonly Color Navy     = new(0.086f, 0.137f, 0.220f, 0.85f);
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
            // Modal centralizado de largura fixa (antes era stretch quase full-width,
            // o que deixava a loja "comprida" com botões esticados)
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(760f, 640f);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.AddComponent<Image>();
            bg.sprite = Rounded(); bg.type = Image.Type.Sliced;
            bg.color = Navy;

            // ── Header ──────────────────────────────────────────────────────────
            // Gem icon from Resources
            var gemTex = Resources.Load<Texture2D>("Icons/icon_gem");
            if (gemTex != null)
            {
                var gemTex2 = SpriteBackgroundRemover.ProcessDarkBg(gemTex);
                var gemSp = Sprite.Create(gemTex2, new Rect(0,0,gemTex2.width,gemTex2.height),
                    new Vector2(0.5f,0.5f), 100f, 0, SpriteMeshType.FullRect);
                var iconGO = new GameObject("GemIcon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(transform, false);
                var irt = iconGO.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1f);
                irt.pivot = new Vector2(0.5f, 1f);
                irt.anchoredPosition = new Vector2(-90f, -10f);
                irt.sizeDelta = new Vector2(36f, 36f);
                var iImg = iconGO.GetComponent<Image>();
                iImg.sprite = gemSp; iImg.preserveAspect = true; iImg.raycastTarget = false;
            }

            var title = MakeText(transform, "Title", "LOJA DE GEMAS", 28, Gold, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(0f, -52f); trt.offsetMax = new Vector2(0f, -8f);

            // Separator
            var sep = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(transform, false);
            var srt = sep.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(16f, -58f); srt.offsetMax = new Vector2(-16f, -54f);
            sep.GetComponent<Image>().color = new Color(1f, 0.808f, 0.227f, 0.35f);

            // Gem balance pill
            {
                var pillGO = new GameObject("BalancePill", typeof(RectTransform), typeof(Image));
                pillGO.transform.SetParent(transform, false);
                var prt = pillGO.GetComponent<RectTransform>();
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
                prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, -60f);
                prt.sizeDelta = new Vector2(260f, 38f);
                var pImg = pillGO.GetComponent<Image>();
                pImg.sprite = Rounded(); pImg.type = Image.Type.Sliced;
                pImg.color = new Color(0.10f, 0.16f, 0.28f, 1f);

                gemBalanceText = MakeText(pillGO.transform, "Balance", "", 18, GemCyan,
                    FontStyles.Bold, TextAlignmentOptions.Center);
                var gbrt = gemBalanceText.rectTransform;
                gbrt.anchorMin = Vector2.zero; gbrt.anchorMax = Vector2.one;
                gbrt.offsetMin = gbrt.offsetMax = Vector2.zero;
            }

            // ── Scrollable rows ──────────────────────────────────────────────────
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(transform, false);
            var scrRT = scrollGO.GetComponent<RectTransform>();
            scrRT.anchorMin = new Vector2(0f, 0.09f); scrRT.anchorMax = new Vector2(1f, 1f);
            scrRT.offsetMin = new Vector2(8f, 0f); scrRT.offsetMax = new Vector2(-8f, -100f);

            var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            vpGO.GetComponent<Mask>().showMaskGraphic = false;
            vpGO.GetComponent<Image>().color = Color.white;

            var contentGO = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(vpGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f); contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f; vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.viewport = vpRT; sr.content = contentRT;
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;

            for (int i = 0; i < GemShop.Upgrades.Length; i++)
                BuildRow(i, contentGO.transform);

            // ── Close button ────────────────────────────────────────────────────
            var closeGO = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var crt = closeGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.25f, 0f); crt.anchorMax = new Vector2(0.75f, 0.09f);
            crt.offsetMin = new Vector2(0f, 6f); crt.offsetMax = new Vector2(0f, -4f);
            var cImg = closeGO.GetComponent<Image>();
            cImg.sprite = Rounded(); cImg.type = Image.Type.Sliced;
            cImg.color = new Color(0.10f, 0.16f, 0.28f, 1f);
            closeGO.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            var cl = MakeText(closeGO.transform, "L", "FECHAR", 18, Color.white, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var clr = cl.rectTransform; clr.anchorMin = Vector2.zero; clr.anchorMax = Vector2.one;
            clr.offsetMin = clr.offsetMax = Vector2.zero; cl.raycastTarget = false;
        }

        private static readonly string[] UpgradeIcons =
        {
            "Icons/icon_sword",   // prod — production boost (closest match)
            "Icons/icon_sword",   // tap
            "Icons/icon_gem",     // prestige
            "Icons/icon_potion",  // start money
            "Icons/icon_frost",   // turbo cooldown
            "Icons/icon_shield",  // offline
            "Icons/icon_gem",     // gem bonus
            "Icons/icon_shield",  // combat_shield
            "Icons/icon_gem",     // kill_bonus
        };

        private void BuildRow(int index, Transform container)
        {
            var card = new GameObject($"Up{index}", typeof(RectTransform), typeof(Image),
                typeof(LayoutElement));
            card.transform.SetParent(container, false);
            card.GetComponent<LayoutElement>().preferredHeight = 104f;
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var cImg = card.GetComponent<Image>();
            cImg.sprite = Rounded(); cImg.type = Image.Type.Sliced;
            cImg.color = NavyCard;

            // Ícone à esquerda, direto sobre o card (fundo transparente, sem tile)
            float iconW = 0f;
            if (index < UpgradeIcons.Length)
            {
                var iconTex = Resources.Load<Texture2D>(UpgradeIcons[index]);
                if (iconTex != null)
                {
                    var proc = SpriteBackgroundRemover.ProcessDarkBg(iconTex);
                    var sp = Sprite.Create(proc, new Rect(0, 0, proc.width, proc.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

                    var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGO.transform.SetParent(card.transform, false);
                    var irt = iconGO.GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
                    irt.pivot = new Vector2(0f, 0.5f);
                    irt.anchoredPosition = new Vector2(14f, 0f);
                    irt.sizeDelta = new Vector2(60f, 60f);
                    var iImg = iconGO.GetComponent<Image>();
                    iImg.sprite = sp; iImg.preserveAspect = true; iImg.raycastTarget = false;
                    iconW = 84f;
                }
            }

            var u = GemShop.Upgrades[index];
            float nameX = iconW + 14f;

            // Name
            var name = MakeText(card.transform, "Name", u.name, 19, Color.white, FontStyles.Bold,
                TextAlignmentOptions.TopLeft);
            var nrt = name.rectTransform;
            nrt.anchorMin = new Vector2(0f, 0.55f); nrt.anchorMax = new Vector2(0.66f, 1f);
            nrt.offsetMin = new Vector2(nameX, 0f); nrt.offsetMax = new Vector2(0f, -6f);

            // Description
            var desc = MakeText(card.transform, "Desc", u.description, 13, TextSec, FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            var drt = desc.rectTransform;
            drt.anchorMin = new Vector2(0f, 0.28f); drt.anchorMax = new Vector2(0.66f, 0.55f);
            drt.offsetMin = new Vector2(nameX, 0f); drt.offsetMax = Vector2.zero;
            desc.textWrappingMode = TextWrappingModes.Normal;

            // Current effect + level
            var effect = MakeText(card.transform, "Effect", "", 13, Green, FontStyles.Bold,
                TextAlignmentOptions.BottomLeft);
            var ert = effect.rectTransform;
            ert.anchorMin = new Vector2(0f, 0f); ert.anchorMax = new Vector2(0.66f, 0.28f);
            ert.offsetMin = new Vector2(nameX, 4f); ert.offsetMax = Vector2.zero;

            var level = MakeText(card.transform, "Lvl", "", 15, Gold, FontStyles.Bold,
                TextAlignmentOptions.TopRight);
            var lrt = level.rectTransform;
            lrt.anchorMin = new Vector2(0.66f, 0.55f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = new Vector2(-14f, -6f);

            // Buy button (mais contido — antes esticava por toda a coluna direita)
            var buyGO = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            buyGO.transform.SetParent(card.transform, false);
            var brt = buyGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.66f, 0.12f); brt.anchorMax = new Vector2(1f, 0.5f);
            brt.offsetMin = new Vector2(8f, 0f); brt.offsetMax = new Vector2(-14f, 0f);
            var buyBg = buyGO.GetComponent<Image>();
            buyBg.sprite = Rounded(); buyBg.type = Image.Type.Sliced;
            buyBg.color = Green;
            var buyBtn = buyGO.GetComponent<Button>();
            int captured = index;
            buyBtn.onClick.AddListener(() => OnBuy(captured));
            var hold = buyGO.AddComponent<HoldButton>();
            hold.Init(() => OnBuy(captured));

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

            var costLabel = MakeText(buyGO.transform, "Cost", "", 17, Color.white, FontStyles.Bold,
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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Confirmacao de Prestigio — construido em runtime, sem prefabs,
    // no mesmo padrao navy/dourado dos outros paineis (ver OfflineProgressPanel).
    public class PrestigePanel : MonoBehaviour
    {
        private TextMeshProUGUI _infoText;
        private TextMeshProUGUI _multText;

        private static readonly Color NavyBg    = new(0.055f, 0.094f, 0.165f, 0.88f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GoldSoft  = new(1f, 0.808f, 0.227f, 0.10f);
        private static readonly Color GoldEdge  = new(1f, 0.808f, 0.227f, 0.28f);
        private static readonly Color GreenColor= new(0.247f, 0.749f, 0.353f, 1f);
        private static readonly Color TextSec   = new(0.624f, 0.698f, 0.788f, 1f);
        private static readonly Color GhostBtn  = new(0.624f, 0.698f, 0.788f, 0.14f);

        private void Awake() => BuildUI();

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 300f);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox();
            bg.type   = Image.Type.Sliced;
            bg.color  = NavyBg;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            TextMeshProUGUI MakeLabel(string name, string text, float size, Color color,
                Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, FontStyles style = FontStyles.Bold)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false);
                var lrt = go.GetComponent<RectTransform>();
                lrt.anchorMin = aMin; lrt.anchorMax = aMax;
                lrt.offsetMin = oMin; lrt.offsetMax = oMax;
                var tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.text = text; tmp.fontSize = size; tmp.color = color;
                tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                if (font != null) tmp.font = font;
                return tmp;
            }

            MakeLabel("Title", "⭐ Prestígio", 22f, GoldColor,
                new Vector2(0f, 0.82f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-14f, -10f));

            _infoText = MakeLabel("Info", "", 13.5f, TextSec,
                new Vector2(0f, 0.44f), new Vector2(1f, 0.80f), new Vector2(18f, 0f), new Vector2(-18f, 0f),
                FontStyles.Normal);
            _infoText.alignment = TextAlignmentOptions.Top;

            // Badge com o multiplicador — mesmo tratamento "pill" dourado usado no resto da UI
            var badgeGO = new GameObject("MultBadge", typeof(RectTransform), typeof(Image));
            badgeGO.transform.SetParent(transform, false);
            var badgeRT = badgeGO.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0f, 0.24f); badgeRT.anchorMax = new Vector2(1f, 0.42f);
            badgeRT.offsetMin = new Vector2(18f, 0f); badgeRT.offsetMax = new Vector2(-18f, 0f);
            var badgeImg = badgeGO.GetComponent<Image>();
            badgeImg.sprite = UiSpriteFactory.RoundedBox(); badgeImg.type = Image.Type.Sliced;
            badgeImg.color = GoldSoft;

            var badgeBorderGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
            badgeBorderGO.transform.SetParent(badgeGO.transform, false);
            var bbrt = badgeBorderGO.GetComponent<RectTransform>();
            bbrt.anchorMin = Vector2.zero; bbrt.anchorMax = Vector2.one; bbrt.offsetMin = bbrt.offsetMax = Vector2.zero;
            var bbImg = badgeBorderGO.GetComponent<Image>();
            bbImg.sprite = UiSpriteFactory.RoundedBox(); bbImg.type = Image.Type.Sliced;
            bbImg.color = GoldEdge; bbImg.raycastTarget = false;

            _multText = MakeLabel("MultText", "", 15f, GoldColor,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _multText.transform.SetParent(badgeGO.transform, false);

            MakeButton("Cancelar", new Vector2(0f, 0f), new Vector2(0.47f, 0.18f),
                new Vector2(18f, 10f), new Vector2(0f, 0f), GhostBtn, TextSec, OnCancel);
            MakeButton("Confirmar", new Vector2(0.53f, 0f), new Vector2(1f, 0.18f),
                new Vector2(0f, 10f), new Vector2(-18f, 0f), GreenColor, new Color(0.06f, 0.14f, 0.07f, 1f), OnConfirm);

            gameObject.SetActive(false);
        }

        private void MakeButton(string label, Vector2 aMin, Vector2 aMax,
            Vector2 oMin, Vector2 oMax, Color color, Color textColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var brt = go.GetComponent<RectTransform>();
            brt.anchorMin = aMin; brt.anchorMax = aMax;
            brt.offsetMin = oMin; brt.offsetMax = oMax;
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.RoundedBox(); img.type = Image.Type.Sliced; img.color = color;
            go.GetComponent<Button>().onClick.AddListener(onClick);

            var txtGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label.ToUpperInvariant(); tmp.fontSize = 13; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = textColor; tmp.raycastTarget = false;
            var f = TMP_Settings.defaultFontAsset; if (f != null) tmp.font = f;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            var gm = GameManager.Instance;
            bool canPrestige = gm.CanPrestige();
            double nextMultiplier = 1.0 + (gm.PrestigeCount + 1) * 0.5;

            if (_infoText != null)
                _infoText.text = canPrestige
                    ? "Reiniciar a startup zera seus funcionários, mas o multiplicador de ganhos permanece para sempre."
                    : $"Você ainda não pode fazer prestígio.\nNecessário: ${NumberFormatter.Format(gm.GetPrestigeRequirement())}\nAtual: ${NumberFormatter.Format(gm.Money)}";

            if (_multText != null)
                _multText.text = canPrestige
                    ? $"Multiplicador após prestígio: ×{nextMultiplier:F1}  •  +{gm.GetPrestigeGemReward()} gemas"
                    : $"Multiplicador atual: ×{gm.PrestigeMultiplier:F1}";
        }

        private void OnCancel() => gameObject.SetActive(false);

        private void OnConfirm()
        {
            GameManager.Instance.Prestige();
            gameObject.SetActive(false);
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Painel "Bem-vindo de volta" — construído em runtime, sem prefabs.
    public class OfflineProgressPanel : MonoBehaviour
    {
        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _earningsText;
        private double _pendingEarnings;

        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.85f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GreenColor= new(0.247f, 0.749f, 0.353f, 1f);
        private static readonly Color NavyBtn   = new(0.10f, 0.17f, 0.28f, 1f);

        private void Awake() => BuildUI();

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 240f);
            rt.anchoredPosition = Vector2.zero;

            // Fundo
            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox();
            bg.type   = Image.Type.Sliced;
            bg.color  = new Color(0.086f, 0.137f, 0.220f, 0.85f);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            TextMeshProUGUI MakeLabel(string name, string text, float size, Color color,
                Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false);
                var lrt = go.GetComponent<RectTransform>();
                lrt.anchorMin = aMin; lrt.anchorMax = aMax;
                lrt.offsetMin = oMin; lrt.offsetMax = oMax;
                var tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.text = text; tmp.fontSize = size; tmp.color = color;
                tmp.fontStyle = FontStyles.Bold; tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                if (font != null) tmp.font = font;
                return tmp;
            }

            MakeLabel("Title", "BEM-VINDO DE VOLTA!", 20f, GoldColor,
                new Vector2(0f, 0.76f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, -8f));

            _timeText = MakeLabel("Time", "", 15f, new Color(0.8f, 0.85f, 1f, 1f),
                new Vector2(0f, 0.52f), new Vector2(1f, 0.76f), new Vector2(12f, 0f), new Vector2(-12f, 0f));

            _earningsText = MakeLabel("Earnings", "", 24f, GreenColor,
                new Vector2(0f, 0.26f), new Vector2(1f, 0.52f), new Vector2(12f, 0f), new Vector2(-12f, 0f));

            // Navy com texto verde de acento (estilo navy do jogo)
            MakeButton("Coletar", new Vector2(0f, 0f), new Vector2(1f, 0.26f),
                new Vector2(16f, 8f), new Vector2(-16f, -8f), NavyBtn, GreenColor, OnCollect);

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
            tmp.text = label; tmp.fontSize = 16; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = textColor; tmp.raycastTarget = false;
            var f = TMP_Settings.defaultFontAsset; if (f != null) tmp.font = f;
        }

        public void Show(double earnings, long seconds)
        {
            _pendingEarnings = earnings;
            gameObject.SetActive(true);

            TimeSpan time = TimeSpan.FromSeconds(seconds);
            if (_timeText != null)
                _timeText.text = time.TotalHours >= 1
                    ? $"Você ficou fora por {(int)time.TotalHours}h {time.Minutes}m"
                    : $"Você ficou fora por {(int)time.TotalMinutes}m {time.Seconds}s";

            if (_earningsText != null)
                _earningsText.text = $"+${NumberFormatter.Format(earnings)}";
        }

        private void OnCollect()
        {
            GameManager.Instance.AddMoney(_pendingEarnings);
            UIManager.Instance.ShowToast($"Coletado: ${NumberFormatter.Format(_pendingEarnings)}", GreenColor);
            gameObject.SetActive(false);
        }
    }
}

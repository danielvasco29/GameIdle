using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class SettingsPanel : MonoBehaviour
    {
        private static readonly Color Navy     = new(0.086f, 0.137f, 0.220f, 0.85f);
        private static readonly Color NavyCard = new(0.106f, 0.169f, 0.275f, 1f);
        private static readonly Color Gold     = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color Red      = new(0.78f, 0.22f, 0.22f, 1f);
        private static readonly Color TextSec  = new(0.624f, 0.698f, 0.788f, 1f);

        private static Sprite Rounded() => UiSpriteFactory.RoundedBox();

        private TMP_FontAsset font;
        private GameObject confirmOverlay;

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

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.18f, 0.20f);
            rt.anchorMax = new Vector2(0.82f, 0.80f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var bg = gameObject.AddComponent<Image>();
            bg.sprite = Rounded(); bg.type = Image.Type.Sliced;
            bg.color = Navy;

            var title = MakeText(transform, "Title", "OPÇÕES", 22, Gold, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(0f, -54f); trt.offsetMax = new Vector2(0f, -10f);

            MakeToggle("MusicBtn", "MUSICA", new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.90f),
                () => !SoundManager.MusicMuted, SoundManager.ToggleMusic);
            MakeToggle("SfxBtn", "SOM", new Vector2(0.1f, 0.64f), new Vector2(0.9f, 0.76f),
                () => !SoundManager.Muted, SoundManager.ToggleMute);

            var resetBtn = MakeButton("ResetBtn", "RESETAR JOGO", NavyCard, new Vector2(0.1f, 0.46f), new Vector2(0.9f, 0.58f),
                ShowResetConfirm);
            resetBtn.GetComponentInChildren<TextMeshProUGUI>().color = Red; // acento vermelho (perigo)
            MakeButton("QuitBtn", "SAIR DO JOGO", NavyCard, new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.44f),
                QuitGame);
            MakeButton("CloseBtn", "FECHAR", NavyCard, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.18f),
                () => gameObject.SetActive(false));
        }

        // Botão liga/desliga que reflete o estado (ON verde / OFF cinza).
        private void MakeToggle(string goName, string label, Vector2 aMin, Vector2 aMax,
                                System.Func<bool> isOn, System.Action onToggle)
        {
            var green = new Color(0.247f, 0.749f, 0.353f, 1f);
            var gray  = new Color(0.5f, 0.55f, 0.65f, 1f);
            var navy  = new Color(0.10f, 0.16f, 0.28f, 1f);

            Button btn = null;
            TextMeshProUGUI lbl = null;
            void Apply()
            {
                bool on = isOn();
                // Navy sempre; estado sinalizado pela cor do texto (verde ON / cinza OFF)
                if (btn != null) btn.GetComponent<Image>().color = navy;
                if (lbl != null) { lbl.text = $"{label}: {(on ? "ON" : "OFF")}"; lbl.color = on ? green : gray; }
            }

            btn = MakeButton(goName, label, navy, aMin, aMax, () => { onToggle(); Apply(); });
            lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            Apply();
        }

        private void ShowResetConfirm()
        {
            if (confirmOverlay != null) Destroy(confirmOverlay);

            confirmOverlay = new GameObject("ConfirmOverlay", typeof(RectTransform), typeof(Image));
            confirmOverlay.transform.SetParent(transform, false);
            var ort = confirmOverlay.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;
            var oImg = confirmOverlay.GetComponent<Image>();
            oImg.sprite = Rounded(); oImg.type = Image.Type.Sliced;
            oImg.color = new Color(0.05f, 0.08f, 0.13f, 0.98f);

            var msg = MakeText(confirmOverlay.transform, "Msg",
                "Apagar TODO o progresso\ne começar um jogo novo?", 16, Color.white, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var mrt = msg.rectTransform;
            mrt.anchorMin = new Vector2(0.05f, 0.5f); mrt.anchorMax = new Vector2(0.95f, 0.9f);
            mrt.offsetMin = mrt.offsetMax = Vector2.zero;
            msg.textWrappingMode = TextWrappingModes.Normal;

            var yesBtn = MakeButton("Yes", "APAGAR TUDO", NavyCard, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.36f),
                () =>
                {
                    GameManager.Instance.HardReset();
                    if (confirmOverlay != null) Destroy(confirmOverlay);
                    gameObject.SetActive(false);
                }, confirmOverlay.transform);
            yesBtn.GetComponentInChildren<TextMeshProUGUI>().color = Red; // acento vermelho (perigo)

            MakeButton("No", "CANCELAR", NavyCard, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.16f),
                () => { if (confirmOverlay != null) Destroy(confirmOverlay); }, confirmOverlay.transform);
        }

        private void QuitGame()
        {
            SaveSystem.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Open() => gameObject.SetActive(true);

        private Button MakeButton(string goName, string label, Color color, Vector2 aMin, Vector2 aMax,
                                  UnityEngine.Events.UnityAction onClick, Transform parent = null)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent ?? transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = Rounded(); img.type = Image.Type.Sliced;
            img.color = color;
            go.GetComponent<Button>().onClick.AddListener(onClick);

            var t = MakeText(go.transform, "L", label, 16, Color.white, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var lrt = t.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            t.raycastTarget = false;
            return go.GetComponent<Button>();
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

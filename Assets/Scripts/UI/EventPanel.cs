using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class EventPanel : MonoBehaviour
    {
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descText;
        private RectTransform _choicesArea;
        private readonly List<GameObject> _spawnedChoices = new();
        private EventData _currentEvent;

        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.97f);
        private static readonly Color NavyCard  = new(0.10f,  0.16f,  0.27f,  1f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GreenColor= new(0.35f, 0.88f, 0.45f, 1f);
        private static readonly Color TextMain  = new(0.93f, 0.95f, 0.98f, 1f);

        private void Awake() => BuildUI();

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(660f, 520f);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox(); bg.type = Image.Type.Sliced; bg.color = NavyDark;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // Faixa dourada no topo (destaque do evento) — recuada das bordas
            // arredondadas para não vazar nos cantos do painel
            var stripe = new GameObject("TitleStripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(transform, false);
            var srt = stripe.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(10f, 0f); srt.offsetMax = new Vector2(-10f, 0f);
            srt.sizeDelta = new Vector2(0f, 84f);
            srt.anchoredPosition = new Vector2(0f, -10f);
            var sImg = stripe.GetComponent<Image>();
            sImg.sprite = UiSpriteFactory.RoundedBox(); sImg.type = Image.Type.Sliced;
            sImg.color = new Color(0.22f, 0.15f, 0.04f, 1f);
            sImg.raycastTarget = false;

            // Título do evento (fonte maior e centralizado na faixa)
            _titleText = MakeLabel("Title", 28f, GoldColor, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -94f), new Vector2(-24f, -10f), font);

            // Linha separadora
            var sepGO = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sepGO.transform.SetParent(transform, false);
            var sepRT = sepGO.GetComponent<RectTransform>();
            sepRT.anchorMin = new Vector2(0f, 1f); sepRT.anchorMax = new Vector2(1f, 1f);
            sepRT.pivot = new Vector2(0.5f, 1f);
            sepRT.offsetMin = new Vector2(24f, 0f); sepRT.offsetMax = new Vector2(-24f, 0f);
            sepRT.sizeDelta = new Vector2(0f, 2f);
            sepRT.anchoredPosition = new Vector2(0f, -100f);
            sepGO.GetComponent<Image>().color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.35f);
            sepGO.GetComponent<Image>().raycastTarget = false;

            // Descrição do evento (região abaixo do separador, texto centralizado).
            // Os offsets definem a região completa — não sobrescrever com sizeDelta,
            // senão a label vai parar na borda superior e fica cortada.
            _descText = MakeLabel("Desc", 20f, TextMain, FontStyles.Normal, TextAlignmentOptions.Top,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(30f, -236f), new Vector2(-30f, -114f), font);
            _descText.textWrappingMode = TextWrappingModes.Normal;
            _descText.lineSpacing = 10f;

            // Área das escolhas
            var areaGO = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup));
            areaGO.transform.SetParent(transform, false);
            _choicesArea = areaGO.GetComponent<RectTransform>();
            _choicesArea.anchorMin = new Vector2(0f, 0f); _choicesArea.anchorMax = new Vector2(1f, 1f);
            _choicesArea.offsetMin = new Vector2(22f, 20f); _choicesArea.offsetMax = new Vector2(-22f, -244f);
            var vlg = areaGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = true;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            gameObject.SetActive(false);
        }

        private TextMeshProUGUI MakeLabel(string name, float size, Color color, FontStyles style,
            TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = size; tmp.color = color; tmp.fontStyle = style; tmp.alignment = align;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;
            return tmp;
        }

        public void Show(EventData eventData)
        {
            if (eventData == null) return;
            _currentEvent = eventData;
            gameObject.SetActive(true);

            _titleText.text = eventData.title;
            _descText.text  = eventData.description;

            foreach (var go in _spawnedChoices) Destroy(go);
            _spawnedChoices.Clear();

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            for (int i = 0; i < eventData.choices.Length; i++)
            {
                int idx = i;
                _spawnedChoices.Add(BuildChoiceButton(eventData.choices[i], () => OnChoiceSelected(idx), font));
            }
        }

        private GameObject BuildChoiceButton(EventChoiceData choice, System.Action onClick, TMP_FontAsset font)
        {
            var go = new GameObject("Choice", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_choicesArea, false);
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.RoundedBox(); img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.22f, 0.36f, 1f);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());

            // Linha de acento esquerda
            var ac = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            ac.transform.SetParent(go.transform, false);
            var art = ac.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(0f, 1f);
            art.offsetMin = new Vector2(0f, 6f); art.offsetMax = new Vector2(4f, -6f);
            ac.GetComponent<Image>().color = GreenColor; ac.GetComponent<Image>().raycastTarget = false;

            // Texto principal da escolha
            var top = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            top.transform.SetParent(go.transform, false);
            var trt = top.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.48f); trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16f, 0f); trt.offsetMax = new Vector2(-12f, -4f);
            var ttmp = top.GetComponent<TextMeshProUGUI>();
            ttmp.text = choice.text; ttmp.fontSize = 21; ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = Color.white; ttmp.alignment = TextAlignmentOptions.BottomLeft; ttmp.raycastTarget = false;
            if (font != null) ttmp.font = font;

            // Efeito/recompensa da escolha
            var sub = new GameObject("Effect", typeof(RectTransform), typeof(TextMeshProUGUI));
            sub.transform.SetParent(go.transform, false);
            var srt = sub.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = new Vector2(1f, 0.48f);
            srt.offsetMin = new Vector2(16f, 4f); srt.offsetMax = new Vector2(-12f, 0f);
            var stmp = sub.GetComponent<TextMeshProUGUI>();
            stmp.text = choice.effectDescription; stmp.fontSize = 16; stmp.fontStyle = FontStyles.Bold;
            stmp.color = GreenColor; stmp.alignment = TextAlignmentOptions.TopLeft; stmp.raycastTarget = false;
            if (font != null) stmp.font = font;

            return go;
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            GameEventSystem.Instance.ResolveEvent(_currentEvent, choiceIndex);
            gameObject.SetActive(false);
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Painel de eventos aleatórios — construído em runtime (sem prefabs).
    public class EventPanel : MonoBehaviour
    {
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descText;
        private RectTransform _choicesArea;
        private readonly List<GameObject> _spawnedChoices = new();
        private EventData _currentEvent;

        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.85f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GreenColor= new(0.247f, 0.749f, 0.353f, 1f);
        private static readonly Color BtnColor  = new(0.16f, 0.30f, 0.46f, 1f);

        private void Awake() => BuildUI();

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(480f, 360f);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox(); bg.type = Image.Type.Sliced; bg.color = NavyDark;
            UiDepth.AddGlassTrim(bg);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            _titleText = MakeLabel("Title", 20f, GoldColor, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 0.82f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-16f, -10f), font);

            _descText = MakeLabel("Desc", 14f, new Color(0.85f, 0.89f, 0.96f), FontStyles.Normal, TextAlignmentOptions.Top,
                new Vector2(0f, 0.55f), new Vector2(1f, 0.82f), new Vector2(20f, 0f), new Vector2(-20f, 0f), font);
            _descText.textWrappingMode = TextWrappingModes.Normal;

            // Área das escolhas
            var areaGO = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup));
            areaGO.transform.SetParent(transform, false);
            _choicesArea = areaGO.GetComponent<RectTransform>();
            _choicesArea.anchorMin = new Vector2(0f, 0f); _choicesArea.anchorMax = new Vector2(1f, 0.55f);
            _choicesArea.offsetMin = new Vector2(16f, 12f); _choicesArea.offsetMax = new Vector2(-16f, -4f);
            var vlg = areaGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = true;
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
                var choice = eventData.choices[i];
                _spawnedChoices.Add(BuildChoiceButton(choice, () => OnChoiceSelected(idx), font));
            }
        }

        private GameObject BuildChoiceButton(EventChoiceData choice, System.Action onClick, TMP_FontAsset font)
        {
            var go = new GameObject("Choice", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_choicesArea, false);
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.RoundedBox(); img.type = Image.Type.Sliced; img.color = BtnColor;
            go.GetComponent<Button>().onClick.AddListener(() => onClick());

            var top = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            top.transform.SetParent(go.transform, false);
            var trt = top.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.45f); trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 0f); trt.offsetMax = new Vector2(-12f, -2f);
            var ttmp = top.GetComponent<TextMeshProUGUI>();
            ttmp.text = choice.text; ttmp.fontSize = 14; ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = Color.white; ttmp.alignment = TextAlignmentOptions.BottomLeft; ttmp.raycastTarget = false;
            if (font != null) ttmp.font = font;

            var sub = new GameObject("Effect", typeof(RectTransform), typeof(TextMeshProUGUI));
            sub.transform.SetParent(go.transform, false);
            var srt = sub.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = new Vector2(1f, 0.45f);
            srt.offsetMin = new Vector2(12f, 2f); srt.offsetMax = new Vector2(-12f, 0f);
            var stmp = sub.GetComponent<TextMeshProUGUI>();
            stmp.text = choice.effectDescription; stmp.fontSize = 11; stmp.fontStyle = FontStyles.Bold;
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

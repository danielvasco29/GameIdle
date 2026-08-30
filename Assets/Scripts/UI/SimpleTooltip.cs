using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameIdle
{
    // Tooltip simples para os ícones do topo: mostra um rótulo navy abaixo do
    // botão enquanto o ponteiro está sobre ele (desktop) ou durante o toque.
    // Criado preguiçosamente na primeira exibição.
    public class SimpleTooltip : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public string text = "";

        private GameObject _tip;

        public void OnPointerEnter(PointerEventData e) => Show(true);
        public void OnPointerExit(PointerEventData e)  => Show(false);
        public void OnPointerDown(PointerEventData e)  => Show(true);
        public void OnPointerUp(PointerEventData e)    => Show(false);
        private void OnDisable()                       { if (_tip != null) _tip.SetActive(false); }

        private void Show(bool on)
        {
            if (!on) { if (_tip != null) _tip.SetActive(false); return; }
            if (_tip == null) Build();
            _tip.SetActive(true);
        }

        private void Build()
        {
            _tip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            _tip.transform.SetParent(transform, false);
            var rt = _tip.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); // abaixo do botão
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -6f);
            rt.sizeDelta = new Vector2(Mathf.Max(64f, text.Length * 9f + 18f), 26f);
            var bg = _tip.GetComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox(); bg.type = Image.Type.Sliced;
            bg.color = new Color(0.05f, 0.09f, 0.16f, 0.97f);
            bg.raycastTarget = false;

            var lblGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(_tip.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = lblGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 13; tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.85f, 0.91f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var f = TMP_Settings.defaultFontAsset; if (f != null) tmp.font = f;
        }
    }
}

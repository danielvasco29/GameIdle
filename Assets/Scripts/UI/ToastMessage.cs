using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class ToastMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup canvasGroup;
        private Coroutine showCoroutine;
        private Image _edge;

        private static readonly Color NavyGlass = new(0.055f, 0.094f, 0.165f, 0.90f);

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            // Keep gameObject active — StartCoroutine requires activeInHierarchy.
            // If the parent is inactive SetActive(true) on the child still leaves
            // activeInHierarchy = false, so we never deactivate.

            BuildBackground();
        }

        // Fundo "glass" navy com borda fina — mesmo tratamento visual dos
        // outros paineis do jogo. A cena so define posicao/tamanho do Toast;
        // o visual e sempre garantido aqui, independente do que foi montado nela.
        private void BuildBackground()
        {
            var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox();
            bg.type   = Image.Type.Sliced;
            bg.color  = NavyGlass;

            var edgeGO = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            edgeGO.transform.SetParent(transform, false);
            edgeGO.transform.SetAsFirstSibling(); // atras do texto, na frente do fundo
            var ert = edgeGO.GetComponent<RectTransform>();
            ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one; ert.offsetMin = ert.offsetMax = Vector2.zero;
            _edge = edgeGO.GetComponent<Image>();
            _edge.sprite = UiSpriteFactory.RoundedBox();
            _edge.type   = Image.Type.Sliced;
            _edge.color  = new Color(1f, 1f, 1f, 0f);
            _edge.raycastTarget = false;
        }

        public void Show(string message, Color? tintColor = null)
        {
            // Lazy init in case Awake() was skipped because m_IsActive=0 in the scene file.
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            if (showCoroutine != null) StopCoroutine(showCoroutine);
            showCoroutine = StartCoroutine(ShowRoutine(message, tintColor ?? Color.white));
        }

        private IEnumerator ShowRoutine(string message, Color tintColor)
        {
            if (messageText == null) messageText = GetComponentInChildren<TextMeshProUGUI>();
            if (messageText == null) yield break;
            messageText.text  = message;
            messageText.color = tintColor;
            if (_edge != null) _edge.color = new Color(tintColor.r, tintColor.g, tintColor.b, 0.35f);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable   = true;

            yield return Fade(0f, 1f, fadeDuration);
            yield return new WaitForSeconds(displayDuration);
            yield return Fade(1f, 0f, fadeDuration);

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0;
            while (t < duration)
            {
                canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
                t += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}

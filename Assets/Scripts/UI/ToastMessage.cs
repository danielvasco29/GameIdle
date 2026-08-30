using System.Collections;
using TMPro;
using UnityEngine;

namespace GameIdle
{
    public class ToastMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup canvasGroup;
        private Coroutine showCoroutine;

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

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
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        public void Show(string message, Color? tintColor = null)
        {
            if (showCoroutine != null) StopCoroutine(showCoroutine);
            gameObject.SetActive(true);
            showCoroutine = StartCoroutine(ShowRoutine(message, tintColor ?? Color.white));
        }

        private IEnumerator ShowRoutine(string message, Color tintColor)
        {
            if (messageText == null) messageText = GetComponentInChildren<TextMeshProUGUI>();
            if (messageText == null) { gameObject.SetActive(false); yield break; }
            messageText.text  = message;
            messageText.color = tintColor;

            yield return Fade(0f, 1f, fadeDuration);
            yield return new WaitForSeconds(displayDuration);
            yield return Fade(1f, 0f, fadeDuration);

            gameObject.SetActive(false);
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

using System.Collections;
using TMPro;
using UnityEngine;

namespace GameIdle
{
    public class ToastMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float fadeDuration    = 0.3f;

        private CanvasGroup canvasGroup;
        private Coroutine   showCoroutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha          = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }

        public void Show(string message)
        {
            // Safety: ensure GO is active regardless of scene state
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);
            if (showCoroutine != null) StopCoroutine(showCoroutine);
            showCoroutine = StartCoroutine(ShowRoutine(message));
        }

        private IEnumerator ShowRoutine(string message)
        {
            messageText.text = message;
            yield return Fade(0f, 1f, fadeDuration);
            yield return new WaitForSeconds(displayDuration);
            yield return Fade(1f, 0f, fadeDuration);
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

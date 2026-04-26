using System;
using System.Collections;
using UnityEngine;

namespace GameIdle
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelAnimator : MonoBehaviour
    {
        private CanvasGroup cg;
        private Coroutine   current;

        private const float OpenDur  = 0.18f;
        private const float CloseDur = 0.13f;

        private void Awake()
        {
            cg = GetComponent<CanvasGroup>();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Run(AnimateIn());
        }

        public void Close(Action onDone = null)
        {
            Run(AnimateOut(() =>
            {
                gameObject.SetActive(false);
                onDone?.Invoke();
            }));
        }

        private void Run(IEnumerator r)
        {
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(r);
        }

        private IEnumerator AnimateIn()
        {
            if (cg == null) cg = GetComponent<CanvasGroup>();
            transform.localScale = Vector3.one * 0.82f;
            cg.alpha = 0;

            for (float t = 0; t < OpenDur; t += Time.unscaledDeltaTime)
            {
                float p = 1f - Mathf.Pow(1f - t / OpenDur, 3f); // ease-out cubic
                transform.localScale = Vector3.Lerp(Vector3.one * 0.82f, Vector3.one, p);
                cg.alpha = p;
                yield return null;
            }

            transform.localScale = Vector3.one;
            cg.alpha = 1;
        }

        private IEnumerator AnimateOut(Action onDone)
        {
            if (cg == null) cg = GetComponent<CanvasGroup>();
            Vector3 fromScale = transform.localScale;

            for (float t = 0; t < CloseDur; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.Pow(t / CloseDur, 2f); // ease-in quad
                transform.localScale = Vector3.Lerp(fromScale, Vector3.one * 0.82f, p);
                cg.alpha = 1f - p;
                yield return null;
            }

            transform.localScale = Vector3.one * 0.82f;
            cg.alpha = 0;
            onDone?.Invoke();
        }
    }
}

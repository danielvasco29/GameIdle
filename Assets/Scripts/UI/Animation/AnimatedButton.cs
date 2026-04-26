using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameIdle
{
    [RequireComponent(typeof(Button))]
    public class AnimatedButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private Button button;
        private Coroutine current;
        private Vector3 baseScale;

        private const float HoverScale  = 1.06f;
        private const float ClickSquish = 0.90f;
        private const float Dur         = 0.07f;

        private void Awake()
        {
            button    = GetComponent<Button>();
            baseScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!button.interactable) return;
            Run(ScaleTo(baseScale * HoverScale, Dur));
        }

        public void OnPointerExit(PointerEventData _)
        {
            Run(ScaleTo(baseScale, Dur));
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (!button.interactable) return;
            Run(Bounce());
        }

        private void Run(IEnumerator r)
        {
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(r);
        }

        private IEnumerator ScaleTo(Vector3 target, float duration)
        {
            Vector3 from = transform.localScale;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.Lerp(from, target, t / duration);
                yield return null;
            }
            transform.localScale = target;
        }

        private IEnumerator Bounce()
        {
            yield return ScaleTo(baseScale * ClickSquish, Dur);
            yield return ScaleTo(baseScale, Dur);
        }

        private void OnDisable()
        {
            if (current != null) StopCoroutine(current);
            transform.localScale = baseScale;
        }
    }
}

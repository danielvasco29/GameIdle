using UnityEngine;
using UnityEngine.EventSystems;

namespace GameIdle
{
    // Feedback tátil leve para botões: cresce sutil no hover e afunda no clique.
    // Anexar a qualquer GameObject com Button (usa o RectTransform do próprio botão).
    public class ButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.06f;
        public float pressScale = 0.93f;

        private RectTransform _rt;
        private Vector3 _base = Vector3.one;
        private float _target = 1f;
        private bool _hover, _press;

        private void Awake() { _rt = GetComponent<RectTransform>(); if (_rt != null) _base = _rt.localScale; }
        private void OnEnable() { _target = 1f; _hover = _press = false; if (_rt != null) _rt.localScale = _base; }

        public void OnPointerEnter(PointerEventData e) { _hover = true;  Recompute(); }
        public void OnPointerExit(PointerEventData e)  { _hover = false; _press = false; Recompute(); }
        public void OnPointerDown(PointerEventData e)  { _press = true;  Recompute(); }
        public void OnPointerUp(PointerEventData e)    { _press = false; Recompute(); }

        private void Recompute() => _target = _press ? pressScale : (_hover ? hoverScale : 1f);

        private void Update()
        {
            if (_rt == null) return;
            _rt.localScale = Vector3.Lerp(_rt.localScale, _base * _target,
                Mathf.Min(1f, Time.unscaledDeltaTime * 14f));
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameIdle
{
    // Adicione este componente ao mesmo GameObject que tem um Button.
    // Chame Init() para registrar o callback que será disparado a cada "tap".
    // Enquanto o dedo/mouse estiver pressionado, OnHeld() é chamado em
    // intervalos decrescentes (começa em initialInterval, reduz até minInterval).
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float initialInterval = 0.18f; // intervalo inicial (s)
        [SerializeField] private float minInterval     = 0.04f; // intervalo mínimo (s)
        [SerializeField] private float rampTime        = 1.8f;  // tempo até atingir velocidade máxima

        private Action _onTap;
        private bool   _held;
        private float  _timer;
        private float  _elapsed;
        private float  _currentInterval;

        public void Init(Action onTap)
        {
            _onTap = onTap;
        }

        public void OnPointerDown(PointerEventData _)
        {
            _held            = true;
            _elapsed         = 0f;
            _currentInterval = initialInterval;
            _timer           = 0f;
            // dispara imediatamente — o click normal do Button também dispara,
            // então não chamamos _onTap aqui para não duplicar o primeiro tap.
        }

        public void OnPointerUp(PointerEventData _)   => _held = false;
        public void OnPointerExit(PointerEventData _) => _held = false;

        private void Update()
        {
            if (!_held || _onTap == null) return;

            _elapsed += Time.deltaTime;
            _timer   += Time.deltaTime;

            // Ramp: interpola de initialInterval até minInterval ao longo de rampTime
            float t = Mathf.Clamp01(_elapsed / rampTime);
            _currentInterval = Mathf.Lerp(initialInterval, minInterval, t);

            if (_timer >= _currentInterval)
            {
                _timer -= _currentInterval;
                _onTap.Invoke();
            }
        }
    }
}

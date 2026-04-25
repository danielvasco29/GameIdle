using System;
using System.Collections;
using UnityEngine;

namespace GameIdle
{
    [DefaultExecutionOrder(-80)]
    public class GameEventSystem : MonoBehaviour
    {
        public static GameEventSystem Instance { get; private set; }

        [SerializeField] private float minTriggerInterval = 30f;
        [SerializeField] private float maxTriggerInterval = 120f;

        private EventData[] allEvents;
        private Coroutine eventCoroutine;

        public event Action<EventData> OnEventTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            allEvents = Resources.LoadAll<EventData>("Events");
        }

        public void StartEventCycle()
        {
            if (eventCoroutine != null) StopCoroutine(eventCoroutine);
            eventCoroutine = StartCoroutine(EventCycleRoutine());
        }

        private IEnumerator EventCycleRoutine()
        {
            while (true)
            {
                float wait = UnityEngine.Random.Range(minTriggerInterval, maxTriggerInterval);
                yield return new WaitForSeconds(wait);

                if (allEvents == null || allEvents.Length == 0) continue;

                EventData triggered = allEvents[UnityEngine.Random.Range(0, allEvents.Length)];
                OnEventTriggered?.Invoke(triggered);
            }
        }

        public void ResolveEvent(EventData eventData, int choiceIndex)
        {
            if (eventData == null) return;
            if (choiceIndex < 0 || choiceIndex >= eventData.choices.Length) return;

            var choice = eventData.choices[choiceIndex];
            if (choice.effectType == EffectType.None) return;

            var effect = new EventEffect
            {
                eventId = eventData.eventId + "_" + choiceIndex,
                type = choice.effectType,
                value = choice.effectValue,
                duration = choice.duration,
                timeRemaining = choice.duration,
                isPermanent = choice.duration <= 0
            };

            GameManager.Instance.ApplyEffect(effect);
            UIManager.Instance.ShowToast(choice.effectDescription);
        }

        public void ResolveBestChoice(EventData eventData)
        {
            if (eventData == null || eventData.choices.Length == 0) return;

            int bestIndex = 0;
            float bestValue = float.MinValue;
            for (int i = 0; i < eventData.choices.Length; i++)
            {
                if (eventData.choices[i].effectValue > bestValue)
                {
                    bestValue = eventData.choices[i].effectValue;
                    bestIndex = i;
                }
            }
            ResolveEvent(eventData, bestIndex);
            UIManager.Instance.ShowToast("Evento resolvido automaticamente!");
        }
    }
}

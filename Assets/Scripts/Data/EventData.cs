using System;
using UnityEngine;

namespace GameIdle
{
    public enum EffectType
    {
        ProductionModifier,   // Adds/subtracts % to production
        MultiplierModifier,   // Adds/subtracts % to multiplier
        MoneyBonus,           // Instant money bonus
        None
    }

    [Serializable]
    public class EventChoiceData
    {
        public string text;
        public EffectType effectType;
        [Tooltip("Percentage as decimal: 0.1 = +10%, -0.2 = -20%")]
        public float effectValue;
        [Tooltip("Duration in seconds. 0 = permanent.")]
        public float duration;
        public string effectDescription;
    }

    [CreateAssetMenu(fileName = "NewEvent", menuName = "GameIdle/Event Data")]
    public class EventData : ScriptableObject
    {
        [Header("Identidade")]
        public string eventId;
        public string title;
        [TextArea] public string description;
        public Sprite illustration;

        [Header("Timing")]
        public float minTriggerInterval = 30f;
        public float maxTriggerInterval = 120f;

        [Header("Escolhas")]
        public EventChoiceData[] choices;
    }
}

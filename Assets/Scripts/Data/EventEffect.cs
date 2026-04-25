using System;

namespace GameIdle
{
    [Serializable]
    public class EventEffect
    {
        public string eventId;
        public EffectType type;
        public float value;            // e.g. 0.1 = +10%, -0.2 = -20%
        public float duration;
        public float timeRemaining;
        public bool isPermanent;       // duration == 0

        public bool IsExpired => !isPermanent && timeRemaining <= 0;

        public void Tick(float deltaTime)
        {
            if (!isPermanent) timeRemaining -= deltaTime;
        }
    }
}

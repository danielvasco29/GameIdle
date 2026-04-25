using System;
using System.Collections.Generic;

namespace GameIdle
{
    [Serializable]
    public class CharacterSaveData
    {
        public string characterId;
        public int level;
    }

    [Serializable]
    public class SaveData
    {
        public double money;
        public double totalEarned;
        public double moneyPerSecond;
        public int prestigeCount;
        public double prestigeMultiplier;
        public long lastLoginTimestamp;
        public List<CharacterSaveData> characters = new();
    }
}

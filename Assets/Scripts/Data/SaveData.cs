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
        public int gems;
        public List<int> gemUpgrades = new();
        public long lastLoginTimestamp;
        public List<CharacterSaveData> characters = new();
    }

    [Serializable]
    public class PrestigeRecord
    {
        public string playerName;
        public int prestigeCount;
        public string date;
    }

    [Serializable]
    public class RankingData
    {
        public List<PrestigeRecord> records = new();
    }
}

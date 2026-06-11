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

        // Conquistas
        public List<string> unlockedAchievements = new();

        // Missões diárias
        public string lastMissionDate = "";
        public List<int>  missionProgress = new();
        public List<bool> missionClaimed  = new();

        // Contadores para conquistas/missões
        public int lifetimeTapCount;
        public int lifetimePrestigeCount;
        public int lifetimeHireCount;
        public int lifetimeKillCount;
        public int lifetimeBossKillCount;
        public int combatWave;
        public int combatCycle;
    }

    [Serializable]
    public class PrestigeRecord
    {
        public string playerName;
        public int prestigeCount;
        public int killCount;
        public int bossKillCount;
        public string date;
    }

    [Serializable]
    public class RankingData
    {
        public List<PrestigeRecord> records = new();
    }
}

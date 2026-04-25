using System;

namespace GameIdle
{
    [Serializable]
    public class CharacterInstance
    {
        public CharacterData data;
        public int level;
        public bool isUnlocked;

        // Cost formula: baseCost * 1.15^level
        public double GetCurrentCost() =>
            data.baseCost * Math.Pow(1.15, level);

        // Production formula: baseProduction * level
        public double GetCurrentProduction() =>
            data.type == CharacterType.Multiplier ? 0.0 : data.baseProduction * level;

        // Multiplier formula: multiplier^level
        public double GetCurrentMultiplier() =>
            data.type == CharacterType.Multiplier ? Math.Pow(data.multiplier, level) : 1.0;

        public bool CanAfford(double money) => money >= GetCurrentCost();
    }
}

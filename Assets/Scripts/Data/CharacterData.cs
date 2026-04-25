using UnityEngine;

namespace GameIdle
{
    public enum CharacterType
    {
        Production,   // Generates money per second directly
        Multiplier,   // Multiplies total income (stacks multiplicatively)
        Automation    // Same as Production but represents automated workers
    }

    [CreateAssetMenu(fileName = "NewCharacter", menuName = "GameIdle/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identidade")]
        public string characterId;
        public string characterName;
        [TextArea] public string description;
        public Sprite icon;
        public Color tintColor = Color.white;

        [Header("Tipo")]
        public CharacterType type;

        [Header("Estatísticas")]
        public double baseProduction = 1.0;   // For Production/Automation: money/s per level
        public double multiplier = 1.0;        // For Multiplier: multiplier per level (e.g. 1.2 = +20% per level)
        public double baseCost = 10.0;         // Cost to hire first unit
    }
}

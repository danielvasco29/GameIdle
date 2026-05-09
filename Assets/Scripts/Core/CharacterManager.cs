using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameIdle
{
    [DefaultExecutionOrder(-90)]
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        private CharacterData[] characterDefinitions;
        private CharacterInstance[] characters;

        public event Action OnCharactersUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize()
        {
            characterDefinitions = Resources.LoadAll<CharacterData>("Characters");
            Array.Sort(characterDefinitions, (a, b) => a.baseCost.CompareTo(b.baseCost));

            characters = new CharacterInstance[characterDefinitions.Length];
            for (int i = 0; i < characterDefinitions.Length; i++)
            {
                characters[i] = new CharacterInstance
                {
                    data = characterDefinitions[i],
                    level = 0,
                    isUnlocked = i == 0
                };
            }
        }

        public CharacterInstance[] GetAllCharacters() => characters;

        public double GetTotalMPS()
        {
            double total = 0;
            foreach (var c in characters)
                if (c.level > 0) total += c.GetCurrentProduction();
            return total;
        }

        public double GetTotalMultiplier()
        {
            double multiplier = 1.0;
            foreach (var c in characters)
                if (c.level > 0 && c.data.type == CharacterType.Multiplier)
                    multiplier *= c.GetCurrentMultiplier();
            return multiplier;
        }

        public bool TryUpgrade(int index)
        {
            if (index < 0 || index >= characters.Length) return false;

            var character = characters[index];
            double cost = character.GetCurrentCost();

            if (!GameManager.Instance.SpendMoney(cost)) return false;

            character.level++;
            if (index + 1 < characters.Length)
                characters[index + 1].isUnlocked = true;

            GameManager.Instance.RecalculateStats();
            OnCharactersUpdated?.Invoke();
            return true;
        }

        public (CharacterInstance next, CharacterInstance via) GetNextUnlock()
        {
            for (int i = 1; i < characters.Length; i++)
            {
                if (!characters[i].isUnlocked)
                    return (characters[i], characters[i - 1]);
            }
            return (null, null);
        }

        public void ResetAll()
        {
            foreach (var c in characters) c.level = 0;
            for (int i = 0; i < characters.Length; i++)
                characters[i].isUnlocked = i == 0;
            OnCharactersUpdated?.Invoke();
        }

        public List<CharacterSaveData> GetSaveData()
        {
            var list = new List<CharacterSaveData>();
            foreach (var c in characters)
                list.Add(new CharacterSaveData { characterId = c.data.characterId, level = c.level });
            return list;
        }

        public void ApplySaveData(List<CharacterSaveData> saveData)
        {
            if (saveData == null || characters == null) return;
            foreach (var saved in saveData)
            {
                for (int i = 0; i < characters.Length; i++)
                {
                    if (characters[i].data.characterId == saved.characterId)
                    {
                        characters[i].level = saved.level;
                        if (saved.level > 0 && i + 1 < characters.Length)
                            characters[i + 1].isUnlocked = true;
                        break;
                    }
                }
            }
        }
    }
}

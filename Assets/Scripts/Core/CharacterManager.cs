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

        // Bulk-buy mode shared across all character cards: 1, 10, or -1 (Max).
        public static int BuyAmount = 1;

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

        // How many levels would actually be bought for `index` given the current
        // BuyAmount mode and money (Max resolves to what the player can afford).
        public int GetPurchaseCount(int index)
        {
            if (index < 0 || index >= characters.Length) return 0;
            if (BuyAmount == -1)
                return Math.Max(1, characters[index].GetMaxAffordable(GameManager.Instance.Money));
            return BuyAmount;
        }

        // Total cost for the resolved purchase count above.
        public double GetPurchaseCost(int index)
        {
            if (index < 0 || index >= characters.Length) return 0;
            return characters[index].GetCostForLevels(GetPurchaseCount(index));
        }

        public bool TryUpgrade(int index)
        {
            if (index < 0 || index >= characters.Length) return false;

            var character = characters[index];
            int count = BuyAmount == -1
                ? character.GetMaxAffordable(GameManager.Instance.Money)
                : BuyAmount;
            if (count < 1) return false;

            double cost = character.GetCostForLevels(count);
            if (!GameManager.Instance.SpendMoney(cost)) return false;

            character.level += count;
            if (index + 1 < characters.Length)
                characters[index + 1].isUnlocked = true;

            GameManager.Instance.RecalculateStats();
            OnCharactersUpdated?.Invoke();
            SaveSystem.Save();
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

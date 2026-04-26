using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class EventChoiceButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI choiceText;
        [SerializeField] private TextMeshProUGUI effectText;
        [SerializeField] private Button button;

        private static readonly Color ColorPositive = new Color(0.13f, 0.55f, 0.13f, 1f);
        private static readonly Color ColorNegative  = new Color(0.72f, 0.18f, 0.07f, 1f);
        private static readonly Color ColorMoney     = new Color(0.75f, 0.52f, 0.02f, 1f);
        private static readonly Color ColorNeutral   = new Color(0.16f, 0.38f, 0.60f, 1f);

        public void Setup(EventChoiceData choice, Action onClicked)
        {
            choiceText.text = choice.text;
            effectText.text = choice.effectDescription;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke());

            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = GetEffectColor(choice.effectType, choice.effectValue);
        }

        private static Color GetEffectColor(EffectType type, float value)
        {
            return type switch
            {
                EffectType.MoneyBonus => ColorMoney,
                EffectType.ProductionModifier or EffectType.MultiplierModifier => value >= 0 ? ColorPositive : ColorNegative,
                _ => ColorNeutral
            };
        }
    }
}

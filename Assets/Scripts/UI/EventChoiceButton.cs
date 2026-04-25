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

        public void Setup(EventChoiceData choice, Action onClicked)
        {
            choiceText.text = choice.text;
            effectText.text = choice.effectDescription;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke());
        }
    }
}

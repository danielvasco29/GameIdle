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
            choiceText = choiceText ?? GetComponentInChildren<TextMeshProUGUI>();
            button     = button     ?? GetComponent<Button>() ?? gameObject.AddComponent<Button>();

            if (choiceText != null) choiceText.text = choice.text;
            if (effectText != null) effectText.text  = choice.effectDescription;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke());
        }
    }
}

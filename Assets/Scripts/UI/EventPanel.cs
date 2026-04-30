using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class EventPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private Button watchAdButton;

        private EventData currentEvent;
        private readonly List<GameObject> spawnedChoices = new();

        private void Awake()
        {
            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(OnWatchAdClicked);
            gameObject.SetActive(false);
        }

        public void Show(EventData eventData)
        {
            if (titleText == null || descriptionText == null) return;
            currentEvent = eventData;
            gameObject.SetActive(true);

            titleText.text = eventData.title;
            descriptionText.text = eventData.description;

            foreach (var go in spawnedChoices) Destroy(go);
            spawnedChoices.Clear();

            for (int i = 0; i < eventData.choices.Length; i++)
            {
                int idx = i;
                var go = Instantiate(choiceButtonPrefab, choicesContainer);
                go.GetComponent<EventChoiceButton>().Setup(
                    eventData.choices[i],
                    () => OnChoiceSelected(idx)
                );
                spawnedChoices.Add(go);
            }
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            GameEventSystem.Instance.ResolveEvent(currentEvent, choiceIndex);
            gameObject.SetActive(false);
        }

        private void OnWatchAdClicked()
        {
            MonetizationManager.Instance.RewardResolveEvent(currentEvent);
            gameObject.SetActive(false);
        }
    }
}

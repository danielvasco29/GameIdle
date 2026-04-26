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
        [SerializeField] private Button adButton;
        [SerializeField] private Button closeButton;

        private EventData currentEvent;
        private readonly List<GameObject> spawnedChoices = new();
        private PanelAnimator animator;

        private PanelAnimator Anim =>
            animator != null ? animator : (animator = gameObject.AddComponent<PanelAnimator>());

        private void Awake()
        {
            if (adButton != null)
                adButton.onClick.AddListener(OnWatchAdClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(() => Anim.Close());

            if (adButton != null && adButton.GetComponent<AnimatedButton>() == null)
                adButton.gameObject.AddComponent<AnimatedButton>();
            if (closeButton != null && closeButton.GetComponent<AnimatedButton>() == null)
                closeButton.gameObject.AddComponent<AnimatedButton>();
        }

        public void Show(EventData eventData)
        {
            currentEvent       = eventData;
            titleText.text     = eventData.title;
            descriptionText.text = eventData.description;

            foreach (var go in spawnedChoices) Destroy(go);
            spawnedChoices.Clear();

            for (int i = 0; i < eventData.choices.Length; i++)
            {
                int idx = i;
                var go  = Instantiate(choiceButtonPrefab, choicesContainer);
                go.GetComponent<EventChoiceButton>().Setup(
                    eventData.choices[i],
                    () => OnChoiceSelected(idx));
                spawnedChoices.Add(go);
            }

            Anim.Open();
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            GameEventSystem.Instance.ResolveEvent(currentEvent, choiceIndex);
            Anim.Close();
        }

        private void OnWatchAdClicked()
        {
            if (MonetizationManager.Instance != null)
                MonetizationManager.Instance.RewardResolveEvent(currentEvent);
            Anim.Close();
        }
    }
}

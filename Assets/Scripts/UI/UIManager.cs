using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Display Principal")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI mpsText;
        [SerializeField] private TextMeshProUGUI prestigeInfoText;

        [Header("Personagens")]
        [SerializeField] private Transform charactersContent;
        [SerializeField] private GameObject characterButtonPrefab;

        [Header("Botões")]
        [SerializeField] private Button prestigeButton;

        [Header("Painéis")]
        [SerializeField] private EventPanel eventPanel;
        [SerializeField] private PrestigePanel prestigePanel;
        [SerializeField] private OfflineProgressPanel offlinePanel;

        [Header("Toast")]
        [SerializeField] private ToastMessage toast;

        private readonly List<CharacterButton> characterButtons = new();
        private float uiRefreshTimer;
        private const float UiRefreshInterval = 0.1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            GameManager.Instance.OnStatsUpdated += UpdateStatsDisplay;
            CharacterManager.Instance.OnCharactersUpdated += RebuildCharacterButtons;
            GameEventSystem.Instance.OnEventTriggered += ShowEventPanel;
            prestigeButton.onClick.AddListener(() => prestigePanel.Show());

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
                GameManager.Instance.OnStatsUpdated -= UpdateStatsDisplay;
            }
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharactersUpdated -= RebuildCharacterButtons;
            if (GameEventSystem.Instance != null)
                GameEventSystem.Instance.OnEventTriggered -= ShowEventPanel;
        }

        private void Update()
        {
            uiRefreshTimer -= Time.deltaTime;
            if (uiRefreshTimer <= 0)
            {
                uiRefreshTimer = UiRefreshInterval;
                UpdateMoneyDisplay();
                RefreshButtonAffordability();
            }
        }

        public void RefreshAll()
        {
            UpdateMoneyDisplay();
            UpdateStatsDisplay();
            RebuildCharacterButtons();
        }

        private void UpdateMoneyDisplay()
        {
            moneyText.text = $"${NumberFormatter.Format(GameManager.Instance.Money)}";
        }

        private void UpdateStatsDisplay()
        {
            mpsText.text = $"+{NumberFormatter.Format(GameManager.Instance.MoneyPerSecond)}/s";

            bool canPrestige = GameManager.Instance.CanPrestige();
            prestigeInfoText.text = canPrestige
                ? "PRESTIGIO DISPONIVEL!"
                : $"Prestígio em: ${NumberFormatter.Format(GameManager.Instance.GetPrestigeRequirement())}";

            if (prestigeButton != null)
                prestigeButton.interactable = canPrestige;
        }

        private void RebuildCharacterButtons()
        {
            foreach (var btn in characterButtons)
                if (btn != null) Destroy(btn.gameObject);
            characterButtons.Clear();

            var chars = CharacterManager.Instance.GetAllCharacters();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!chars[i].isUnlocked) continue;
                var go = Instantiate(characterButtonPrefab, charactersContent);
                var btn = go.GetComponent<CharacterButton>();
                btn.Setup(chars[i], i);
                characterButtons.Add(btn);
            }
        }

        private void RefreshButtonAffordability()
        {
            foreach (var btn in characterButtons)
                if (btn != null) btn.Refresh();
        }

        public void ShowToast(string message) => toast.Show(message);

        public void ShowOfflineProgress(double earned, long seconds) =>
            offlinePanel.Show(earned, seconds);

        public void ShowEventPanel(EventData eventData) =>
            eventPanel.Show(eventData);
    }
}

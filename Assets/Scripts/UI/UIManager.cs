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

        private Canvas mainCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            mainCanvas = FindObjectOfType<Canvas>();

            LoadCharacterSprites();
            GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            GameManager.Instance.OnStatsUpdated += UpdateStatsDisplay;
            CharacterManager.Instance.OnCharactersUpdated += RebuildCharacterButtons;
            GameEventSystem.Instance.OnEventTriggered += ShowEventPanel;
            prestigeButton.onClick.AddListener(() => prestigePanel.Show());

            if (prestigeButton.GetComponent<AnimatedButton>() == null)
                prestigeButton.gameObject.AddComponent<AnimatedButton>();

            CreateTapZone();
            RefreshAll();
        }

        // --- Tap Zone (click-to-earn) ---

        private void CreateTapZone()
        {
            if (mainCanvas == null) return;

            RectTransform panelMain = null;
            foreach (Transform child in mainCanvas.transform)
            {
                if (child.name == "Panel_Main") { panelMain = child as RectTransform; break; }
            }
            if (panelMain == null) return;

            var go = new GameObject("TapZone", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panelMain, false);
            go.transform.SetAsFirstSibling(); // behind other UI elements

            var rt              = (RectTransform)go.transform;
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var img         = go.GetComponent<Image>();
            img.color       = Color.clear;

            var btn         = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(OnTapZone);
            go.AddComponent<AnimatedButton>();
        }

        private void OnTapZone()
        {
            double earned = GameManager.Instance.ClickMoney();
            SpawnFloatingMoney(earned, Input.mousePosition);
        }

        public void SpawnFloatingMoney(double amount, Vector3 screenPos)
        {
            if (mainCanvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)mainCanvas.transform,
                screenPos,
                mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera,
                out Vector2 localPos);

            FloatingText.Spawn(mainCanvas.transform, localPos,
                $"+${NumberFormatter.Format(amount)}", new Color(0.3f, 1f, 0.5f));
        }

        // --- Sprites ---

        private void LoadCharacterSprites()
        {
            var sheet = Resources.Load<Texture2D>("CharacterSheet");
            if (sheet == null) return;

            string[] order = {
                "dev", "marketing", "designer", "ceo", "manager",
                "cto", "analista_dados", "suporte_n1", "suporte_n2", "analista_redes",
                "analista_infra", "escovador_bits", "puxa_saco", "secretaria"
            };

            int cols = 5, rows = 3;
            int cellW = sheet.width / cols;
            int cellH = sheet.height / rows;

            var allChars = Resources.LoadAll<CharacterData>("Characters");
            var byId = new System.Collections.Generic.Dictionary<string, CharacterData>();
            foreach (var c in allChars) byId[c.characterId] = c;

            for (int i = 0; i < order.Length; i++)
            {
                int col = i % cols;
                int row = rows - 1 - (i / cols);
                var rect   = new Rect(col * cellW, row * cellH, cellW, cellH);
                var sprite = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f));
                if (byId.TryGetValue(order[i], out var data))
                    data.icon = sprite;
            }
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
                var go  = Instantiate(characterButtonPrefab, charactersContent);
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

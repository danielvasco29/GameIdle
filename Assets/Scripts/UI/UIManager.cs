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
            mainCanvas = FindFirstObjectByType<Canvas>();

            // Ensure Toast is active regardless of scene state
            if (toast != null) toast.gameObject.SetActive(true);

            LoadCharacterSprites();
            LoadSceneImages();
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

        // --- Scene Background ---

        private void LoadSceneImages()
        {
            var scene = Resources.Load<Texture2D>("GameScene");
            if (scene == null)
            {
                Debug.LogWarning("[UIManager] GameScene.png nao encontrado em Resources/");
                return;
            }

            int W = scene.width;
            int H = scene.height;

            // Concept art layout (1536x1024):
            //   Top  ~41%: Logo (left 35%) | Isometric office (right 65%)
            //   Mid  ~25%: UI mockup panels  — ignored
            //   Bot  ~34%: 4 room views side by side (CEO/Reuniao/Pesquisa/Relatorios)
            //
            // Unity Y=0 is at BOTTOM of texture, so we flip:
            int topH = Mathf.RoundToInt(H * 0.41f); // ~420px
            int botH = Mathf.RoundToInt(H * 0.34f); // ~348px
            int roomW = W / 4;                        // ~384px

            // Isometric office — right 65% of the top strip
            int officeX = Mathf.RoundToInt(W * 0.35f); // ~537
            var officeSprite = Sprite.Create(scene,
                new Rect(officeX, H - topH, W - officeX, topH),
                new Vector2(0.5f, 0.5f));

            // Bottom rooms
            var ceoSprite      = Sprite.Create(scene, new Rect(0,          0, roomW, botH), new Vector2(0.5f, 0.5f));
            var meetingSprite   = Sprite.Create(scene, new Rect(roomW,     0, roomW, botH), new Vector2(0.5f, 0.5f));
            var researchSprite  = Sprite.Create(scene, new Rect(roomW * 2, 0, roomW, botH), new Vector2(0.5f, 0.5f));
            var reportsSprite   = Sprite.Create(scene, new Rect(roomW * 3, 0, roomW, botH), new Vector2(0.5f, 0.5f));

            // Office isometric as Panel_Main background
            ApplySceneBackground("Panel_Main", officeSprite, 0.65f);

            // CEO room as Panel_Left background (behind character list)
            ApplySceneBackground("Panel_Left", ceoSprite, 0.45f);

            Debug.Log($"[UIManager] GameScene carregado: {W}x{H}, office=({officeX},{H - topH},{W - officeX},{topH})");
        }

        private void ApplySceneBackground(string panelName, Sprite sprite, float alpha)
        {
            if (mainCanvas == null) return;

            RectTransform panel = null;
            foreach (Transform child in mainCanvas.transform)
                if (child.name == panelName) { panel = child as RectTransform; break; }
            if (panel == null) return;

            var go = new GameObject("SceneBG", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            go.transform.SetAsFirstSibling();

            var rt              = (RectTransform)go.transform;
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var img             = go.GetComponent<Image>();
            img.sprite          = sprite;
            img.color           = new Color(1f, 1f, 1f, alpha);
            img.type            = Image.Type.Simple;
            img.preserveAspect  = false;
            img.raycastTarget   = false; // don't block clicks from elements behind
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

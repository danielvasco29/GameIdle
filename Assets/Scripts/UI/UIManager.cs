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
        private RoomSystem roomSystem;

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

            roomSystem = gameObject.AddComponent<RoomSystem>();
            CeoPanel.Create(mainCanvas);

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
            PolishLayout();
            RefreshAll();
        }

        // --- Layout polish ---
        // Repositions and restyles existing scene UI at runtime so it doesn't
        // require touching the .unity file. Run once after backgrounds inject.

        private static RectTransform FindRectByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root as RectTransform;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindRectByName(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private void PolishLayout()
        {
            if (mainCanvas == null) return;

            var canvasTr = mainCanvas.transform;
            RectTransform panelMain = FindRectByName(canvasTr, "Panel_Main");
            RectTransform panelLeft = FindRectByName(canvasTr, "Panel_Left");

            // Strengthen the dark overlays so foreground text wins against the scene art
            DarkenOverlay(panelMain, 0.55f);
            DarkenOverlay(panelLeft, 0.70f);

            // Money: large, bold, centered at top of Panel_Main
            if (moneyText != null)
            {
                var rt = moneyText.rectTransform;
                rt.SetParent(panelMain != null ? (Transform)panelMain : canvasTr, false);
                rt.anchorMin       = new Vector2(0.15f, 0.92f);
                rt.anchorMax       = new Vector2(0.85f, 1f);
                rt.pivot           = new Vector2(0.5f, 1f);
                rt.sizeDelta       = Vector2.zero;
                rt.anchoredPosition = new Vector2(0f, -10f);
                moneyText.fontSize    = 56f;
                moneyText.fontStyle   = FontStyles.Bold;
                moneyText.color       = new Color(0.45f, 1f, 0.6f);
                moneyText.alignment   = TextAlignmentOptions.Center;
                moneyText.outlineWidth = 0.25f;
                moneyText.outlineColor = Color.black;
                moneyText.transform.SetAsLastSibling();
            }

            // MPS: smaller, just below money
            if (mpsText != null)
            {
                var rt = mpsText.rectTransform;
                rt.SetParent(panelMain != null ? (Transform)panelMain : canvasTr, false);
                rt.anchorMin       = new Vector2(0.15f, 0.85f);
                rt.anchorMax       = new Vector2(0.85f, 0.92f);
                rt.pivot           = new Vector2(0.5f, 0.5f);
                rt.sizeDelta       = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                mpsText.fontSize   = 24f;
                mpsText.color      = new Color(0.75f, 0.92f, 1f, 0.9f);
                mpsText.alignment  = TextAlignmentOptions.Center;
                mpsText.transform.SetAsLastSibling();
            }

            // "Sua Startup": centered band below money
            var companyInfo = FindRectByName(panelMain, "CompanyInfo");
            if (companyInfo != null)
            {
                companyInfo.anchorMin       = new Vector2(0.15f, 0.78f);
                companyInfo.anchorMax       = new Vector2(0.85f, 0.85f);
                companyInfo.pivot           = new Vector2(0.5f, 0.5f);
                companyInfo.sizeDelta       = Vector2.zero;
                companyInfo.anchoredPosition = Vector2.zero;
                var companyTmp = companyInfo.GetComponentInChildren<TextMeshProUGUI>();
                if (companyTmp != null)
                {
                    companyTmp.fontSize     = 32f;
                    companyTmp.fontStyle    = FontStyles.Bold;
                    companyTmp.color        = new Color(1f, 0.93f, 0.55f);
                    companyTmp.alignment    = TextAlignmentOptions.Center;
                    companyTmp.outlineWidth = 0.25f;
                    companyTmp.outlineColor = Color.black;
                }
                companyInfo.SetAsLastSibling();
            }

            // PrestigeInfo subtitle: directly under "Sua Startup"
            var prestigeInfo = FindRectByName(panelMain, "PrestigeInfo");
            if (prestigeInfo != null)
            {
                prestigeInfo.anchorMin       = new Vector2(0.15f, 0.73f);
                prestigeInfo.anchorMax       = new Vector2(0.85f, 0.78f);
                prestigeInfo.pivot           = new Vector2(0.5f, 0.5f);
                prestigeInfo.sizeDelta       = Vector2.zero;
                prestigeInfo.anchoredPosition = Vector2.zero;
                if (prestigeInfoText != null)
                {
                    prestigeInfoText.fontSize  = 16f;
                    prestigeInfoText.color     = new Color(1f, 1f, 1f, 0.75f);
                    prestigeInfoText.alignment = TextAlignmentOptions.Center;
                }
                prestigeInfo.SetAsLastSibling();
            }

            // Prestige button: shrink and pin to top-right corner of Panel_Main, with halo
            if (prestigeButton != null)
            {
                var rt = (RectTransform)prestigeButton.transform;
                rt.anchorMin       = new Vector2(1f, 1f);
                rt.anchorMax       = new Vector2(1f, 1f);
                rt.pivot           = new Vector2(1f, 1f);
                rt.sizeDelta       = new Vector2(170f, 56f);
                rt.anchoredPosition = new Vector2(-18f, -18f);
                rt.SetAsLastSibling();

                AddPrestigeGlow(prestigeButton);

                var btnImg = prestigeButton.GetComponent<Image>();
                if (btnImg != null) btnImg.color = new Color(0.55f, 0.30f, 0.95f, 0.92f);

                var btnLbl = prestigeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnLbl != null)
                {
                    btnLbl.fontSize     = 20f;
                    btnLbl.fontStyle    = FontStyles.Bold;
                    btnLbl.color        = Color.white;
                    btnLbl.alignment    = TextAlignmentOptions.Center;
                    btnLbl.outlineWidth = 0.25f;
                    btnLbl.outlineColor = new Color(0f, 0f, 0f, 0.8f);
                }
            }
        }

        private static void DarkenOverlay(Transform panel, float alpha)
        {
            if (panel == null) return;
            var overlay = FindRectByName(panel, "BGOverlay");
            if (overlay == null) return;
            var img = overlay.GetComponent<Image>();
            if (img != null) img.color = new Color(0f, 0f, 0f, alpha);
        }

        private static void AddPrestigeGlow(Button btn)
        {
            // Skip if already added
            if (btn.transform.Find("Glow") != null) return;

            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(btn.transform, false);
            glow.transform.SetAsFirstSibling();

            var rt          = (RectTransform)glow.transform;
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.sizeDelta    = new Vector2(28f, 28f);
            rt.anchoredPosition = Vector2.zero;

            var img         = glow.GetComponent<Image>();
            img.color       = new Color(0.75f, 0.45f, 1f, 0.35f);
            img.raycastTarget = false;
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
            Debug.Log($"[UIManager] GameScene carregado: {W}x{H}");

            // Concept art layout (1536x1024):
            //   Top  ~41%: Logo (left 35%) | Isometric office (right 65%)
            //   Mid  ~25%: UI mockup panels — ignored
            //   Bot  ~34%: 4 room views side by side
            int topH  = Mathf.RoundToInt(H * 0.41f);
            int botH  = Mathf.RoundToInt(H * 0.34f);
            int roomW = W / 4;

            int officeX = Mathf.RoundToInt(W * 0.35f);
            var officeSprite = Sprite.Create(scene,
                new Rect(officeX, H - topH, W - officeX, topH),
                new Vector2(0.5f, 0.5f));

            var ceoSprite = Sprite.Create(scene,
                new Rect(0, 0, roomW, botH),
                new Vector2(0.5f, 0.5f));

            // Office goes inside Panel_Main (behind its UI children)
            // CEO room goes inside Panel_Left (behind character list)
            InjectBackground("Panel_Main", officeSprite, 0.60f);
            InjectBackground("Panel_Left", ceoSprite,    0.38f);

            // Wire room bar at bottom of Panel_Main
            if (roomSystem != null)
            {
                RectTransform panelMain = null;
                foreach (Transform child in mainCanvas.transform)
                    if (child.name == "Panel_Main") { panelMain = child as RectTransform; break; }
                if (panelMain != null)
                    roomSystem.BuildBar(panelMain, scene, W, H);
            }
        }

        private void InjectBackground(string panelName, Sprite sprite, float alpha)
        {
            if (mainCanvas == null) return;

            // Find panel as direct child of Canvas
            RectTransform panel = null;
            foreach (Transform child in mainCanvas.transform)
                if (child.name == panelName) { panel = child as RectTransform; break; }

            if (panel == null)
            {
                Debug.LogWarning($"[UIManager] Panel '{panelName}' nao encontrado no Canvas");
                return;
            }

            Debug.Log($"[UIManager] Injetando background em '{panelName}'");

            var go = new GameObject("SceneBG", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            go.transform.SetAsFirstSibling(); // behind all children of this panel

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
            img.raycastTarget   = false;

            // Dark overlay for text legibility
            var overlay = new GameObject("BGOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(panel, false);
            overlay.transform.SetSiblingIndex(1); // just above SceneBG
            var ovRt            = (RectTransform)overlay.transform;
            ovRt.anchorMin      = Vector2.zero;
            ovRt.anchorMax      = Vector2.one;
            ovRt.sizeDelta      = Vector2.zero;
            ovRt.anchoredPosition = Vector2.zero;
            var ovImg           = overlay.GetComponent<Image>();
            ovImg.color         = new Color(0f, 0f, 0f, 0.45f);
            ovImg.raycastTarget = false;
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

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FontStyles = TMPro.FontStyles;

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

        // Polish layout references
        private static readonly Color NeonGreen  = new(0.45f, 1f,    0.6f);
        private static readonly Color NeonCyan   = new(0.3f,  0.95f, 1.0f);
        private static readonly Color NeonOrange = new(1.0f,  0.6f,  0.2f);

        // Effects HUD (item 2)
        private GameObject effectsHUD;
        private float effectsHUDTimer;
        private const float EffectsHUDInterval = 0.25f;

        // Prestige progress bar (item 7)
        private Image prestigeProgressBar;

        // Floating money bursts (item 8)
        private RectTransform panelMain;
        private float floatBurstTimer;
        private const float FloatBurstInterval = 1.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            AutoFindComponents();

            GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            GameManager.Instance.OnStatsUpdated += UpdateStatsDisplay;
            CharacterManager.Instance.OnCharactersUpdated += RebuildCharacterButtons;
            GameEventSystem.Instance.OnEventTriggered += ShowEventPanel;
            if (prestigeButton != null)
                prestigeButton.onClick.AddListener(() => prestigePanel.Show());

            RefreshAll();
            PolishLayout();
        }

        private static TextMeshProUGUI GetOrAddSceneTMP(string goName)
        {
            var go = GameObject.Find(goName);
            if (go == null) return null;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = "";
                var refFont = Object.FindFirstObjectByType<TextMeshProUGUI>();
                if (refFont != null && refFont.font != null) tmp.font = refFont.font;
            }
            return tmp;
        }

        private void AutoFindComponents()
        {
            if (moneyText == null)        moneyText        = GetOrAddSceneTMP("MoneyText");
            if (mpsText == null)          mpsText          = GetOrAddSceneTMP("MpsText");
            if (prestigeInfoText == null) prestigeInfoText = GetOrAddSceneTMP("PrestigeInfo");
            if (prestigeButton == null)
            {
                var btnGO = GameObject.Find("PrestigeButton");
                if (btnGO != null) prestigeButton = btnGO.GetComponent<Button>();
            }
            if (charactersContent == null)
            {
                var contentGO = GameObject.Find("Content");
                if (contentGO != null) charactersContent = contentGO.transform;
            }

            // Garante que o Content tem VerticalLayoutGroup + ContentSizeFitter
            if (charactersContent != null)
            {
                var contentGO = charactersContent.gameObject;
                var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
                if (vlg == null)
                {
                    vlg = contentGO.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = 8;
                    vlg.padding = new RectOffset(8, 8, 8, 8);
                    vlg.childControlWidth   = true;
                    vlg.childControlHeight  = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                }
                var csf = contentGO.GetComponent<ContentSizeFitter>();
                if (csf == null)
                {
                    csf = contentGO.AddComponent<ContentSizeFitter>();
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }
        }

        private void PolishLayout()
        {
            ApplyTitleStyle();
            SetupEffectsHUD();
            SetupEquipeHeader();
            ApplyNeonTheme();
            SetupPrestigeProgressBar();
            ExpandPanelLeft();
            var pmGO = GameObject.Find("Panel_Main");
            if (pmGO != null) panelMain = pmGO.GetComponent<RectTransform>();
        }

        private void ExpandPanelLeft()
        {
            var panelLeft = GameObject.Find("Panel_Left");
            if (panelLeft == null) return;
            var rt = panelLeft.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.sizeDelta        = new Vector2(480f, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(240f, rt.anchoredPosition.y);
        }

        private void ApplyTitleStyle()
        {
            var titleGO = GameObject.Find("TitleText");
            if (titleGO == null) return;

            var tmp = titleGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize   = 28;
                tmp.fontStyle  = FontStyles.Bold;
                tmp.color      = NeonGreen;
            }

            // Subtle semi-transparent stripe behind the whole top bar
            var topBar = titleGO.transform.parent;
            if (topBar == null) return;
            var stripeGO = new GameObject("TopBarStripe", typeof(RectTransform), typeof(Image));
            stripeGO.transform.SetParent(topBar, false);
            stripeGO.transform.SetAsFirstSibling();
            var rt  = stripeGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = stripeGO.GetComponent<Image>();
            img.color         = new Color(0.05f, 0.18f, 0.10f, 0.55f);
            img.raycastTarget = false;
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

            effectsHUDTimer -= Time.deltaTime;
            if (effectsHUDTimer <= 0)
            {
                effectsHUDTimer = EffectsHUDInterval;
                RefreshEffectsHUD();
            }

            if (panelMain != null && GameManager.Instance.MoneyPerSecond > 0)
            {
                floatBurstTimer -= Time.deltaTime;
                if (floatBurstTimer <= 0)
                {
                    floatBurstTimer = FloatBurstInterval;
                    SpawnFloatingMoney();
                }
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
            if (moneyText != null)
                moneyText.text = $"${NumberFormatter.Format(GameManager.Instance.Money)}";
        }

        private void UpdateStatsDisplay()
        {
            if (mpsText != null)
                mpsText.text = $"+{NumberFormatter.Format(GameManager.Instance.MoneyPerSecond)}/s";

            bool canPrestige = GameManager.Instance.CanPrestige();
            if (prestigeInfoText != null)
                prestigeInfoText.text = canPrestige
                    ? "⭐ Prestígio disponível!"
                    : $"Prestígio em: ${NumberFormatter.Format(1_000_000_000.0)}";

            if (prestigeButton != null)
                prestigeButton.interactable = canPrestige;

            UpdatePrestigeProgressBar();
        }

        private void RebuildCharacterButtons()
        {
            foreach (var btn in characterButtons)
                if (btn != null) Destroy(btn.gameObject);
            characterButtons.Clear();

            if (characterButtonPrefab == null || charactersContent == null) return;

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

        private void SetupPrestigeProgressBar()
        {
            if (prestigeButton == null) return;
            var barGO = new GameObject("PrestigeBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(prestigeButton.transform, false);
            var rt = barGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, 4f);
            prestigeProgressBar              = barGO.GetComponent<Image>();
            prestigeProgressBar.type         = Image.Type.Filled;
            prestigeProgressBar.fillMethod   = Image.FillMethod.Horizontal;
            prestigeProgressBar.raycastTarget = false;
            UpdatePrestigeProgressBar();
        }

        private void UpdatePrestigeProgressBar()
        {
            if (prestigeProgressBar == null) return;
            bool ready = GameManager.Instance.CanPrestige();
            float fill = Mathf.Clamp01(
                (float)(GameManager.Instance.TotalEarned / GameManager.Instance.GetPrestigeRequirement()));
            prestigeProgressBar.fillAmount = fill;
            prestigeProgressBar.color = ready
                ? new Color(1f, 0.84f, 0f, 1f)
                : new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.85f);
        }

        private void ApplyNeonTheme()
        {
            if (moneyText != null) moneyText.color = NeonGreen;
            if (mpsText   != null) mpsText.color   = NeonCyan;

            // PrestigeButtonLabel is scene-serialized → outline is safe
            var presLabel = GameObject.Find("PrestigeButtonLabel")?.GetComponent<TextMeshProUGUI>();
            if (presLabel != null)
            {
                presLabel.outlineColor = NeonCyan;
                presLabel.outlineWidth = 0.15f;
            }

            var companyInfo = GameObject.Find("CompanyInfo")?.GetComponent<TextMeshProUGUI>();
            if (companyInfo != null) companyInfo.color = NeonOrange;
        }

        private void SetupEquipeHeader()
        {
            var panelLeft = GameObject.Find("Panel_Left");
            if (panelLeft == null) return;

            // Shrink ScrollView top edge to make room
            var scrollView = panelLeft.transform.Find("ScrollView");
            if (scrollView != null)
            {
                var srt = scrollView.GetComponent<RectTransform>();
                srt.offsetMax = new Vector2(srt.offsetMax.x, -32f);
            }

            // 32 px stripe anchored to top of Panel_Left
            var headerGO = new GameObject("EquipeHeader", typeof(RectTransform), typeof(Image));
            headerGO.transform.SetParent(panelLeft.transform, false);
            headerGO.transform.SetAsFirstSibling();
            var hrt = headerGO.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.offsetMin = new Vector2(0f, -32f);
            hrt.offsetMax = Vector2.zero;
            var himg = headerGO.GetComponent<Image>();
            himg.color         = new Color(0.12f, 0.08f, 0.04f, 0.92f);
            himg.raycastTarget = false;

            var labelGO = new GameObject("EquipeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(headerGO.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var ltmp = labelGO.GetComponent<TextMeshProUGUI>();
            ltmp.text         = "EQUIPE";
            ltmp.fontSize     = 15;
            ltmp.fontStyle    = FontStyles.Bold;
            ltmp.color        = NeonOrange;
            ltmp.alignment    = TextAlignmentOptions.Center;
            ltmp.raycastTarget = false;
        }

        private void SetupEffectsHUD()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            effectsHUD = new GameObject("EffectsHUD", typeof(RectTransform), typeof(Image));
            effectsHUD.transform.SetParent(canvas.transform, false);

            var rt = effectsHUD.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -82f);
            rt.sizeDelta        = new Vector2(620f, 28f);

            var bg = effectsHUD.GetComponent<Image>();
            bg.color         = new Color(0f, 0f, 0f, 0.3f);
            bg.raycastTarget = false;

            var hlg = effectsHUD.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing              = 6;
            hlg.padding              = new RectOffset(6, 6, 3, 3);
            hlg.childAlignment       = TextAnchor.MiddleCenter;
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            effectsHUD.SetActive(false);
        }

        private void RefreshEffectsHUD()
        {
            if (effectsHUD == null) return;
            var effects = GameManager.Instance.GetActiveEffects();

            for (int i = effectsHUD.transform.childCount - 1; i >= 0; i--)
                Destroy(effectsHUD.transform.GetChild(i).gameObject);

            if (effects.Count == 0) { effectsHUD.SetActive(false); return; }
            effectsHUD.SetActive(true);

            foreach (var effect in effects)
            {
                Color pillColor = GetEffectPillColor(effect);
                var pillGO = new GameObject("Pill", typeof(RectTransform), typeof(Image));
                pillGO.transform.SetParent(effectsHUD.transform, false);
                pillGO.GetComponent<RectTransform>().sizeDelta = new Vector2(115, 22);
                var pillImg = pillGO.GetComponent<Image>();
                pillImg.color         = new Color(pillColor.r, pillColor.g, pillColor.b, 0.65f);
                pillImg.raycastTarget = false;

                var textGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(pillGO.transform, false);
                var textRt = textGO.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(3, 0);
                textRt.offsetMax = new Vector2(-3, 0);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.text      = FormatEffectLabel(effect);
                tmp.fontSize  = 9;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            }
        }

        private static Color GetEffectPillColor(EventEffect e)
        {
            if (e.isPermanent)                                         return new Color(1f,   0.84f, 0f);
            if (e.value < 0)                                           return new Color(1f,   0.3f,  0.3f);
            if (e.type == EffectType.MultiplierModifier)               return new Color(0.3f, 0.6f,  1f);
            return new Color(0.3f, 0.9f, 0.4f);
        }

        private static string FormatEffectLabel(EventEffect e)
        {
            string t = e.type switch
            {
                EffectType.ProductionModifier => "PROD",
                EffectType.MultiplierModifier => "MULT",
                EffectType.MoneyBonus         => "BONUS",
                _                             => "FX"
            };
            string v = e.value >= 0 ? $"+{e.value * 100:F0}%" : $"{e.value * 100:F0}%";
            return e.isPermanent ? $"{v} {t} ∞" : $"{v} {t} {e.timeRemaining:F0}s";
        }

        private void SpawnFloatingMoney()
        {
            var go = new GameObject("FloatMoney", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FloatingText));
            go.transform.SetParent(panelMain, false);
            var rt = go.GetComponent<RectTransform>();
            float halfW = panelMain.rect.width  * 0.35f;
            float halfH = panelMain.rect.height * 0.35f;
            rt.anchoredPosition = new Vector2(Random.Range(-halfW, halfW), -halfH);
            rt.sizeDelta        = new Vector2(160f, 30f);
            double amount = GameManager.Instance.MoneyPerSecond * FloatBurstInterval;
            go.GetComponent<FloatingText>().Init($"+${NumberFormatter.Format(amount)}", NeonGreen);
        }

        public void ShowToast(string message, Color? color = null) => toast.Show(message, color);

        public void ShowOfflineProgress(double earned, long seconds) =>
            offlinePanel.Show(earned, seconds);

        public void ShowEventPanel(EventData eventData) =>
            eventPanel.Show(eventData);
    }
}

using System.Collections;
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

        private static readonly Color NeonGreen  = new(0.45f, 1f,    0.6f);
        private static readonly Color NeonCyan   = new(0.3f,  0.95f, 1.0f);
        private static readonly Color NeonOrange = new(1.0f,  0.6f,  0.2f);

        private GameObject effectsHUD;
        private float effectsHUDTimer;
        private const float EffectsHUDInterval = 0.25f;

        private Image prestigeProgressBar;

        private RectTransform panelMain;
        private float floatBurstTimer;
        private const float FloatBurstInterval = 1.5f;

        // Tap button
        private RectTransform tapButtonRT;
        private TextMeshProUGUI tapValueText;

        // Próximo desbloqueio
        private TextMeshProUGUI nextUnlockNameText;
        private TextMeshProUGUI nextUnlockCostText;
        private Image nextUnlockBar;

        // Stats do Panel_Main
        private TextMeshProUGUI statMpsText;
        private TextMeshProUGUI statMultText;
        private TextMeshProUGUI statTotalText;
        private TextMeshProUGUI statPrestigeText;

        // Contador suave de dinheiro
        private double displayedMoney;

        // Cached TMP font to avoid repeated FindAnyObjectByType calls
        private TMP_FontAsset cachedFont;

        // Prestige button label (cached to avoid Find every frame)
        private TextMeshProUGUI prestigeButtonLabel;

        // Ranking panel
        private RankingPanel rankingPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnsureInputComponents();
        }

        private void EnsureInputComponents()
        {
            // GraphicRaycaster GUID in scene may reference old built-in assembly;
            // ensure one always exists on the Canvas so UI buttons can receive clicks.
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            // Ensure a working EventSystem + StandaloneInputModule.
            // In Unity 6 with com.unity.inputsystem installed, the scene's
            // StandaloneInputModule GUID may not resolve → no UI input at all.
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                var esGO = new GameObject("EventSystem");
                es = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            else if (es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
            {
                // The existing EventSystem may have a missing-script input module;
                // add StandaloneInputModule so the old Input Manager can drive UI.
                es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void Start()
        {
            AutoFindComponents();

            GameManager.Instance.OnStatsUpdated += UpdateStatsDisplay;
            CharacterManager.Instance.OnCharactersUpdated += RebuildCharacterButtons;
            GameEventSystem.Instance.OnEventTriggered += ShowEventPanel;
            if (prestigeButton != null)
            {
                prestigeButton.onClick.RemoveAllListeners();
                prestigeButton.onClick.AddListener(OnPrestigeDirectClick);
            }

            RefreshAll();
            displayedMoney = GameManager.Instance.Money;
            PolishLayout();
        }

        private TMP_FontAsset GetCachedFont()
        {
            if (cachedFont != null) return cachedFont;
            var existing = Object.FindAnyObjectByType<TextMeshProUGUI>();
            if (existing != null && existing.font != null) cachedFont = existing.font;
            return cachedFont;
        }

        private TextMeshProUGUI GetOrAddSceneTMP(string goName)
        {
            var go = GameObject.Find(goName);
            if (go == null) return null;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = "";
                var f = GetCachedFont();
                if (f != null) tmp.font = f;
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
                if (btnGO != null)
                    prestigeButton = btnGO.GetComponent<Button>() ?? btnGO.AddComponent<Button>();
            }
            if (charactersContent == null)
            {
                var contentGO = GameObject.Find("Content");
                if (contentGO != null) charactersContent = contentGO.transform;
            }

            // Ensure ScrollRect exists on ScrollView (GUID may be broken in scene).
            EnsureScrollRect();

            if (charactersContent != null)
            {
                var contentGO = charactersContent.gameObject;

                var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
                if (vlg != null) DestroyImmediate(vlg);

                var glg = contentGO.GetComponent<GridLayoutGroup>();
                if (glg == null)
                {
                    glg = contentGO.AddComponent<GridLayoutGroup>();
                    glg.cellSize        = new Vector2(460f, 120f);
                    glg.spacing         = new Vector2(0f, 6f);
                    glg.padding         = new RectOffset(8, 8, 6, 6);
                    glg.childAlignment  = TextAnchor.UpperLeft;
                    glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                    glg.constraintCount = 1;
                }

                var csf = contentGO.GetComponent<ContentSizeFitter>();
                if (csf == null)
                {
                    csf = contentGO.AddComponent<ContentSizeFitter>();
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }
        }

        private void EnsureScrollRect()
        {
            var scrollViewGO = GameObject.Find("ScrollView");
            if (scrollViewGO == null) return;

            var sr = scrollViewGO.GetComponent<ScrollRect>();
            if (sr == null) sr = scrollViewGO.AddComponent<ScrollRect>();

            // Wire viewport and content if not already set
            var viewport = scrollViewGO.transform.Find("Viewport");
            if (viewport == null) viewport = scrollViewGO.transform; // fallback

            var content = viewport.Find("Content") ?? scrollViewGO.transform.Find("Content");

            if (sr.viewport == null && viewport != null)
                sr.viewport = viewport.GetComponent<RectTransform>();
            if (sr.content == null && content != null)
                sr.content = content.GetComponent<RectTransform>();

            sr.horizontal = false;
            sr.vertical   = true;
            sr.scrollSensitivity = 30f;
            if (sr.movementType == ScrollRect.MovementType.Unrestricted)
                sr.movementType = ScrollRect.MovementType.Clamped;

            // Viewport needs a Mask to clip children
            if (viewport != null && viewport != scrollViewGO.transform)
            {
                var mask = viewport.GetComponent<Mask>();
                if (mask == null) mask = viewport.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                var maskImg = viewport.GetComponent<Image>();
                if (maskImg == null)
                {
                    maskImg = viewport.gameObject.AddComponent<Image>();
                    maskImg.color = Color.white;
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
            StylePrestigeButton();
            ExpandPanelLeft();
            SetupTapButton();
            SetupMainStats();
            SetupNextUnlockBanner();
            SetupRankingPanel();
        }

        private void SetupRankingPanel()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            // Panel
            var panelGO = new GameObject("RankingPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvas.transform, false);
            rankingPanel = panelGO.AddComponent<RankingPanel>();

            // Ranking button — top-right corner, large enough to tap
            var btnGO = new GameObject("RankingButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(canvas.transform, false);
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-6f, -6f);
            brt.sizeDelta = new Vector2(80f, 44f);
            btnGO.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.28f, 0.95f);
            btnGO.GetComponent<Button>().onClick.AddListener(OpenRanking);

            var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(btnGO.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var ltmp = lblGO.GetComponent<TextMeshProUGUI>();
            ltmp.text = "TOP\nRANKING";
            ltmp.fontSize = 11;
            ltmp.fontStyle = FontStyles.Bold;
            ltmp.color = new Color(1f, 0.84f, 0f);
            ltmp.alignment = TextAlignmentOptions.Center;
            ltmp.raycastTarget = false;
            var ff = GetCachedFont();
            if (ff != null) ltmp.font = ff;
        }

        private void OpenRanking()
        {
            if (rankingPanel != null) rankingPanel.Open();
        }

        // ── Tap Button ────────────────────────────────────────────────────────

        private void SetupTapButton()
        {
            var pmGO = GameObject.Find("Panel_Main");
            if (pmGO == null) return;
            panelMain = pmGO.GetComponent<RectTransform>();

            // Destroy stale objects left in the backup scene from a previous play session
            for (int i = pmGO.transform.childCount - 1; i >= 0; i--)
            {
                string n = pmGO.transform.GetChild(i).name;
                if (n == "PanelBG" || n == "TapButton" || n == "TapValue")
                    DestroyImmediate(pmGO.transform.GetChild(i).gameObject);
            }

            // Fundo escuro para o painel principal
            var bgGO = new GameObject("PanelBG", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(pmGO.transform, false);
            bgGO.transform.SetAsFirstSibling();
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.07f, 0.12f, 1f);
            bgImg.raycastTarget = false;

            // Botão principal
            var tapGO = new GameObject("TapButton", typeof(RectTransform), typeof(Image), typeof(Button));
            tapGO.transform.SetParent(pmGO.transform, false);
            tapButtonRT = tapGO.GetComponent<RectTransform>();
            tapButtonRT.anchorMin = tapButtonRT.anchorMax = tapButtonRT.pivot = new Vector2(0.5f, 0.5f);
            tapButtonRT.anchoredPosition = new Vector2(0f, 50f);
            tapButtonRT.sizeDelta = new Vector2(220f, 220f);
            var tapImg = tapGO.GetComponent<Image>();
            tapImg.color = new Color(0.08f, 0.42f, 0.22f, 1f);
            var tapBtn = tapGO.GetComponent<Button>();
            tapBtn.targetGraphic = tapImg;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(tapGO.transform, false);
            var lRT = labelGO.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero;
            lRT.anchorMax = Vector2.one;
            lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            var lTMP = labelGO.GetComponent<TextMeshProUGUI>();
            lTMP.text = "TRABALHAR\n[ >_ ]";
            lTMP.fontSize = 24;
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.color = Color.white;
            lTMP.alignment = TextAlignmentOptions.Center;
            lTMP.raycastTarget = false;

            // Texto "+$X / tap" abaixo do botão
            var tvGO = new GameObject("TapValue", typeof(RectTransform), typeof(TextMeshProUGUI));
            tvGO.transform.SetParent(pmGO.transform, false);
            var tvRT = tvGO.GetComponent<RectTransform>();
            tvRT.anchorMin = tvRT.anchorMax = tvRT.pivot = new Vector2(0.5f, 0.5f);
            tvRT.anchoredPosition = new Vector2(0f, -75f);
            tvRT.sizeDelta = new Vector2(280f, 36f);
            tapValueText = tvGO.GetComponent<TextMeshProUGUI>();
            tapValueText.fontSize = 17;
            tapValueText.fontStyle = FontStyles.Bold;
            tapValueText.color = NeonCyan;
            tapValueText.alignment = TextAlignmentOptions.Center;
            tapValueText.raycastTarget = false;
            UpdateTapValueText();

            tapBtn.onClick.AddListener(OnTapClicked);
            StartCoroutine(PulseTapButton());
        }

        private void UpdateTapValueText()
        {
            if (tapValueText == null || GameManager.Instance == null) return;
            tapValueText.text = $"+${NumberFormatter.Format(GameManager.Instance.GetTapValue())} / tap";
        }

        private void OnTapClicked()
        {
            double val = GameManager.Instance.GetTapValue();
            GameManager.Instance.Tap();
            UpdateTapValueText();

            if (panelMain != null)
            {
                var go = new GameObject("FloatTap", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FloatingText));
                go.transform.SetParent(panelMain, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200f, 40f);
                rt.anchoredPosition = new Vector2(Random.Range(-60f, 60f), Random.Range(20f, 90f));
                go.GetComponent<FloatingText>().Init($"+${NumberFormatter.Format(val)}", NeonGreen);
            }

            if (tapButtonRT != null) StartCoroutine(PunchScale(tapButtonRT, 0.12f));
        }

        private IEnumerator PulseTapButton()
        {
            if (tapButtonRT == null) yield break;
            var img = tapButtonRT.GetComponent<Image>();
            var c1  = new Color(0.08f, 0.42f, 0.22f, 1f);
            var c2  = new Color(0.14f, 0.60f, 0.32f, 1f);
            while (tapButtonRT != null && img != null)
            {
                float e = 0f;
                while (e < 0.9f) { e += Time.deltaTime; img.color = Color.Lerp(c1, c2, e / 0.9f); yield return null; }
                e = 0f;
                while (e < 0.9f) { e += Time.deltaTime; img.color = Color.Lerp(c2, c1, e / 0.9f); yield return null; }
            }
        }

        private IEnumerator PunchScale(RectTransform rt, float duration)
        {
            if (rt == null) yield break;
            Vector3 orig = rt.localScale;
            Vector3 big  = orig * 1.12f;
            float half   = duration * 0.5f;
            float e = 0f;
            while (e < half) { e += Time.deltaTime; rt.localScale = Vector3.Lerp(orig, big, e / half); yield return null; }
            e = 0f;
            while (e < half) { e += Time.deltaTime; rt.localScale = Vector3.Lerp(big, orig, e / half); yield return null; }
            rt.localScale = orig;
        }

        // ── Layout & Theme ────────────────────────────────────────────────────

        private void ExpandPanelLeft()
        {
            var panelLeft = GameObject.Find("Panel_Left");
            if (panelLeft == null) return;
            var rt = panelLeft.GetComponent<RectTransform>();
            if (rt == null) return;
            // 520px gives the 2-column grid (228×2 + spacing + padding) room
            // to clear the vertical scrollbar (~15px) without being clipped.
            rt.sizeDelta        = new Vector2(520f, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(260f, rt.anchoredPosition.y);
        }

        private void ApplyTitleStyle()
        {
            var titleGO = GameObject.Find("TitleText");
            if (titleGO == null) return;
            var tmp = titleGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { tmp.fontSize = 28; tmp.fontStyle = FontStyles.Bold; tmp.color = NeonGreen; }

            var topBar = titleGO.transform.parent;
            if (topBar == null) return;
            var stripeGO = new GameObject("TopBarStripe", typeof(RectTransform), typeof(Image));
            stripeGO.transform.SetParent(topBar, false);
            stripeGO.transform.SetAsFirstSibling();
            var rt = stripeGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = stripeGO.GetComponent<Image>();
            img.color = new Color(0.05f, 0.18f, 0.10f, 0.55f);
            img.raycastTarget = false;
        }

        private void ApplyNeonTheme()
        {
            if (moneyText != null) moneyText.color = NeonGreen;
            if (mpsText   != null) mpsText.color   = NeonCyan;

            var presLabel = GameObject.Find("PrestigeButtonLabel")?.GetComponent<TextMeshProUGUI>();
            if (presLabel != null) { presLabel.outlineColor = NeonCyan; presLabel.outlineWidth = 0.15f; }

            var companyInfo = GameObject.Find("CompanyInfo")?.GetComponent<TextMeshProUGUI>();
            if (companyInfo != null) companyInfo.color = NeonOrange;
        }

        private void SetupEquipeHeader()
        {
            var panelLeft = GameObject.Find("Panel_Left");
            if (panelLeft == null) return;

            var scrollView = panelLeft.transform.Find("ScrollView");
            if (scrollView != null)
            {
                var srt = scrollView.GetComponent<RectTransform>();
                srt.offsetMax = new Vector2(srt.offsetMax.x, -32f);
            }

            var headerGO = new GameObject("EquipeHeader", typeof(RectTransform), typeof(Image));
            headerGO.transform.SetParent(panelLeft.transform, false);
            headerGO.transform.SetAsFirstSibling();
            var hrt = headerGO.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f);
            hrt.offsetMin = new Vector2(0f, -32f); hrt.offsetMax = Vector2.zero;
            var himg = headerGO.GetComponent<Image>();
            himg.color = new Color(0.12f, 0.08f, 0.04f, 0.92f);
            himg.raycastTarget = false;

            var labelGO = new GameObject("EquipeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(headerGO.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var ltmp = labelGO.GetComponent<TextMeshProUGUI>();
            ltmp.text = "EQUIPE"; ltmp.fontSize = 15; ltmp.fontStyle = FontStyles.Bold;
            ltmp.color = NeonOrange; ltmp.alignment = TextAlignmentOptions.Center;
            ltmp.raycastTarget = false;
        }

        // ── Effects HUD ───────────────────────────────────────────────────────

        private void SetupEffectsHUD()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            effectsHUD = new GameObject("EffectsHUD", typeof(RectTransform), typeof(Image));
            effectsHUD.transform.SetParent(canvas.transform, false);
            var rt = effectsHUD.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -82f);
            rt.sizeDelta = new Vector2(620f, 28f);
            var bg = effectsHUD.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.3f);
            bg.raycastTarget = false;
            var hlg = effectsHUD.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.padding = new RectOffset(6, 6, 3, 3);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
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
                Color pc = GetEffectPillColor(effect);
                var pillGO = new GameObject("Pill", typeof(RectTransform), typeof(Image));
                pillGO.transform.SetParent(effectsHUD.transform, false);
                pillGO.GetComponent<RectTransform>().sizeDelta = new Vector2(115, 22);
                var pImg = pillGO.GetComponent<Image>();
                pImg.color = new Color(pc.r, pc.g, pc.b, 0.65f); pImg.raycastTarget = false;
                var tGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
                tGO.transform.SetParent(pillGO.transform, false);
                var trt = tGO.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(3, 0); trt.offsetMax = new Vector2(-3, 0);
                var tmp = tGO.GetComponent<TextMeshProUGUI>();
                tmp.text = FormatEffectLabel(effect); tmp.fontSize = 9;
                tmp.fontStyle = FontStyles.Bold; tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            }
        }

        private static Color GetEffectPillColor(EventEffect e)
        {
            if (e.isPermanent)                               return new Color(1f,   0.84f, 0f);
            if (e.value < 0)                                 return new Color(1f,   0.3f,  0.3f);
            if (e.type == EffectType.MultiplierModifier)     return new Color(0.3f, 0.6f,  1f);
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

        // ── Prestige Progress Bar ─────────────────────────────────────────────

        private void SetupPrestigeProgressBar()
        {
            if (prestigeButton == null) return;
            var barGO = new GameObject("PrestigeBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(prestigeButton.transform, false);
            var rt = barGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0f, 4f);
            prestigeProgressBar = barGO.GetComponent<Image>();
            prestigeProgressBar.type = Image.Type.Filled;
            prestigeProgressBar.fillMethod = Image.FillMethod.Horizontal;
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

        private void StylePrestigeButton()
        {
            if (prestigeButton == null) return;

            var img = prestigeButton.GetComponent<Image>();
            if (img == null) img = prestigeButton.gameObject.AddComponent<Image>();
            img.color = new Color(0.55f, 0.1f, 0.8f, 1f);
            prestigeButton.targetGraphic = img;

            // Collapse the vertical stretch (anchorMin.y != anchorMax.y in scene) to a
            // single point at the bottom so sizeDelta.y becomes the true fixed height.
            var rt = prestigeButton.GetComponent<RectTransform>();
            float anchorX0 = rt.anchorMin.x; // keep horizontal anchors from scene
            float anchorX1 = rt.anchorMax.x;
            rt.anchorMin        = new Vector2(anchorX0, 0f);
            rt.anchorMax        = new Vector2(anchorX1, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(0f, 52f);   // 52 px tall, full width between anchors
            rt.anchoredPosition = new Vector2(0f, 10f);   // 10 px from bottom of parent

            var labelGO = prestigeButton.transform.Find("PrestigeButtonLabel");
            TextMeshProUGUI label = labelGO != null
                ? (labelGO.GetComponent<TextMeshProUGUI>() ?? labelGO.gameObject.AddComponent<TextMeshProUGUI>())
                : null;
            if (label == null)
            {
                var go = new GameObject("PrestigeButtonLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(prestigeButton.transform, false);
                var lrt = go.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                label = go.GetComponent<TextMeshProUGUI>();
            }
            label.fontSize  = 14;
            label.fontStyle = FontStyles.Bold;
            label.color     = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var f = GetCachedFont();
            if (f != null) label.font = f;
            prestigeButtonLabel = label;

            RefreshPrestigeLabel();
        }

        private void RefreshPrestigeLabel()
        {
            if (prestigeButtonLabel == null)
            {
                var go = prestigeButton?.transform.Find("PrestigeButtonLabel");
                if (go != null) prestigeButtonLabel = go.GetComponent<TextMeshProUGUI>();
            }
            if (prestigeButtonLabel == null || GameManager.Instance == null) return;

            int count       = GameManager.Instance.PrestigeCount;
            double nextMult = 1.0 + (count + 1) * 0.5;
            bool ready      = GameManager.Instance.CanPrestige();

            prestigeButtonLabel.text = ready
                ? $"PRESTÍGIO ★\n#{count} → x{nextMult:F1}"
                : $"PRESTÍGIO\n#{count} → x{nextMult:F1}";

            var img = prestigeButton?.GetComponent<Image>();
            if (img != null)
                img.color = ready
                    ? new Color(0.8f, 0.2f, 1.0f, 1f)
                    : new Color(0.55f, 0.1f, 0.8f, 1f);
        }

        // ── Floating Money ────────────────────────────────────────────────────

        private void SpawnFloatingMoney()
        {
            var go = new GameObject("FloatMoney", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FloatingText));
            go.transform.SetParent(panelMain, false);
            var rt = go.GetComponent<RectTransform>();
            float halfW = panelMain.rect.width  * 0.35f;
            float halfH = panelMain.rect.height * 0.35f;
            rt.anchoredPosition = new Vector2(Random.Range(-halfW, halfW), -halfH);
            rt.sizeDelta = new Vector2(160f, 30f);
            double amount = GameManager.Instance.MoneyPerSecond * FloatBurstInterval;
            go.GetComponent<FloatingText>().Init($"+${NumberFormatter.Format(amount)}", NeonGreen);
        }

        // ── Core UI Update ────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsUpdated -= UpdateStatsDisplay;
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharactersUpdated -= RebuildCharacterButtons;
            if (GameEventSystem.Instance != null)
                GameEventSystem.Instance.OnEventTriggered -= ShowEventPanel;
        }

        private void Update()
        {
            // Contador suave: sobe com lerp, cai imediato (gasto)
            if (GameManager.Instance != null)
            {
                double target = GameManager.Instance.Money;
                displayedMoney = target < displayedMoney
                    ? target
                    : displayedMoney + (target - displayedMoney) * (double)Mathf.Min(1f, Time.deltaTime * 8f);
                if (moneyText != null)
                    moneyText.text = $"${NumberFormatter.Format(displayedMoney)}";
            }

            uiRefreshTimer -= Time.deltaTime;
            if (uiRefreshTimer <= 0)
            {
                uiRefreshTimer = UiRefreshInterval;
                RefreshButtonAffordability();
                RefreshNextUnlockBanner();
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

        private void OnPrestigeDirectClick()
        {
            if (!GameManager.Instance.CanPrestige()) { ShowToast("Prestígio requer $1B total!", new Color(1f,0.4f,0.4f)); return; }
            GameManager.Instance.Prestige();
            ShowToast("Prestígio realizado! Multiplicador aumentado.", new Color(1f, 0.84f, 0f));
        }

        public void RefreshAll()
        {
            displayedMoney = GameManager.Instance.Money;
            UpdateStatsDisplay();
            RebuildCharacterButtons();
        }

        private void UpdateStatsDisplay()
        {
            if (mpsText != null)
                mpsText.text = $"+{NumberFormatter.Format(GameManager.Instance.MoneyPerSecond)}/s";

            bool canPrestige = GameManager.Instance.CanPrestige();
            if (prestigeInfoText != null)
                prestigeInfoText.text = canPrestige
                    ? "[*] Prestígio disponível!"
                    : $"Prestígio em: ${NumberFormatter.Format(1_000_000_000.0)}";

            if (prestigeButton != null)
                prestigeButton.interactable = canPrestige;

            UpdatePrestigeProgressBar();
            RefreshPrestigeLabel();
            UpdateTapValueText();
            RefreshMainStats();
        }

        private void RebuildCharacterButtons()
        {
            var chars = CharacterManager.Instance.GetAllCharacters();

            // Count how many are now unlocked
            int unlocked = 0;
            for (int i = 0; i < chars.Length; i++)
                if (chars[i].isUnlocked) unlocked++;

            // If the count hasn't changed just refresh labels — no destroy/create needed
            if (unlocked == characterButtons.Count && characterButtons.Count > 0)
            {
                foreach (var btn in characterButtons)
                    if (btn != null) btn.Refresh();
                RefreshNextUnlockBanner();
                return;
            }

            // New character unlocked (or reset after prestige) — full rebuild
            foreach (var btn in characterButtons)
                if (btn != null) Destroy(btn.gameObject);
            characterButtons.Clear();

            if (characterButtonPrefab == null || charactersContent == null) return;

            for (int i = 0; i < chars.Length; i++)
            {
                if (!chars[i].isUnlocked) continue;
                var go = Instantiate(characterButtonPrefab, charactersContent);
                var btn = go.GetComponent<CharacterButton>() ?? go.AddComponent<CharacterButton>();
                btn.Setup(chars[i], i);
                characterButtons.Add(btn);
            }
            RefreshNextUnlockBanner();
        }

        private void RefreshButtonAffordability()
        {
            foreach (var btn in characterButtons)
                if (btn != null) btn.Refresh();
        }

        // ── Stats Panel_Main ─────────────────────────────────────────────────

        private void SetupMainStats()
        {
            if (panelMain == null) return;

            // Remove stale StatsCard from backup scene
            var stale = panelMain.Find("StatsCard");
            if (stale != null) DestroyImmediate(stale.gameObject);

            // Card de stats no topo do Panel_Main
            var cardGO = new GameObject("StatsCard", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(panelMain, false);
            var crt = cardGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.offsetMin = new Vector2(12f, -108f);
            crt.offsetMax = new Vector2(-12f, -8f);
            cardGO.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.88f);
            cardGO.GetComponent<Image>().raycastTarget = false;

            // Linha 1: MPS (esquerda) + Multiplicador (direita)
            statMpsText = CreateBannerLabel(cardGO.transform, "StatMPS",
                new Vector2(0f, 0.5f), new Vector2(0.58f, 1f),
                new Vector2(10f, 0f), new Vector2(0f, -4f),
                "", 16, NeonCyan, TextAlignmentOptions.MidlineLeft);

            statMultText = CreateBannerLabel(cardGO.transform, "StatMult",
                new Vector2(0.58f, 0.5f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(-10f, -4f),
                "", 15, new Color(1f, 0.92f, 0.35f), TextAlignmentOptions.MidlineRight);

            // Linha 2: Total ganho (esquerda) + Prestígio (direita)
            statTotalText = CreateBannerLabel(cardGO.transform, "StatTotal",
                new Vector2(0f, 0f), new Vector2(0.58f, 0.5f),
                new Vector2(10f, 4f), new Vector2(0f, 0f),
                "", 13, new Color(1f, 1f, 1f, 0.65f), TextAlignmentOptions.MidlineLeft);

            statPrestigeText = CreateBannerLabel(cardGO.transform, "StatPrestige",
                new Vector2(0.58f, 0f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(-10f, 0f),
                "", 13, NeonOrange, TextAlignmentOptions.MidlineRight);

            RefreshMainStats();
        }

        private void RefreshMainStats()
        {
            if (statMpsText == null) return;
            statMpsText.text = $"+{NumberFormatter.Format(GameManager.Instance.MoneyPerSecond)}/s";
            double mult = CharacterManager.Instance.GetTotalMultiplier()
                          * GameManager.Instance.PrestigeMultiplier;
            statMultText.text  = $"x{mult:F2} mult";
            statTotalText.text = $"Total: ${NumberFormatter.Format(GameManager.Instance.TotalEarned)}";
            int pc = GameManager.Instance.PrestigeCount;
            statPrestigeText.text = pc > 0 ? $"[P] Prestígio x{pc}" : "";
        }

        // ── Próximo Desbloqueio ───────────────────────────────────────────────

        private void SetupNextUnlockBanner()
        {
            var panelLeft = GameObject.Find("Panel_Left");
            if (panelLeft == null) return;

            // Encolhe o ScrollView pelo baixo para dar espaço ao banner
            var scrollView = panelLeft.transform.Find("ScrollView");
            if (scrollView != null)
            {
                var srt = scrollView.GetComponent<RectTransform>();
                srt.offsetMin = new Vector2(srt.offsetMin.x, 75f);
            }

            // Container do banner
            var bannerGO = new GameObject("NextUnlockBanner", typeof(RectTransform), typeof(Image));
            bannerGO.transform.SetParent(panelLeft.transform, false);
            var brt = bannerGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = new Vector2(0f, 75f);
            bannerGO.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.02f, 0.95f);
            bannerGO.GetComponent<Image>().raycastTarget = false;

            // Título "PRÓXIMO DESBLOQUEIO"
            CreateBannerLabel(bannerGO.transform, "Title",
                new Vector2(0f, 0.65f), new Vector2(1f, 1f),
                new Vector2(8f, 0f), new Vector2(-8f, -3f),
                "PRÓXIMO DESBLOQUEIO", 11, NeonOrange, TextAlignmentOptions.MidlineLeft);

            // Nome do personagem
            nextUnlockNameText = CreateBannerLabel(bannerGO.transform, "NextName",
                new Vector2(0f, 0.32f), new Vector2(0.58f, 0.65f),
                new Vector2(8f, 0f), Vector2.zero,
                "", 15, Color.white, TextAlignmentOptions.MidlineLeft);

            // Custo
            nextUnlockCostText = CreateBannerLabel(bannerGO.transform, "NextCost",
                new Vector2(0.58f, 0.32f), new Vector2(1f, 0.65f),
                Vector2.zero, new Vector2(-8f, 0f),
                "", 14, new Color(1f, 0.92f, 0.35f), TextAlignmentOptions.MidlineRight);

            // Fundo da barra
            var barBGGO = new GameObject("BarBG", typeof(RectTransform), typeof(Image));
            barBGGO.transform.SetParent(bannerGO.transform, false);
            var barBGRT = barBGGO.GetComponent<RectTransform>();
            barBGRT.anchorMin = new Vector2(0f, 0f);
            barBGRT.anchorMax = new Vector2(1f, 0f);
            barBGRT.offsetMin = new Vector2(8f, 7f);
            barBGRT.offsetMax = new Vector2(-8f, 18f);
            barBGGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
            barBGGO.GetComponent<Image>().raycastTarget = false;

            // Preenchimento da barra
            var barGO = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(barBGGO.transform, false);
            var barRT = barGO.GetComponent<RectTransform>();
            barRT.anchorMin = Vector2.zero; barRT.anchorMax = Vector2.one;
            barRT.offsetMin = barRT.offsetMax = Vector2.zero;
            nextUnlockBar = barGO.GetComponent<Image>();
            nextUnlockBar.type = Image.Type.Filled;
            nextUnlockBar.fillMethod = Image.FillMethod.Horizontal;
            nextUnlockBar.color = NeonOrange;
            nextUnlockBar.raycastTarget = false;

            RefreshNextUnlockBanner();
        }

        private TextMeshProUGUI CreateBannerLabel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            string text, float fontSize, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Bold;
            tmp.color = color; tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap; tmp.raycastTarget = false;
            return tmp;
        }

        private void RefreshNextUnlockBanner()
        {
            if (nextUnlockNameText == null) return;
            var (next, via) = CharacterManager.Instance.GetNextUnlock();
            if (next == null)
            {
                nextUnlockNameText.text = "✅ Todos desbloqueados!";
                if (nextUnlockCostText != null) nextUnlockCostText.text = "";
                if (nextUnlockBar != null) nextUnlockBar.fillAmount = 1f;
                return;
            }
            nextUnlockNameText.text = $">> {next.data.characterName}";
            double cost = via.GetCurrentCost();
            if (nextUnlockCostText != null)
                nextUnlockCostText.text = $"${NumberFormatter.Format(cost)}";
            if (nextUnlockBar != null)
                nextUnlockBar.fillAmount = Mathf.Clamp01((float)(GameManager.Instance.Money / cost));
        }

        public void ShowToast(string message, Color? color = null)
        {
            if (toast == null) return;
            // Toast is saved as inactive in the scene (m_IsActive=0).
            // Activate it and every ancestor so activeInHierarchy=true,
            // which is required for StartCoroutine to work.
            var t = toast.transform;
            while (t != null) { if (!t.gameObject.activeSelf) t.gameObject.SetActive(true); t = t.parent; }
            toast.Show(message, color);
        }
        public void ShowOfflineProgress(double earned, long seconds) { if (offlinePanel != null) offlinePanel.Show(earned, seconds); }
        public void ShowEventPanel(EventData eventData) => eventPanel.Show(eventData);
    }
}

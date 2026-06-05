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
        private EventPanel eventPanel;
        [SerializeField] private PrestigePanel prestigePanel;
        private OfflineProgressPanel offlinePanel;
        private MissionPanel missionPanel;
        private AchievementPanel achievementPanel;

        // Indicadores "pronto" (pontinho vermelho)
        private Image _missionDot;
        private Image _achievementDot;

        // Painéis modais — só um aberto por vez
        private readonly List<GameObject> modalPanels = new();
        public void CloseAllModals()
        {
            foreach (var p in modalPanels)
                if (p != null) p.SetActive(false);
        }

        [Header("Toast")]
        [SerializeField] private ToastMessage toast;

        private readonly List<CharacterButton> characterButtons = new();
        private float uiRefreshTimer;
        private const float UiRefreshInterval = 0.1f;

        // Idle Startup Tycoon palette
        private static readonly Color NavyDark    = new(0.086f, 0.137f, 0.220f, 1f); // #16233a
        private static readonly Color NavyCard    = new(0.106f, 0.169f, 0.275f, 1f); // #1b2b46
        private static readonly Color GoldColor   = new(1f,     0.808f, 0.227f, 1f); // #ffce3a
        private static readonly Color GreenBtn    = new(0.247f, 0.749f, 0.353f, 1f); // #3fbf5a
        private static readonly Color BlueAccent  = new(0.290f, 0.620f, 1f,    1f);  // #4a9eff
        private static readonly Color TextPrimary = new(0.933f, 0.953f, 0.980f, 1f); // #eef3fa
        private static readonly Color TextSec     = new(0.624f, 0.698f, 0.788f, 1f); // #9fb2c9
        // Keep aliases used elsewhere in the file
        private static readonly Color NeonGreen  = GreenBtn;
        private static readonly Color NeonCyan   = BlueAccent;
        private static readonly Color NeonOrange = GoldColor;

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
        private Image tapFaceImg;
        private Image tapGlowImg;
        private bool _tapPunching;

        // Boost TURBO
        private Image _boostBtnImg;
        private TextMeshProUGUI _boostBtnText;
        private static readonly Color BoostReady    = new(0.18f, 0.72f, 0.42f, 1f);
        private static readonly Color BoostActive   = new(1f, 0.75f, 0.08f, 1f);
        private static readonly Color BoostCooldown = new(0.25f, 0.28f, 0.38f, 1f);

        // Próximo desbloqueio — pulse
        private Image _nextUnlockBannerBg;
        private bool  _nextUnlockWasAffordable;

        // Office workers
        private OfficeWorkerManager _workerManager;

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

        // Bulk-buy mode toggle (x1 / x10 / Máx)
        private Button[] buyModeButtons;
        private static readonly int[] BuyModeValues = { 1, 10, -1 };

        // Gem currency display (created at runtime — gems are a new system)
        private TextMeshProUGUI gemText;
        private GemShopPanel gemShopPanel;
        private SettingsPanel settingsPanel;

        // Runtime-generated UI sprites (no dependency on Unity built-in resources)
        private static Sprite Circle()  => UiSpriteFactory.Circle();
        private static Sprite Rounded() => UiSpriteFactory.RoundedBox();

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
            SoundManager.Get(); // ensure SFX system exists
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
                if (glg == null) glg = contentGO.AddComponent<GridLayoutGroup>();
                glg.cellSize        = new Vector2(460f, 112f);
                glg.spacing         = new Vector2(0f, 9f);
                glg.padding         = new RectOffset(10, 10, 10, 10);
                glg.childAlignment  = TextAnchor.UpperCenter;
                glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 1;

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
            SetupTopBar();
            SetupEffectsHUD();
            SetupEquipeHeader();
            SetupBuyModeBar();
            ApplyNeonTheme();
            SetupPrestigeProgressBar();
            StylePrestigeButton();
            ExpandPanelLeft();
            SetupTapButton();
            SetupMainStats();
            SetupNextUnlockBanner();
            StylePrestigeNotice();
            SetupRankingPanel();
            SetupShopAndSettings();

            // Agora que offlinePanel existe, mostra o progresso offline pendente
            // (GameManager.Start roda antes deste Start por causa do execution order).
            if (_hasPendingOffline)
            {
                _hasPendingOffline = false;
                offlinePanel.Show(_pendingOfflineEarned, _pendingOfflineSeconds);
            }
        }

        private bool   _hasPendingOffline;
        private double _pendingOfflineEarned;
        private long   _pendingOfflineSeconds;

        private void SetupShopAndSettings()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            var shopGO = new GameObject("GemShopPanel", typeof(RectTransform));
            shopGO.transform.SetParent(canvas.transform, false);
            gemShopPanel = shopGO.AddComponent<GemShopPanel>();
            shopGO.SetActive(false); // ensure it never blocks raycasts while closed
            modalPanels.Add(shopGO);

            var setGO = new GameObject("SettingsPanel", typeof(RectTransform));
            setGO.transform.SetParent(canvas.transform, false);
            settingsPanel = setGO.AddComponent<SettingsPanel>();
            setGO.SetActive(false);
            modalPanels.Add(setGO);

            // Settings (menu) button — top-left corner
            var btnGO = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(canvas.transform, false);
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(8f, -8f);
            brt.sizeDelta = new Vector2(76f, 36f);
            var bImg = btnGO.GetComponent<Image>();
            bImg.sprite = Rounded(); bImg.type = Image.Type.Sliced;
            bImg.color = NavyCard;
            btnGO.GetComponent<Button>().onClick.AddListener(() => { if (settingsPanel != null) { CloseAllModals(); settingsPanel.Open(); } });
            var ml = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            ml.transform.SetParent(btnGO.transform, false);
            var mlr = ml.GetComponent<RectTransform>();
            mlr.anchorMin = Vector2.zero; mlr.anchorMax = Vector2.one;
            mlr.offsetMin = mlr.offsetMax = Vector2.zero;
            var mlt = ml.GetComponent<TextMeshProUGUI>();
            mlt.text = "MENU"; mlt.fontSize = 13; mlt.fontStyle = FontStyles.Bold;
            mlt.color = TextSec; mlt.alignment = TextAlignmentOptions.Center;
            mlt.raycastTarget = false;
            var f = GetCachedFont(); if (f != null) mlt.font = f;

            // ── Offline Progress Panel ─────────────────────────────────────
            var offlineGO = new GameObject("OfflineProgressPanel", typeof(RectTransform));
            offlineGO.transform.SetParent(canvas.transform, false);
            offlinePanel = offlineGO.AddComponent<OfflineProgressPanel>();

            // ── Mission Panel ──────────────────────────────────────────────
            var missionGO = new GameObject("MissionPanel", typeof(RectTransform));
            missionGO.transform.SetParent(canvas.transform, false);
            missionPanel = missionGO.AddComponent<MissionPanel>();
            missionGO.SetActive(false);
            modalPanels.Add(missionGO);

            // Botão Missões — ao lado do MENU
            var mBtnGO = new GameObject("MissionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            mBtnGO.transform.SetParent(canvas.transform, false);
            var mbrt = mBtnGO.GetComponent<RectTransform>();
            mbrt.anchorMin = mbrt.anchorMax = mbrt.pivot = new Vector2(0f, 1f);
            mbrt.anchoredPosition = new Vector2(92f, -8f);
            mbrt.sizeDelta = new Vector2(90f, 36f);
            var mbImg = mBtnGO.GetComponent<Image>();
            mbImg.sprite = Rounded(); mbImg.type = Image.Type.Sliced; mbImg.color = NavyCard;
            mBtnGO.GetComponent<Button>().onClick.AddListener(() => { if (missionPanel != null) { CloseAllModals(); missionPanel.Open(); } });
            var mbl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            mbl.transform.SetParent(mBtnGO.transform, false);
            var mblr = mbl.GetComponent<RectTransform>();
            mblr.anchorMin = Vector2.zero; mblr.anchorMax = Vector2.one; mblr.offsetMin = mblr.offsetMax = Vector2.zero;
            var mblt = mbl.GetComponent<TextMeshProUGUI>();
            mblt.text = "MISSOES"; mblt.fontSize = 11; mblt.fontStyle = FontStyles.Bold;
            mblt.color = TextSec; mblt.alignment = TextAlignmentOptions.Center; mblt.raycastTarget = false;
            var mf = GetCachedFont(); if (mf != null) mblt.font = mf;
            _missionDot = MakeNotifyDot(mBtnGO.transform);

            // ── Achievement Panel ──────────────────────────────────────────
            var achGO = new GameObject("AchievementPanel", typeof(RectTransform));
            achGO.transform.SetParent(canvas.transform, false);
            achievementPanel = achGO.AddComponent<AchievementPanel>();
            achGO.SetActive(false);
            modalPanels.Add(achGO);

            // ── Event Panel ────────────────────────────────────────────────
            var eventGO = new GameObject("EventPanel", typeof(RectTransform));
            eventGO.transform.SetParent(canvas.transform, false);
            eventPanel = eventGO.AddComponent<EventPanel>();
            eventGO.SetActive(false);
            modalPanels.Add(eventGO);

            // Botão Conquistas — ao lado de MISSOES
            var aBtnGO = new GameObject("AchievementButton", typeof(RectTransform), typeof(Image), typeof(Button));
            aBtnGO.transform.SetParent(canvas.transform, false);
            var abrt = aBtnGO.GetComponent<RectTransform>();
            abrt.anchorMin = abrt.anchorMax = abrt.pivot = new Vector2(0f, 1f);
            abrt.anchoredPosition = new Vector2(188f, -8f);
            abrt.sizeDelta = new Vector2(110f, 36f);
            var abImg = aBtnGO.GetComponent<Image>();
            abImg.sprite = Rounded(); abImg.type = Image.Type.Sliced; abImg.color = NavyCard;
            aBtnGO.GetComponent<Button>().onClick.AddListener(() => { if (achievementPanel != null) { CloseAllModals(); achievementPanel.Open(); } });
            var abl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            abl.transform.SetParent(aBtnGO.transform, false);
            var ablr = abl.GetComponent<RectTransform>();
            ablr.anchorMin = Vector2.zero; ablr.anchorMax = Vector2.one; ablr.offsetMin = ablr.offsetMax = Vector2.zero;
            var ablt = abl.GetComponent<TextMeshProUGUI>();
            ablt.text = "CONQUISTAS"; ablt.fontSize = 11; ablt.fontStyle = FontStyles.Bold;
            ablt.color = TextSec; ablt.alignment = TextAlignmentOptions.Center; ablt.raycastTarget = false;
            var af = GetCachedFont(); if (af != null) ablt.font = af;
            _achievementDot = MakeNotifyDot(aBtnGO.transform);
        }

        private void UpdateNotifyDots()
        {
            if (_missionDot != null)
            {
                bool show = DailyMissionSystem.HasClaimable();
                if (_missionDot.gameObject.activeSelf != show) _missionDot.gameObject.SetActive(show);
            }
            if (_achievementDot != null)
            {
                bool show = AchievementManager.HasUnseen;
                if (_achievementDot.gameObject.activeSelf != show) _achievementDot.gameObject.SetActive(show);
            }
        }

        // Pontinho vermelho de notificação no canto superior direito do botão.
        private Image MakeNotifyDot(Transform parent)
        {
            var go = new GameObject("NotifyDot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-2f, -2f);
            rt.sizeDelta = new Vector2(12f, 12f);
            var img = go.GetComponent<Image>();
            img.sprite = Circle();
            img.color = new Color(0.95f, 0.25f, 0.25f, 1f);
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }

        // The floating "Prestígio disponível" text used to overlap the prestige
        // button at the bottom. We hide it — the prestige button label now
        // carries the goal/gem info on its own.
        private void StylePrestigeNotice()
        {
            if (prestigeInfoText != null)
                prestigeInfoText.gameObject.SetActive(false);
        }

        private void SetupRankingPanel()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            // Panel
            var panelGO = new GameObject("RankingPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvas.transform, false);
            rankingPanel = panelGO.AddComponent<RankingPanel>();
            modalPanels.Add(panelGO);

            // Ranking button — top-right corner, large enough to tap
            var btnGO = new GameObject("RankingButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(canvas.transform, false);
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-6f, -6f);
            brt.sizeDelta = new Vector2(80f, 44f);
            btnGO.GetComponent<Image>().color = NavyCard;
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
            ltmp.color = GoldColor;
            ltmp.alignment = TextAlignmentOptions.Center;
            ltmp.raycastTarget = false;
            var ff = GetCachedFont();
            if (ff != null) ltmp.font = ff;
        }

        private void OpenRanking()
        {
            if (rankingPanel != null) { CloseAllModals(); rankingPanel.Open(); }
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
                if (n == "PanelBG" || n == "TapButton" || n == "TapValue" || n == "TapValuePill")
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
            // Try loading office background art; fall back to solid navy
            var officeTex = Resources.Load<Texture2D>("UI/office_bg");
            if (officeTex != null)
            {
                bgImg.sprite = Sprite.Create(officeTex, new Rect(0, 0, officeTex.width, officeTex.height), new Vector2(0.5f, 0.5f));
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = NavyDark;
            }
            bgImg.raycastTarget = false;

            // Botão principal — container só com o Button; as camadas visuais
            // são filhas para podermos empilhar brilho/sombra/face/brilho-topo.
            var tapGO = new GameObject("TapButton", typeof(RectTransform), typeof(Button));
            tapGO.transform.SetParent(pmGO.transform, false);
            tapButtonRT = tapGO.GetComponent<RectTransform>();
            tapButtonRT.anchorMin = tapButtonRT.anchorMax = tapButtonRT.pivot = new Vector2(0.5f, 0.5f);
            tapButtonRT.anchoredPosition = new Vector2(0f, 50f);
            tapButtonRT.sizeDelta = new Vector2(228f, 228f);
            var tapBtn = tapGO.GetComponent<Button>();

            Image AddLayer(string name, Vector2 offMin, Vector2 offMax, Color col, bool ray)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(tapGO.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = offMin; rt.offsetMax = offMax;
                var im = go.GetComponent<Image>();
                im.sprite = Rounded(); im.type = Image.Type.Sliced;
                im.color = col; im.raycastTarget = ray;
                return im;
            }

            // back → front
            tapGlowImg = AddLayer("Glow",   new Vector2(-26f, -26f), new Vector2(26f, 26f),
                                  new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.22f), false);
            AddLayer("Shadow", new Vector2(-3f, -14f), new Vector2(9f, -3f), new Color(0f, 0f, 0f, 0.38f), false);
            AddLayer("Border", new Vector2(-3f, -3f), new Vector2(3f, 3f), new Color(0.13f, 0.46f, 0.21f, 1f), false);
            tapFaceImg = AddLayer("Face", Vector2.zero, Vector2.zero, GreenBtn, true);
            tapBtn.targetGraphic = tapFaceImg;

            // Brilho no topo (sheen) — meia altura superior, bem suave
            var sheen = AddLayer("Sheen", Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.10f), false);
            sheen.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            sheen.rectTransform.anchorMax = new Vector2(1f, 1f);
            sheen.rectTransform.offsetMin = new Vector2(10f, 4f);
            sheen.rectTransform.offsetMax = new Vector2(-10f, -10f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(tapGO.transform, false);
            var lRT = labelGO.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero;
            lRT.anchorMax = Vector2.one;
            lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            var lTMP = labelGO.GetComponent<TextMeshProUGUI>();
            lTMP.text = "<size=30>TRABALHAR</size>\n<size=20><color=#d4f0d8>[ >_ ]</color></size>";
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.color = Color.white;
            lTMP.alignment = TextAlignmentOptions.Center;
            lTMP.raycastTarget = false;
            var lf = GetCachedFont(); if (lf != null) lTMP.font = lf;

            // Pílula dourada com o valor por clique, logo abaixo do botão
            var pillGO = new GameObject("TapValuePill", typeof(RectTransform), typeof(Image));
            pillGO.transform.SetParent(pmGO.transform, false);
            var pillRT = pillGO.GetComponent<RectTransform>();
            pillRT.anchorMin = pillRT.anchorMax = pillRT.pivot = new Vector2(0.5f, 0.5f);
            pillRT.anchoredPosition = new Vector2(0f, -82f);
            pillRT.sizeDelta = new Vector2(220f, 38f);
            var pillImg = pillGO.GetComponent<Image>();
            pillImg.sprite = Rounded(); pillImg.type = Image.Type.Sliced;
            pillImg.color = new Color(0f, 0f, 0f, 0.45f);
            pillImg.raycastTarget = false;

            var tvGO = new GameObject("TapValue", typeof(RectTransform), typeof(TextMeshProUGUI));
            tvGO.transform.SetParent(pillGO.transform, false);
            var tvRT = tvGO.GetComponent<RectTransform>();
            tvRT.anchorMin = Vector2.zero; tvRT.anchorMax = Vector2.one;
            tvRT.offsetMin = tvRT.offsetMax = Vector2.zero;
            tapValueText = tvGO.GetComponent<TextMeshProUGUI>();
            tapValueText.fontSize = 19;
            tapValueText.fontStyle = FontStyles.Bold;
            tapValueText.color = GoldColor;
            tapValueText.alignment = TextAlignmentOptions.Center;
            tapValueText.raycastTarget = false;
            var tf = GetCachedFont(); if (tf != null) tapValueText.font = tf;
            UpdateTapValueText();

            tapBtn.onClick.AddListener(OnTapClicked);

            // Hold: segura o botão para disparar repetidamente com aceleração
            var holdBtn = tapGO.AddComponent<HoldButton>();
            holdBtn.Init(OnTapClicked);

            StartCoroutine(PulseTapButton());

            // ── Botão TURBO ────────────────────────────────────────────────
            var boostGO = new GameObject("TurboButton", typeof(RectTransform), typeof(Image), typeof(Button));
            boostGO.transform.SetParent(pmGO.transform, false);
            var brt2 = boostGO.GetComponent<RectTransform>();
            brt2.anchorMin = brt2.anchorMax = brt2.pivot = new Vector2(0.5f, 0.5f);
            brt2.anchoredPosition = new Vector2(0f, -130f);
            brt2.sizeDelta = new Vector2(160f, 44f);
            _boostBtnImg = boostGO.GetComponent<Image>();
            _boostBtnImg.sprite = Rounded(); _boostBtnImg.type = Image.Type.Sliced;
            _boostBtnImg.color = BoostReady;
            boostGO.GetComponent<Button>().onClick.AddListener(OnTurboClicked);

            var btLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btLabel.transform.SetParent(boostGO.transform, false);
            var btlRT = btLabel.GetComponent<RectTransform>();
            btlRT.anchorMin = Vector2.zero; btlRT.anchorMax = Vector2.one;
            btlRT.offsetMin = btlRT.offsetMax = Vector2.zero;
            _boostBtnText = btLabel.GetComponent<TextMeshProUGUI>();
            _boostBtnText.text = "TURBO  x5  30s";
            _boostBtnText.fontSize = 15; _boostBtnText.fontStyle = FontStyles.Bold;
            _boostBtnText.alignment = TextAlignmentOptions.Center;
            _boostBtnText.color = Color.white; _boostBtnText.raycastTarget = false;
            var btf = GetCachedFont(); if (btf != null) _boostBtnText.font = btf;

            // ── Office Worker Manager ──────────────────────────────────────
            var workerGO = new GameObject("OfficeWorkerManager", typeof(OfficeWorkerManager));
            workerGO.transform.SetParent(pmGO.transform, false);
            _workerManager = workerGO.GetComponent<OfficeWorkerManager>();
            _workerManager.Init(panelMain);
        }

        private void UpdateTapValueText()
        {
            if (tapValueText == null || GameManager.Instance == null) return;
            tapValueText.text = $"+${NumberFormatter.Format(GameManager.Instance.GetTapValue())} / tap";
        }

        private void OnTurboClicked()
        {
            GameManager.Instance.ActivateTapBoost();
            AchievementManager.RegisterTurboUse();
            UpdateBoostButton();
            UpdateTapValueText();
        }

        private void UpdateBoostButton()
        {
            if (_boostBtnImg == null || _boostBtnText == null) return;
            var gm = GameManager.Instance;
            if (gm.TapBoostActive)
            {
                _boostBtnImg.color  = BoostActive;
                _boostBtnText.text  = $"TURBO  x5  {Mathf.CeilToInt(gm.TapBoostRemaining)}s";
            }
            else if (gm.TapBoostOnCooldown)
            {
                _boostBtnImg.color = BoostCooldown;
                int rem = Mathf.CeilToInt(gm.TapBoostCooldownRemaining);
                _boostBtnText.text = $"TURBO  {rem / 60}:{(rem % 60):D2}";
            }
            else
            {
                _boostBtnImg.color = BoostReady;
                _boostBtnText.text = "TURBO  x5  30s";
            }
        }

        private void OnTapClicked()
        {
            double val = GameManager.Instance.GetTapValue();
            GameManager.Instance.Tap();
            UpdateTapValueText();
            if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();

            if (panelMain != null)
            {
                var go = new GameObject("FloatTap", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FloatingText));
                go.transform.SetParent(panelMain, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200f, 40f);
                rt.anchoredPosition = new Vector2(Random.Range(-60f, 60f), Random.Range(20f, 90f));
                go.GetComponent<FloatingText>().Init($"+${NumberFormatter.Format(val)}", NeonGreen);

                // Coins flying up
                int coins = Random.Range(3, 6);
                for (int i = 0; i < coins; i++)
                    StartCoroutine(FlyCoin());
            }

            if (tapButtonRT != null && !_tapPunching) StartCoroutine(PunchScale(tapButtonRT, 0.12f));
        }

        // A small gold coin that pops out of the tap button and arcs upward.
        private IEnumerator FlyCoin()
        {
            if (panelMain == null) yield break;
            var go = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panelMain, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            float size = Random.Range(16f, 26f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = Circle();
            img.color  = GoldColor;
            img.raycastTarget = false;

            Vector2 start = new Vector2(Random.Range(-50f, 50f), 60f);
            Vector2 vel   = new Vector2(Random.Range(-110f, 110f), Random.Range(260f, 360f));
            float gravity = -620f;
            float life = 0.9f;
            float t = 0f;
            while (t < life && go != null)
            {
                float dt = Time.deltaTime;
                t += dt;
                vel.y += gravity * dt;
                start += vel * dt;
                rt.anchoredPosition = start;
                var c = img.color; c.a = Mathf.Clamp01(1f - t / life); img.color = c;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t / life);
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        private IEnumerator PulseTapButton()
        {
            if (tapFaceImg == null) yield break;
            var c1  = GreenBtn;
            var c2  = new Color(0.31f, 0.85f, 0.43f, 1f);
            var g1  = new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.16f);
            var g2  = new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.42f);
            while (tapFaceImg != null)
            {
                float e = 0f;
                while (e < 0.9f)
                {
                    e += Time.deltaTime; float t = e / 0.9f;
                    tapFaceImg.color = Color.Lerp(c1, c2, t);
                    if (tapGlowImg != null) tapGlowImg.color = Color.Lerp(g1, g2, t);
                    yield return null;
                }
                e = 0f;
                while (e < 0.9f)
                {
                    e += Time.deltaTime; float t = e / 0.9f;
                    tapFaceImg.color = Color.Lerp(c2, c1, t);
                    if (tapGlowImg != null) tapGlowImg.color = Color.Lerp(g2, g1, t);
                    yield return null;
                }
            }
        }

        private IEnumerator PunchScale(RectTransform rt, float duration)
        {
            if (rt == null) yield break;
            _tapPunching = true;
            Vector3 orig = rt.localScale;
            Vector3 big  = orig * 1.12f;
            float half   = duration * 0.5f;
            float e = 0f;
            while (e < half) { e += Time.deltaTime; rt.localScale = Vector3.Lerp(orig, big, e / half); yield return null; }
            e = 0f;
            while (e < half) { e += Time.deltaTime; rt.localScale = Vector3.Lerp(big, orig, e / half); yield return null; }
            rt.localScale = orig;
            _tapPunching = false;
        }

        // ── Layout & Theme ────────────────────────────────────────────────────

        private void ExpandPanelLeft()
        {
            var panelLeft = GameObject.Find("Panel_Left");
            if (panelLeft == null) return;
            var rt = panelLeft.GetComponent<RectTransform>();
            if (rt == null) return;
            // The office art (Panel_Main) starts at x=380 in the 1920 reference,
            // so Panel_Left must stop there — anything wider slides under the
            // office and hides the right of each card (cost + level badge).
            const float panelW = 380f;
            rt.anchorMin        = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax        = new Vector2(0f, rt.anchorMax.y);
            rt.sizeDelta        = new Vector2(panelW, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(panelW * 0.5f, rt.anchoredPosition.y);

            // Fit the card to the visible width (minus the ~17px scrollbar and the
            // grid's 10px side padding) so cost + level are never clipped.
            if (charactersContent != null)
            {
                var glg = charactersContent.GetComponent<GridLayoutGroup>();
                if (glg != null)
                    glg.cellSize = new Vector2(panelW - 38f, glg.cellSize.y);
            }
        }

        private void ApplyTitleStyle()
        {
            var titleGO = GameObject.Find("TitleText");
            if (titleGO == null) return;
            var tmp = titleGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { tmp.fontSize = 26; tmp.fontStyle = FontStyles.Bold; tmp.color = TextPrimary; }

            var topBar = titleGO.transform.parent;
            if (topBar == null) return;
            var stripeGO = new GameObject("TopBarStripe", typeof(RectTransform), typeof(Image));
            stripeGO.transform.SetParent(topBar, false);
            stripeGO.transform.SetAsFirstSibling();
            var rt = stripeGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = stripeGO.GetComponent<Image>();
            img.color = NavyDark;
            img.raycastTarget = false;
        }

        // Top-right gem pill + coin icon next to money. Gems are a new currency
        // earned from prestige, so the whole display is built at runtime.
        private void SetupTopBar()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            // Gold coin icon to the left of the money value
            if (moneyText != null)
            {
                var coinGO = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
                coinGO.transform.SetParent(moneyText.transform, false);
                var crt = coinGO.GetComponent<RectTransform>();
                crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0f, 0.5f);
                crt.anchoredPosition = new Vector2(-30f, 0f);
                crt.sizeDelta = new Vector2(24f, 24f);
                var ci = coinGO.GetComponent<Image>();
                ci.sprite = Circle();
                ci.color  = GoldColor;
                ci.raycastTarget = false;
                // "$" stamped on the coin
                var sGO = new GameObject("S", typeof(RectTransform), typeof(TextMeshProUGUI));
                sGO.transform.SetParent(coinGO.transform, false);
                var srt = sGO.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
                srt.offsetMin = srt.offsetMax = Vector2.zero;
                var stmp = sGO.GetComponent<TextMeshProUGUI>();
                stmp.text = "$"; stmp.fontSize = 16; stmp.fontStyle = FontStyles.Bold;
                stmp.color = NavyDark; stmp.alignment = TextAlignmentOptions.Center;
                stmp.raycastTarget = false;
                var f = GetCachedFont(); if (f != null) stmp.font = f;
            }

            // Gem pill, top-right (left of the ranking button) — opens the gem shop
            var pillGO = new GameObject("GemPill", typeof(RectTransform), typeof(Image), typeof(Button));
            pillGO.transform.SetParent(canvas.transform, false);
            var prt = pillGO.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 1f);
            prt.anchoredPosition = new Vector2(-94f, -8f);
            prt.sizeDelta = new Vector2(116f, 36f);
            var pImg = pillGO.GetComponent<Image>();
            pImg.sprite = Rounded(); pImg.type = Image.Type.Sliced;
            pImg.color  = NavyCard;
            pImg.raycastTarget = true;
            pillGO.GetComponent<Button>().onClick.AddListener(() => { if (gemShopPanel != null) { CloseAllModals(); gemShopPanel.Open(); } });

            // Gem icon: a cyan diamond (rounded square rotated 45°)
            var gemGO = new GameObject("GemIcon", typeof(RectTransform), typeof(Image));
            gemGO.transform.SetParent(pillGO.transform, false);
            var grt = gemGO.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0f, 0.5f);
            grt.anchoredPosition = new Vector2(12f, 0f);
            grt.sizeDelta = new Vector2(18f, 18f);
            grt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var gImg = gemGO.GetComponent<Image>();
            gImg.sprite = UiSpriteFactory.Box(); gImg.type = Image.Type.Simple;
            gImg.color  = new Color(0.32f, 0.85f, 1f, 1f);
            gImg.raycastTarget = false;

            // Gem count
            var gtGO = new GameObject("GemCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            gtGO.transform.SetParent(pillGO.transform, false);
            var gtrt = gtGO.GetComponent<RectTransform>();
            gtrt.anchorMin = new Vector2(0f, 0f); gtrt.anchorMax = new Vector2(1f, 1f);
            gtrt.offsetMin = new Vector2(34f, 0f); gtrt.offsetMax = new Vector2(-8f, 0f);
            gemText = gtGO.GetComponent<TextMeshProUGUI>();
            gemText.fontSize = 16; gemText.fontStyle = FontStyles.Bold;
            gemText.color = new Color(0.7f, 0.93f, 1f, 1f);
            gemText.alignment = TextAlignmentOptions.MidlineLeft;
            gemText.raycastTarget = false;
            var gf = GetCachedFont(); if (gf != null) gemText.font = gf;

            RefreshGemDisplay();
        }

        private void RefreshGemDisplay()
        {
            if (gemText == null || GameManager.Instance == null) return;
            int g = GameManager.Instance.Gems;
            gemText.text = g < 100_000 ? g.ToString("N0") : NumberFormatter.Format(g);
        }

        private void ApplyNeonTheme()
        {
            if (moneyText != null) { moneyText.color = GoldColor; moneyText.fontSize = 30; moneyText.fontStyle = FontStyles.Bold; }
            if (mpsText   != null) { mpsText.color   = TextPrimary; }

            var companyInfo = GameObject.Find("CompanyInfo")?.GetComponent<TextMeshProUGUI>();
            if (companyInfo != null) companyInfo.color = TextSec;
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
            himg.color = NavyDark;
            himg.raycastTarget = false;

            var labelGO = new GameObject("EquipeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(headerGO.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(12f, 0f); lrt.offsetMax = Vector2.zero;
            var ltmp = labelGO.GetComponent<TextMeshProUGUI>();
            ltmp.text = "FUNCIONÁRIOS"; ltmp.fontSize = 13; ltmp.fontStyle = FontStyles.Bold;
            ltmp.color = GoldColor; ltmp.alignment = TextAlignmentOptions.MidlineLeft;
            ltmp.raycastTarget = false;
            var lf = GetCachedFont(); if (lf != null) ltmp.font = lf;
        }

        // Dedicated bar (below the FUNCIONÁRIOS header) holding the x1/x10/Máx
        // toggle. Buttons are left-anchored so they stay in the visible column
        // — the right edge of Panel_Left is hidden behind the office artwork.
        private void SetupBuyModeBar()
        {
            var panelLeft = GameObject.Find("Panel_Left");
            if (panelLeft == null) return;

            // Push the scroll list down to make room under the 32px header.
            var scrollView = panelLeft.transform.Find("ScrollView");
            if (scrollView != null)
            {
                var srt = scrollView.GetComponent<RectTransform>();
                srt.offsetMax = new Vector2(srt.offsetMax.x, -66f);
            }

            var barGO = new GameObject("BuyModeBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(panelLeft.transform, false);
            var barRT = barGO.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0f, 1f); barRT.anchorMax = new Vector2(1f, 1f);
            barRT.offsetMin = new Vector2(0f, -66f); barRT.offsetMax = new Vector2(0f, -34f);
            var barImg = barGO.GetComponent<Image>();
            barImg.color = NavyCard;
            barImg.raycastTarget = false;

            // "Comprar:" hint at the left
            var hintGO = new GameObject("BuyHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintGO.transform.SetParent(barGO.transform, false);
            var hrt2 = hintGO.GetComponent<RectTransform>();
            hrt2.anchorMin = hrt2.anchorMax = hrt2.pivot = new Vector2(0f, 0.5f);
            hrt2.anchoredPosition = new Vector2(10f, 0f);
            hrt2.sizeDelta = new Vector2(72f, 24f);
            var htmp = hintGO.GetComponent<TextMeshProUGUI>();
            htmp.text = "Comprar:"; htmp.fontSize = 12; htmp.fontStyle = FontStyles.Bold;
            htmp.color = TextSec; htmp.alignment = TextAlignmentOptions.MidlineLeft;
            htmp.raycastTarget = false;
            var hf = GetCachedFont(); if (hf != null) htmp.font = hf;

            // x1 / x10 / Máx buttons, left-aligned right after the hint
            string[] labels = { "x1", "x10", "MÁX" };
            const float bw = 52f, gap = 6f, startX = 84f;
            buyModeButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var bGO = new GameObject($"BuyMode{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                bGO.transform.SetParent(barGO.transform, false);
                var brt = bGO.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 0.5f);
                brt.anchoredPosition = new Vector2(startX + i * (bw + gap), 0f);
                brt.sizeDelta = new Vector2(bw, 26f);
                var bImg = bGO.GetComponent<Image>();
                bImg.sprite = Rounded(); bImg.type = Image.Type.Sliced;
                int captured = i;
                bGO.GetComponent<Button>().onClick.AddListener(() => SetBuyMode(captured));

                var tGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
                tGO.transform.SetParent(bGO.transform, false);
                var trt = tGO.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
                var ttmp = tGO.GetComponent<TextMeshProUGUI>();
                ttmp.text = labels[i]; ttmp.fontSize = 13; ttmp.fontStyle = FontStyles.Bold;
                ttmp.alignment = TextAlignmentOptions.Center; ttmp.raycastTarget = false;
                var bf = GetCachedFont(); if (bf != null) ttmp.font = bf;

                buyModeButtons[i] = bGO.GetComponent<Button>();
            }
            RefreshBuyModeButtons();
        }

        private void SetBuyMode(int i)
        {
            CharacterManager.BuyAmount = BuyModeValues[i];
            RefreshBuyModeButtons();
            RefreshButtonAffordability();
            if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        }

        private void RefreshBuyModeButtons()
        {
            if (buyModeButtons == null) return;
            for (int i = 0; i < buyModeButtons.Length; i++)
            {
                if (buyModeButtons[i] == null) continue;
                bool active = CharacterManager.BuyAmount == BuyModeValues[i];
                var img = buyModeButtons[i].GetComponent<Image>();
                if (img != null) img.color = active ? GoldColor : NavyCard;
                var lbl = buyModeButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.color = active ? NavyDark : TextSec;
            }
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
            img.sprite = Rounded(); img.type = Image.Type.Sliced;
            img.color = NavyCard;
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
                ? $"PRESTIGIAR  •  +{GameManager.Instance.GetPrestigeGemReward()} gemas\n<size=80%>#{count} → x{nextMult:F1}</size>"
                : $"PRESTÍGIO  •  meta ${NumberFormatter.Format(GameManager.Instance.GetPrestigeRequirement())}\n<size=80%>#{count} → x{nextMult:F1}</size>";

            var img = prestigeButton?.GetComponent<Image>();
            if (img != null)
                img.color = ready ? GreenBtn : NavyCard;
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
                UpdateBoostButton();
                AchievementManager.CheckAll(); // conquistas de dinheiro acumulado (idle)
                UpdateNotifyDots();
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
                    ? $"Prestígio pronto! +{GameManager.Instance.GetPrestigeGemReward()} gemas"
                    : $"Prestígio em: ${NumberFormatter.Format(GameManager.Instance.GetPrestigeRequirement())}";

            if (prestigeButton != null)
                prestigeButton.interactable = canPrestige;

            UpdatePrestigeProgressBar();
            RefreshPrestigeLabel();
            UpdateTapValueText();
            RefreshMainStats();
            RefreshGemDisplay();
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

            // New character unlocked (or reset after prestige) — full rebuild.
            // SetParent(null) removes old buttons from the layout group immediately
            // so deferred Destroy() doesn't leave ghost cards visible for one frame.
            foreach (var btn in characterButtons)
            {
                if (btn == null) continue;
                btn.transform.SetParent(null);
                Destroy(btn.gameObject);
            }
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
            cardGO.GetComponent<Image>().color = NavyCard;
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
                          * GameManager.Instance.PrestigeMultiplier
                          * GemShop.GetPrestigeBonus() * GemShop.GetProductionMult();
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

            // Container do banner — clicável para comprar o próximo personagem
            var bannerGO = new GameObject("NextUnlockBanner", typeof(RectTransform), typeof(Image), typeof(Button));
            bannerGO.transform.SetParent(panelLeft.transform, false);
            var brt = bannerGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = new Vector2(0f, 75f);
            _nextUnlockBannerBg = bannerGO.GetComponent<Image>();
            _nextUnlockBannerBg.color = NavyDark;
            _nextUnlockBannerBg.raycastTarget = true;
            var bannerBtn = bannerGO.GetComponent<Button>();
            bannerBtn.targetGraphic = _nextUnlockBannerBg;
            bannerBtn.onClick.AddListener(OnNextUnlockBannerClicked);

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
                nextUnlockNameText.text = "Todos desbloqueados!";
                if (nextUnlockCostText != null) nextUnlockCostText.text = "";
                if (nextUnlockBar != null) nextUnlockBar.fillAmount = 1f;
                _nextUnlockWasAffordable = false;
                return;
            }
            nextUnlockNameText.text = $">> {next.data.characterName}";
            double cost = via.GetCurrentCost();
            if (nextUnlockCostText != null)
                nextUnlockCostText.text = $"${NumberFormatter.Format(cost)}";

            bool affordable = GameManager.Instance.Money >= cost;
            if (nextUnlockBar != null)
                nextUnlockBar.fillAmount = Mathf.Clamp01((float)(GameManager.Instance.Money / cost));

            // Pulsa o banner ao ficar acessível pela primeira vez
            if (affordable && !_nextUnlockWasAffordable)
                StartCoroutine(PulseNextUnlockBanner());
            _nextUnlockWasAffordable = affordable;
        }

        private void OnNextUnlockBannerClicked()
        {
            var (next, via) = CharacterManager.Instance.GetNextUnlock();
            if (next == null) return;
            int idx = System.Array.IndexOf(CharacterManager.Instance.GetAllCharacters(), next);
            if (idx < 0) return;
            bool ok = CharacterManager.Instance.TryUpgrade(idx);
            if (ok)
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayBuy();
                RefreshNextUnlockBanner();
                RefreshButtonAffordability();
            }
            else
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
                ShowToast("Dinheiro insuficiente!", new Color(1f, 0.3f, 0.3f));
            }
        }

        private IEnumerator PulseNextUnlockBanner()
        {
            if (_nextUnlockBannerBg == null) yield break;
            var highlight = new Color(NeonOrange.r, NeonOrange.g, NeonOrange.b, 0.35f);
            for (int i = 0; i < 3; i++)
            {
                float e = 0f;
                while (e < 0.18f) { e += Time.deltaTime; _nextUnlockBannerBg.color = Color.Lerp(NavyDark, highlight, e / 0.18f); yield return null; }
                e = 0f;
                while (e < 0.18f) { e += Time.deltaTime; _nextUnlockBannerBg.color = Color.Lerp(highlight, NavyDark, e / 0.18f); yield return null; }
            }
            _nextUnlockBannerBg.color = NavyDark;
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
        public void ShowOfflineProgress(double earned, long seconds)
        {
            if (offlinePanel != null) { offlinePanel.Show(earned, seconds); return; }
            // UI ainda não montada — guarda para mostrar ao fim do PolishLayout
            _hasPendingOffline = true;
            _pendingOfflineEarned = earned;
            _pendingOfflineSeconds = seconds;
        }
        public void ShowEventPanel(EventData eventData)
        {
            if (eventPanel == null) return;
            CloseAllModals();
            eventPanel.Show(eventData);
        }

        public void ShowAchievementToast(string name, string description, int gemReward)
        {
            ShowToast($"Conquista: {name} • +{gemReward} gemas", new Color(1f, 0.84f, 0.1f));
        }

        // Celebração ao contratar (primeiro nível) — texto grande no escritório,
        // área que não sofre clipping do ScrollView do painel esquerdo.
        public void ShowHiredCelebration(string characterName)
        {
            if (panelMain == null) return;
            var go = new GameObject("HiredCelebration", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FloatingText));
            go.transform.SetParent(panelMain, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 60f);
            rt.anchoredPosition = new Vector2(0f, 160f);
            var f = GetCachedFont(); if (f != null) go.GetComponent<TextMeshProUGUI>().font = f;
            go.GetComponent<FloatingText>().Init($"CONTRATADO: {characterName.ToUpper()}!", new Color(0.4f, 1f, 0.6f), 34f);
        }
    }
}

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
        private ActiveEffectsPanel activeEffectsPanel;
        private Image _bonusDot;

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

        // Idle Startup Tycoon palette (navy)
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
        private Image _tapRingImg;
        private bool _tapPunching;
        private Image _prestigeSheen;
        private Image _prestigeStarGlow;
        private RectTransform _prestigeStarRt;

        // Boost TURBO
        private Image _boostBtnImg;
        private TextMeshProUGUI _boostBtnText;
        private static readonly Color BoostReady    = new(0.18f, 0.72f, 0.42f, 1f);
        private static readonly Color BoostActive   = new(1f, 0.75f, 0.08f, 1f);
        private static readonly Color BoostCooldown = new(0.25f, 0.28f, 0.38f, 1f);

        // Próximo desbloqueio — pulse


        // Office workers
        private OfficeWorkerManager _workerManager;

        // Combat
        private Image           _attackBtnImg;
        private TextMeshProUGUI _attackBtnText;

        // Próximo desbloqueio

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
        private StatsPanel statsPanel;


        // Gem currency display (created at runtime — gems are a new system)
        private TextMeshProUGUI gemText;
        private GemShopPanel gemShopPanel;
        private SettingsPanel settingsPanel;
        private CombatUpgradePanel combatUpgradePanel;

        public void OpenCombatUpgrades()
        {
            if (combatUpgradePanel != null) combatUpgradePanel.Open();
        }

        // Runtime-generated UI sprites (no dependency on Unity built-in resources)
        private static Sprite Circle()  => UiSpriteFactory.Circle();
        private static Sprite Rounded() => UiSpriteFactory.RoundedBox();
        private static Sprite Glow()    => UiSpriteFactory.Glow();

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
                glg.cellSize        = new Vector2(460f, 120f);
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
            SetupTopSeparator();
            SetupEffectsHUD();
            SetupEquipeHeader();
            ApplyNeonTheme();
            SetupPrestigeProgressBar();
            StylePrestigeButton();
            ExpandPanelLeft();
            SetupTapButton();
            SetupMainStats();
            SetupNextUnlockBanner();
            StylePrestigeNotice();
            SetupRankingPanel();
            SetupStatsPanel();
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

        // Fundo glass unificado das abas do topo + uma fina linha de acento na
        // base, na cor do tema da aba. Dá coesão visual a MENU/MISSOES/etc.
        private static readonly Color TabBg = new(0.075f, 0.122f, 0.200f, 0.92f);
        private void StyleNavTab(GameObject btnGO, Color accent)
        {
            var img = btnGO.GetComponent<Image>();
            if (img != null) img.color = TabBg;

            if (btnGO.transform.Find("Accent") != null) return;
            var ac = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            ac.transform.SetParent(btnGO.transform, false);
            var art = ac.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(1f, 0f);
            art.pivot = new Vector2(0.5f, 0f);
            art.offsetMin = new Vector2(10f, 4f); art.offsetMax = new Vector2(-10f, 7f);
            var aImg = ac.GetComponent<Image>();
            aImg.sprite = Rounded(); aImg.type = Image.Type.Sliced;
            aImg.color = accent; aImg.raycastTarget = false;
        }

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
            brt.anchoredPosition = new Vector2(8f, -10f);
            brt.sizeDelta = new Vector2(96f, 50f);
            var bImg = btnGO.GetComponent<Image>();
            bImg.sprite = Rounded(); bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.10f, 0.16f, 0.26f, 1f);
            btnGO.GetComponent<Button>().onClick.AddListener(() => { if (settingsPanel != null) { CloseAllModals(); settingsPanel.Open(); } });
            var ml = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            ml.transform.SetParent(btnGO.transform, false);
            var mlr = ml.GetComponent<RectTransform>();
            mlr.anchorMin = Vector2.zero; mlr.anchorMax = Vector2.one;
            mlr.offsetMin = mlr.offsetMax = Vector2.zero;
            var mlt = ml.GetComponent<TextMeshProUGUI>();
            mlt.text = "MENU"; mlt.fontSize = 18; mlt.fontStyle = FontStyles.Bold;
            mlt.color = TextSec; mlt.alignment = TextAlignmentOptions.Center;
            mlt.textWrappingMode = TextWrappingModes.NoWrap; mlt.overflowMode = TextOverflowModes.Ellipsis;
            mlt.raycastTarget = false;
            var f = GetCachedFont(); if (f != null) mlt.font = f;
            StyleNavTab(btnGO, TextSec);

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
            mbrt.anchoredPosition = new Vector2(112f, -10f);
            mbrt.sizeDelta = new Vector2(100f, 50f);
            var mbImg = mBtnGO.GetComponent<Image>();
            mbImg.sprite = Rounded(); mbImg.type = Image.Type.Sliced;
            mbImg.color = new Color(0.10f, 0.20f, 0.32f, 1f);
            mBtnGO.GetComponent<Button>().onClick.AddListener(() => { if (missionPanel != null) { CloseAllModals(); missionPanel.Open(); } });
            var mbl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            mbl.transform.SetParent(mBtnGO.transform, false);
            var mblr = mbl.GetComponent<RectTransform>();
            mblr.anchorMin = Vector2.zero; mblr.anchorMax = Vector2.one; mblr.offsetMin = mblr.offsetMax = Vector2.zero;
            var mblt = mbl.GetComponent<TextMeshProUGUI>();
            mblt.text = "MISSOES"; mblt.fontSize = 17; mblt.fontStyle = FontStyles.Bold;
            mblt.color = NeonCyan; mblt.alignment = TextAlignmentOptions.Center; mblt.raycastTarget = false;
            mblt.textWrappingMode = TextWrappingModes.NoWrap; mblt.overflowMode = TextOverflowModes.Ellipsis;
            var mf = GetCachedFont(); if (mf != null) mblt.font = mf;
            StyleNavTab(mBtnGO, NeonCyan);
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
            abrt.anchoredPosition = new Vector2(220f, -10f);
            abrt.sizeDelta = new Vector2(138f, 50f);
            var abImg = aBtnGO.GetComponent<Image>();
            abImg.sprite = Rounded(); abImg.type = Image.Type.Sliced;
            abImg.color = new Color(0.16f, 0.14f, 0.08f, 1f);
            aBtnGO.GetComponent<Button>().onClick.AddListener(() => { if (achievementPanel != null) { CloseAllModals(); achievementPanel.Open(); } });
            var abl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            abl.transform.SetParent(aBtnGO.transform, false);
            var ablr = abl.GetComponent<RectTransform>();
            ablr.anchorMin = Vector2.zero; ablr.anchorMax = Vector2.one; ablr.offsetMin = ablr.offsetMax = Vector2.zero;
            var ablt = abl.GetComponent<TextMeshProUGUI>();
            ablt.text = "CONQUISTAS"; ablt.fontSize = 17; ablt.fontStyle = FontStyles.Bold;
            ablt.color = GoldColor; ablt.alignment = TextAlignmentOptions.Center; ablt.raycastTarget = false;
            ablt.textWrappingMode = TextWrappingModes.NoWrap; ablt.overflowMode = TextOverflowModes.Ellipsis;
            var af = GetCachedFont(); if (af != null) ablt.font = af;
            StyleNavTab(aBtnGO, GoldColor);
            _achievementDot = MakeNotifyDot(aBtnGO.transform);

            // ── Active Effects Panel ───────────────────────────────────────
            var aepGO = new GameObject("ActiveEffectsPanel", typeof(RectTransform));
            aepGO.transform.SetParent(canvas.transform, false);
            activeEffectsPanel = aepGO.AddComponent<ActiveEffectsPanel>();
            aepGO.SetActive(false);
            modalPanels.Add(aepGO);

            // Botão BÔNUS — ao lado de CONQUISTAS
            var bBtnGO = new GameObject("BonusButton", typeof(RectTransform), typeof(Image), typeof(Button));
            bBtnGO.transform.SetParent(canvas.transform, false);
            var bbrt = bBtnGO.GetComponent<RectTransform>();
            bbrt.anchorMin = bbrt.anchorMax = bbrt.pivot = new Vector2(0f, 1f);
            bbrt.anchoredPosition = new Vector2(366f, -10f);
            bbrt.sizeDelta = new Vector2(90f, 50f);
            var bbImg = bBtnGO.GetComponent<Image>();
            bbImg.sprite = Rounded(); bbImg.type = Image.Type.Sliced;
            bbImg.color = new Color(0.18f, 0.14f, 0.06f, 1f);
            bBtnGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (activeEffectsPanel != null) { CloseAllModals(); activeEffectsPanel.Open(); }
            });
            var bbl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            bbl.transform.SetParent(bBtnGO.transform, false);
            var bblr = bbl.GetComponent<RectTransform>();
            bblr.anchorMin = Vector2.zero; bblr.anchorMax = Vector2.one; bblr.offsetMin = bblr.offsetMax = Vector2.zero;
            var bblt = bbl.GetComponent<TextMeshProUGUI>();
            bblt.text = "BONUS"; bblt.fontSize = 17; bblt.fontStyle = FontStyles.Bold;
            bblt.color = GoldColor; bblt.alignment = TextAlignmentOptions.Center; bblt.raycastTarget = false;
            bblt.textWrappingMode = TextWrappingModes.NoWrap; bblt.overflowMode = TextOverflowModes.Ellipsis;
            var bf = GetCachedFont(); if (bf != null) bblt.font = bf;
            StyleNavTab(bBtnGO, GoldColor);
            _bonusDot = MakeNotifyDot(bBtnGO.transform);
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
            if (_bonusDot != null)
            {
                bool show = GameManager.Instance != null && GameManager.Instance.GetActiveEffects().Count > 0;
                if (_bonusDot.gameObject.activeSelf != show) _bonusDot.gameObject.SetActive(show);
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

        // ── Combat ────────────────────────────────────────────────────────────

        private BattlePanel _battlePanel;

        private void SetupCombat(GameObject pmGO)
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            // ── CombatManager ─────────────────────────────────────────────
            var cm = FindFirstObjectByType<CombatManager>();
            if (cm == null)
            {
                var cmGO = new GameObject("CombatManager");
                cm = cmGO.AddComponent<CombatManager>();
            }

            // ── BattlePanel (full-screen, above everything) ───────────────
            var bpGO = new GameObject("BattlePanel", typeof(RectTransform));
            bpGO.transform.SetParent(canvas.transform, false);
            _battlePanel = bpGO.AddComponent<BattlePanel>();
            // BattlePanel must NOT be in modalPanels — CloseAllModals would close the arena.
            _battlePanel.SubscribeToCombat(cm);

            // ── CombatUpgradePanel ────────────────────────────────────────
            var cupGO = new GameObject("CombatUpgradePanel", typeof(RectTransform));
            cupGO.transform.SetParent(canvas.transform, false);
            combatUpgradePanel = cupGO.AddComponent<CombatUpgradePanel>();
            modalPanels.Add(cupGO);

            // ── BATALHAR button — red circle, bottom-left of Panel_Main ──
            var batGO = new GameObject("BattleButton", typeof(RectTransform), typeof(Button));
            batGO.transform.SetParent(pmGO.transform, false);
            var batRT = batGO.GetComponent<RectTransform>();
            batRT.anchorMin = batRT.anchorMax = batRT.pivot = new Vector2(0f, 0f);
            batRT.anchoredPosition = new Vector2(30f, 30f);
            batRT.sizeDelta = new Vector2(160f, 160f);

            Image AddBatCircle(string name, Vector2 offMin, Vector2 offMax, Color col, bool ray)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(batGO.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = offMin; rt.offsetMax = offMax;
                var im = go.GetComponent<Image>();
                im.sprite = Circle(); im.type = Image.Type.Simple;
                im.color = col; im.raycastTarget = ray;
                return im;
            }

            AddBatCircle("Ring",   new Vector2(-40f,-40f), new Vector2(40f,40f),  new Color(0.95f,0.22f,0.22f,0.26f), false).sprite = Glow();
            AddBatCircle("Glow",   new Vector2(-22f,-22f), new Vector2(22f,22f),  new Color(0.95f,0.22f,0.22f,0.34f), false).sprite = Glow();
            AddBatCircle("Border", new Vector2( -2f, -2f), new Vector2( 2f, 2f),  new Color(0.40f,0.05f,0.05f,1f),   false);
            _attackBtnImg = AddBatCircle("Face", Vector2.zero, Vector2.zero, new Color(0.80f,0.12f,0.12f,1f), true);
            batGO.GetComponent<Button>().targetGraphic = _attackBtnImg;

            var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(batGO.transform, false);
            var lRT = lblGO.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = new Vector2(6f,6f); lRT.offsetMax = new Vector2(-6f,-6f);
            _attackBtnText = lblGO.GetComponent<TextMeshProUGUI>();
            _attackBtnText.text = "<size=18><b>BATALHAR</b></size>\n<size=11><color=#ffaaaa>entrar na arena</color></size>";
            _attackBtnText.fontStyle = FontStyles.Bold;
            _attackBtnText.color = Color.white;
            _attackBtnText.alignment = TextAlignmentOptions.Center;
            _attackBtnText.textWrappingMode = TextWrappingModes.Normal;
            _attackBtnText.raycastTarget = false;
            var lf = GetCachedFont(); if (lf != null) _attackBtnText.font = lf;

            batGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                CloseAllModals();
                _battlePanel?.Open();
            });

            // CombatManager reward toast (fires regardless of panel open state)
            cm.OnMonsterDied += reward =>
                ShowToast($"+${NumberFormatter.Format(reward)} recompensa!", new Color(0.25f, 0.9f, 0.35f, 1f));
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

            // Ranking button — na barra de nav superior-esquerda, depois de STATS
            var btnGO = new GameObject("RankingButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(canvas.transform, false);
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(552f, -10f);
            brt.sizeDelta = new Vector2(80f, 50f);
            var bImg2 = btnGO.GetComponent<Image>();
            bImg2.sprite = Rounded(); bImg2.type = Image.Type.Sliced;
            bImg2.color = new Color(0.08f, 0.13f, 0.22f, 0.9f);
            btnGO.GetComponent<Button>().onClick.AddListener(OpenRanking);

            var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(btnGO.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var ltmp = lblGO.GetComponent<TextMeshProUGUI>();
            ltmp.text = "RANK";
            ltmp.fontSize = 16;
            ltmp.fontStyle = FontStyles.Bold;
            ltmp.color = new Color(0.6f, 0.7f, 0.85f, 0.85f);
            ltmp.alignment = TextAlignmentOptions.Center;
            ltmp.raycastTarget = false;
            var ff = GetCachedFont();
            if (ff != null) ltmp.font = ff;
            StyleNavTab(btnGO, new Color(0.6f, 0.7f, 0.85f, 0.85f));
        }

        private void OpenRanking()
        {
            if (rankingPanel != null) { CloseAllModals(); rankingPanel.Open(); }
        }

        private void SetupStatsPanel()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            var panelGO = new GameObject("StatsPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvas.transform, false);
            statsPanel = panelGO.AddComponent<StatsPanel>();
            modalPanels.Add(panelGO);

            // STATS button — na barra de nav superior-esquerda, depois de BONUS
            var btnGO = new GameObject("StatsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(canvas.transform, false);
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(464f, -10f);
            brt.sizeDelta = new Vector2(80f, 50f);
            var bImg = btnGO.GetComponent<Image>();
            bImg.sprite = Rounded(); bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.08f, 0.13f, 0.22f, 0.9f);
            btnGO.GetComponent<Button>().onClick.AddListener(() =>
            { if (statsPanel != null) { CloseAllModals(); statsPanel.Open(); } });
            var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(btnGO.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var ltmp = lblGO.GetComponent<TextMeshProUGUI>();
            ltmp.text = "STATS"; ltmp.fontSize = 16; ltmp.fontStyle = FontStyles.Bold;
            ltmp.color = new Color(0.6f, 0.7f, 0.85f, 0.85f);
            ltmp.alignment = TextAlignmentOptions.Center; ltmp.raycastTarget = false;
            var ff2 = GetCachedFont(); if (ff2 != null) ltmp.font = ff2;
            StyleNavTab(btnGO, new Color(0.6f, 0.7f, 0.85f, 0.85f));
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
                if (n == "PanelBG" || n == "BgFloor" || n == "BgFurniture" || n == "BgPlants" || n == "BgWindows"
                    || n == "TapButton" || n == "TapValue" || n == "TapValuePill"
                    || n == "MonsterView" || n == "AttackButton" || n == "BattleButton" || n == "CombatUpgradeButton")
                    DestroyImmediate(pmGO.transform.GetChild(i).gameObject);
            }

            // ── Layered background ───────────────────────────────────────────
            Image SpawnBgLayer(string resPath, string goName, bool animated = false)
            {
                var tex = Resources.Load<Texture2D>(resPath);
                var layerGO = new GameObject(goName, typeof(RectTransform), typeof(Image));
                layerGO.transform.SetParent(pmGO.transform, false);
                var lrt = layerGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                var img = layerGO.GetComponent<Image>();
                img.raycastTarget = false;
                if (tex != null)
                {
                    img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    img.type = Image.Type.Simple;
                    img.preserveAspect = false;
                    img.color = Color.white;
                }
                else
                {
                    img.color = new Color(0, 0, 0, 0); // transparent if missing
                }
                return img;
            }

            var floorImg     = SpawnBgLayer("Backgrounds/bg_floor",     "BgFloor");
            var furnitureImg = SpawnBgLayer("Backgrounds/bg_furniture",  "BgFurniture");
            var plantsImg    = SpawnBgLayer("Backgrounds/bg_plants",     "BgPlants");
            var windowsImg   = SpawnBgLayer("Backgrounds/bg_windows",    "BgWindows");

            // Base: bg_floor from Backgrounds/ if exists, else UI/office_bg
            Destroy(furnitureImg.gameObject);
            Destroy(plantsImg.gameObject);
            Destroy(windowsImg.gameObject);

            if (floorImg.sprite != null)
            {
                floorImg.color = Color.white;
                floorImg.type  = Image.Type.Simple;
                floorImg.preserveAspect = true;
            }
            else
            {
                var officeTex = Resources.Load<Texture2D>("UI/office_bg");
                if (officeTex != null)
                {
                    floorImg.sprite = Sprite.Create(officeTex, new Rect(0,0,officeTex.width,officeTex.height), new Vector2(0.5f,0.5f));
                    floorImg.color  = Color.white;
                    floorImg.type   = Image.Type.Simple;
                    floorImg.preserveAspect = true;
                }
                else floorImg.color = NavyDark;
            }
            floorImg.transform.SetAsFirstSibling();

            // Estende o escritorio para tras de TODA a interface (sidebar + topo),
            // movendo-o para o Panel_BG em tela cheia. Assim os paineis translucidos
            // (sidebar, barra superior, pills) revelam o cenario por tras.
            var bgPanel = GameObject.Find("Panel_BG");
            if (bgPanel != null && floorImg != null)
            {
                // limpa um floor antigo deixado no Panel_BG em replays do editor
                var stale = bgPanel.transform.Find("BgFloor");
                if (stale != null && stale != floorImg.transform)
                    DestroyImmediate(stale.gameObject);

                floorImg.transform.SetParent(bgPanel.transform, false);
                var frt = floorImg.rectTransform;
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
                // Estica o escritorio para a esquerda (ate proximo da sidebar, com uma
                // pequena folga navy) e o amplia: aumentando a altura via preserveAspect
                // a imagem cresce em largura e preenche as laterais, eliminando o vao
                // navy da esquerda. Pequeno deslocamento à direita para a folga.
                frt.offsetMin = new Vector2(150f, -150f); // esquerda com folga, base estendida
                frt.offsetMax = new Vector2(-200f, 150f); // recua a direita: libera navy p/ a coluna de stats
                floorImg.transform.SetAsLastSibling(); // cobre o tom escuro do Panel_BG

                // Fundo das laterais: gradiente navy combinando com o tema. O
                // escritorio nitido ajusta pela altura e sobravam barras pretas nas
                // laterais; este layer atras preenche tudo com um degrade suave
                // (escuro embaixo -> navy mais claro em cima) integrando o cenario.
                {
                    var staleCover = bgPanel.transform.Find("BgCover");
                    if (staleCover != null) DestroyImmediate(staleCover.gameObject);

                    var coverGO = new GameObject("BgCover", typeof(RectTransform), typeof(Image));
                    coverGO.transform.SetParent(bgPanel.transform, false);
                    var crt = coverGO.GetComponent<RectTransform>();
                    crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
                    crt.offsetMin = crt.offsetMax = Vector2.zero;
                    var coverImg = coverGO.GetComponent<Image>();
                    coverImg.sprite = UiSpriteFactory.VerticalGradient();
                    coverImg.type = Image.Type.Simple;
                    coverImg.preserveAspect = false;
                    coverImg.color = new Color(0.11f, 0.17f, 0.30f, 1f); // navy do tema
                    coverImg.raycastTarget = false;
                    coverGO.transform.SetAsFirstSibling(); // atras do escritorio nitido
                }

                // Panel_Main fica transparente para revelar o escritorio atras dos
                // workers / tap button / pills.
                var pmImg = pmGO.GetComponent<Image>();
                if (pmImg != null) pmImg.color = new Color(0f, 0f, 0f, 0f);
            }

            // ── Monitor glows removidos ───────────────────────────────────────

            // Botão principal circular — camadas empilhadas: ring → glow → borda → face → sheen → label
            var tapGO = new GameObject("TapButton", typeof(RectTransform), typeof(Button));
            tapGO.transform.SetParent(pmGO.transform, false);
            tapButtonRT = tapGO.GetComponent<RectTransform>();
            tapButtonRT.anchorMin = tapButtonRT.anchorMax = tapButtonRT.pivot = new Vector2(1f, 0f);
            tapButtonRT.anchoredPosition = new Vector2(-30f, 30f);
            tapButtonRT.sizeDelta = new Vector2(220f, 220f);
            var tapBtn = tapGO.GetComponent<Button>();

            Image AddCircle(string name, Vector2 offMin, Vector2 offMax, Color col, bool ray)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(tapGO.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = offMin; rt.offsetMax = offMax;
                var im = go.GetComponent<Image>();
                im.sprite = Circle(); im.type = Image.Type.Simple;
                im.color = col; im.raycastTarget = ray;
                return im;
            }

            // Halo exterior pulsante — glow radial suave, bem maior que o botão
            _tapRingImg = AddCircle("Ring", new Vector2(-56f, -56f), new Vector2(56f, 56f),
                new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.22f), false);
            _tapRingImg.sprite = Glow();

            // Glow difuso interno (halo radial mais concentrado)
            tapGlowImg = AddCircle("Glow", new Vector2(-28f, -28f), new Vector2(28f, 28f),
                new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.38f), false);
            tapGlowImg.sprite = Glow();

            // Borda escura (circle ligeiramente maior que face)
            AddCircle("Border", new Vector2(-2f, -2f), new Vector2(2f, 2f),
                new Color(0.10f, 0.38f, 0.18f, 1f), false);

            // Face principal — verde chapado e limpo (mesma pegada do botao "Coletar")
            tapFaceImg = AddCircle("Face", Vector2.zero, Vector2.zero, GreenBtn, true);
            tapBtn.targetGraphic = tapFaceImg;

            // Label: ícone + texto
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(tapGO.transform, false);
            var lRT = labelGO.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = new Vector2(8f, 8f); lRT.offsetMax = new Vector2(-8f, -8f);
            var lTMP = labelGO.GetComponent<TextMeshProUGUI>();
            lTMP.text = "<size=28><b>TRABALHAR</b></size>\n<size=13><color=#a8ddb0>segure para auto</color></size>";
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.color = Color.white;
            lTMP.alignment = TextAlignmentOptions.Center;
            lTMP.textWrappingMode = TextWrappingModes.Normal;
            lTMP.raycastTarget = false;
            var lf = GetCachedFont(); if (lf != null) lTMP.font = lf;

            // Pílula dourada com o valor por clique, logo abaixo do botão
            var pillGO = new GameObject("TapValuePill", typeof(RectTransform), typeof(Image));
            pillGO.transform.SetParent(pmGO.transform, false);
            var pillRT = pillGO.GetComponent<RectTransform>();
            pillRT.anchorMin = pillRT.anchorMax = pillRT.pivot = new Vector2(1f, 0f);
            pillRT.anchoredPosition = new Vector2(-30f, 265f);
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
            tapValueText.fontSize = 22;
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
            brt2.anchorMin = brt2.anchorMax = brt2.pivot = new Vector2(1f, 0f);
            brt2.anchoredPosition = new Vector2(-30f, 258f);
            brt2.sizeDelta = new Vector2(178f, 46f);
            _boostBtnImg = boostGO.GetComponent<Image>();
            _boostBtnImg.sprite = Rounded(); _boostBtnImg.type = Image.Type.Sliced;
            _boostBtnImg.color = BoostReady;
            boostGO.GetComponent<Button>().onClick.AddListener(OnTurboClicked);
            // Sheen layer
            var bSheen = new GameObject("Sheen", typeof(RectTransform), typeof(Image));
            bSheen.transform.SetParent(boostGO.transform, false);
            var bSrt = bSheen.GetComponent<RectTransform>();
            bSrt.anchorMin = new Vector2(0f, 0.5f); bSrt.anchorMax = Vector2.one;
            bSrt.offsetMin = new Vector2(4f, 0f); bSrt.offsetMax = new Vector2(-4f, -3f);
            var bSImg = bSheen.GetComponent<Image>();
            bSImg.sprite = Rounded(); bSImg.type = Image.Type.Sliced;
            bSImg.color = new Color(1f, 1f, 1f, 0.10f); bSImg.raycastTarget = false;

            var btLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btLabel.transform.SetParent(boostGO.transform, false);
            var btlRT = btLabel.GetComponent<RectTransform>();
            btlRT.anchorMin = Vector2.zero; btlRT.anchorMax = Vector2.one;
            btlRT.offsetMin = btlRT.offsetMax = Vector2.zero;
            _boostBtnText = btLabel.GetComponent<TextMeshProUGUI>();
            _boostBtnText.text = "TURBO  x5  30s";
            _boostBtnText.fontSize = 14; _boostBtnText.fontStyle = FontStyles.Bold;
            _boostBtnText.alignment = TextAlignmentOptions.Center;
            _boostBtnText.color = Color.white; _boostBtnText.raycastTarget = false;
            var btf = GetCachedFont(); if (btf != null) _boostBtnText.font = btf;

            // ── Office Props Layer (mesas, props animados) ────────────────
            var propsGO = new GameObject("OfficePropsLayer", typeof(OfficePropsLayer));
            propsGO.transform.SetParent(pmGO.transform, false);
            propsGO.GetComponent<OfficePropsLayer>().Init(panelMain);

            // ── Office Worker Manager ──────────────────────────────────────
            var workerGO = new GameObject("OfficeWorkerManager", typeof(OfficeWorkerManager));
            workerGO.transform.SetParent(pmGO.transform, false);
            _workerManager = workerGO.GetComponent<OfficeWorkerManager>();
            _workerManager.Init(panelMain);

            // ── Combat setup ──────────────────────────────────────────────
            SetupCombat(pmGO);
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
                rt.sizeDelta = new Vector2(260f, 60f);
                rt.anchoredPosition = new Vector2(Random.Range(-80f, 80f), Random.Range(30f, 100f));
                // Turbo tap = gold, normal = green
                Color tapColor = GameManager.Instance.TapBoostActive
                    ? new Color(1f, 0.85f, 0.1f) : NeonGreen;
                go.GetComponent<FloatingText>().Init($"+${NumberFormatter.Format(val)}", tapColor, 32f);

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
            var c1 = GreenBtn;
            var c2 = new Color(0.33f, 0.88f, 0.46f, 1f);
            var g1 = new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.22f);
            var g2 = new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.48f);
            var r1 = new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.12f);
            var r2 = new Color(GreenBtn.r, GreenBtn.g, GreenBtn.b, 0.32f);
            var rs1 = Vector3.one;
            var rs2 = Vector3.one * 1.06f;
            while (tapFaceImg != null)
            {
                float e = 0f; const float dur = 1.1f;
                while (e < dur)
                {
                    e += Time.deltaTime; float t = Mathf.SmoothStep(0f, 1f, e / dur);
                    tapFaceImg.color = Color.Lerp(c1, c2, t);
                    if (tapGlowImg  != null) tapGlowImg.color  = Color.Lerp(g1, g2, t);
                    if (_tapRingImg != null)
                    {
                        _tapRingImg.color = Color.Lerp(r1, r2, t);
                        _tapRingImg.rectTransform.localScale = Vector3.Lerp(rs1, rs2, t);
                    }
                    yield return null;
                }
                e = 0f;
                while (e < dur)
                {
                    e += Time.deltaTime; float t = Mathf.SmoothStep(0f, 1f, e / dur);
                    tapFaceImg.color = Color.Lerp(c2, c1, t);
                    if (tapGlowImg  != null) tapGlowImg.color  = Color.Lerp(g2, g1, t);
                    if (_tapRingImg != null)
                    {
                        _tapRingImg.color = Color.Lerp(r2, r1, t);
                        _tapRingImg.rectTransform.localScale = Vector3.Lerp(rs2, rs1, t);
                    }
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

            // Navy background for the sidebar — same glass hue as the welcome panel
            var panelImg = panelLeft.GetComponent<Image>();
            if (panelImg != null) panelImg.color = new Color(0.055f, 0.094f, 0.165f, 0.82f);

            // Sombra na borda direita — separa a sidebar da arte do escritório
            if (panelLeft.transform.Find("RightShadow") == null)
            {
                var sh = new GameObject("RightShadow", typeof(RectTransform), typeof(Image));
                sh.transform.SetParent(panelLeft.transform, false);
                var shrt = sh.GetComponent<RectTransform>();
                shrt.anchorMin = new Vector2(1f, 0f); shrt.anchorMax = new Vector2(1f, 1f);
                shrt.pivot = new Vector2(1f, 0.5f);
                shrt.sizeDelta = new Vector2(4f, 0f);
                var shImg = sh.GetComponent<Image>();
                shImg.color = new Color(0f, 0f, 0f, 0.28f);
                shImg.raycastTarget = false;
            }

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
            // O proprio Panel_TopBar tem fundo opaco — torna translucido para o
            // escritorio aparecer por tras (mesma pegada do painel Bem-vindo).
            var topBarImg = topBar.GetComponent<Image>();
            if (topBarImg != null)
                topBarImg.color = new Color(0.055f, 0.094f, 0.165f, 0.82f); // glass navy
        }

        // Top-right gem pill + coin icon next to money. Gems are a new currency
        // earned from prestige, so the whole display is built at runtime.
        private void SetupTopBar()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            // Estiliza textos do top bar (campos do scene)
            if (moneyText != null)
            {
                // Reanchor to upper 60% so the /s subtitle fits below
                var mrt2 = moneyText.rectTransform;
                mrt2.anchorMin = new Vector2(0.2f, 0.38f);
                mrt2.anchorMax = new Vector2(0.8f, 1f);
                mrt2.offsetMin = new Vector2(0f, 4f); mrt2.offsetMax = Vector2.zero;
                moneyText.fontSize  = 26; moneyText.fontStyle = FontStyles.Bold;
                moneyText.color     = Color.white; moneyText.alignment = TextAlignmentOptions.Center;
                var mf = GetCachedFont(); if (mf != null) moneyText.font = mf;
            }
            if (mpsText != null)
            {
                // Reanchor to sit as a subtitle below the money amount (center, lower half of nav bar)
                var mrt = mpsText.rectTransform;
                mrt.anchorMin = new Vector2(0.2f, 0f);
                mrt.anchorMax = new Vector2(0.8f, 0.38f);
                mrt.offsetMin = Vector2.zero; mrt.offsetMax = new Vector2(0f, -4f);
                mpsText.fontSize  = 19; mpsText.fontStyle = FontStyles.Bold;
                mpsText.color     = new Color(0.65f, 0.82f, 0.95f, 0.72f);
                mpsText.alignment = TextAlignmentOptions.Center;
                var mf2 = GetCachedFont(); if (mf2 != null) mpsText.font = mf2;
            }
            if (prestigeInfoText != null) prestigeInfoText.gameObject.SetActive(false);

            // Gem pill — na barra de nav superior-esquerda, depois de RANK — abre a loja
            var pillGO = new GameObject("GemPill", typeof(RectTransform), typeof(Image), typeof(Button));
            pillGO.transform.SetParent(canvas.transform, false);
            var prt = pillGO.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 1f);
            prt.anchoredPosition = new Vector2(640f, -10f);
            prt.sizeDelta = new Vector2(110f, 50f);
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
            gemText.fontSize = 17; gemText.fontStyle = FontStyles.Bold;
            gemText.color = new Color(0.7f, 0.93f, 1f, 1f);
            gemText.alignment = TextAlignmentOptions.MidlineLeft;
            gemText.raycastTarget = false;
            var gf = GetCachedFont(); if (gf != null) gemText.font = gf;

            RefreshGemDisplay();
        }

        private void SetupTopSeparator()
        {
            var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null) return;

            // Faixa escura logo abaixo dos botões de nav (separa HUD do campo de jogo)
            var sepGO = new GameObject("TopSeparator", typeof(RectTransform), typeof(Image));
            sepGO.transform.SetParent(canvas.transform, false);
            var rt = sepGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -54f);
            rt.sizeDelta = new Vector2(0f, 3f);
            var img = sepGO.GetComponent<Image>();
            img.color = new Color(BlueAccent.r, BlueAccent.g, BlueAccent.b, 0.55f);
            img.raycastTarget = false;
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
            himg.sprite = Rounded(); himg.type = Image.Type.Sliced;
            himg.color = new Color(0.055f, 0.094f, 0.165f, 0.82f); // glass navy header
            himg.raycastTarget = false;

            var labelGO = new GameObject("EquipeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(headerGO.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(14f, 0f); lrt.offsetMax = Vector2.zero;
            var ltmp = labelGO.GetComponent<TextMeshProUGUI>();
            ltmp.text = "FUNCIONARIOS"; ltmp.fontSize = 16; ltmp.fontStyle = FontStyles.Bold;
            ltmp.color = GoldColor; ltmp.alignment = TextAlignmentOptions.MidlineLeft;
            ltmp.raycastTarget = false;
            var lf = GetCachedFont(); if (lf != null) ltmp.font = lf;
        }

        // Dedicated bar (below the FUNCIONÁRIOS header) holding the x1/x10/Máx
        // toggle. Buttons are left-anchored so they stay in the visible column
        // — the right edge of Panel_Left is hidden behind the office artwork.

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
                var pillGO = new GameObject("Pill", typeof(RectTransform), typeof(Image), typeof(Button));
                pillGO.transform.SetParent(effectsHUD.transform, false);
                pillGO.GetComponent<RectTransform>().sizeDelta = new Vector2(115, 22);
                var pImg = pillGO.GetComponent<Image>();
                pImg.color = new Color(pc.r, pc.g, pc.b, 0.65f);
                pillGO.GetComponent<Button>().onClick.AddListener(() => { CloseAllModals(); activeEffectsPanel?.Open(); });
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

            // Track background
            var trackGO = new GameObject("PrestigeBarTrack", typeof(RectTransform), typeof(Image));
            trackGO.transform.SetParent(prestigeButton.transform, false);
            var trt = trackGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 0f);
            trt.offsetMin = new Vector2(0f, 0f); trt.offsetMax = new Vector2(0f, 5f);
            trackGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            trackGO.GetComponent<Image>().raycastTarget = false;

            var barGO = new GameObject("PrestigeBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(prestigeButton.transform, false);
            var rt = barGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.offsetMin = new Vector2(0f, 0f); rt.offsetMax = new Vector2(0f, 5f);
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
            img.color = new Color(0.055f, 0.094f, 0.165f, 0.82f); // glass navy, welcome-panel hue
            prestigeButton.targetGraphic = img;

            var rt = prestigeButton.GetComponent<RectTransform>();
            float anchorX0 = rt.anchorMin.x;
            float anchorX1 = rt.anchorMax.x;
            rt.anchorMin        = new Vector2(anchorX0, 0f);
            rt.anchorMax        = new Vector2(anchorX1, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(0f, 78f);
            rt.anchoredPosition = new Vector2(0f, 8f);

            // Sheen (topo do botão, visível quando pronto)
            var sheenGO = new GameObject("Sheen", typeof(RectTransform), typeof(Image));
            sheenGO.transform.SetParent(prestigeButton.transform, false);
            var srt = sheenGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.45f); srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(4f, 0f); srt.offsetMax = new Vector2(-4f, -4f);
            _prestigeSheen = sheenGO.GetComponent<Image>();
            _prestigeSheen.sprite = Rounded(); _prestigeSheen.type = Image.Type.Sliced;
            _prestigeSheen.color = new Color(1f, 1f, 1f, 0.07f);
            _prestigeSheen.raycastTarget = false;

            // Glow dourado atrás da estrela (pulsa quando o prestígio está pronto)
            var starGlowGO = new GameObject("StarGlow", typeof(RectTransform), typeof(Image));
            starGlowGO.transform.SetParent(prestigeButton.transform, false);
            var sgrt = starGlowGO.GetComponent<RectTransform>();
            sgrt.anchorMin = sgrt.anchorMax = sgrt.pivot = new Vector2(0f, 0.5f);
            sgrt.anchoredPosition = new Vector2(38f, 0f);
            sgrt.sizeDelta = new Vector2(78f, 78f);
            _prestigeStarGlow = starGlowGO.GetComponent<Image>();
            _prestigeStarGlow.sprite = Glow();
            _prestigeStarGlow.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0f);
            _prestigeStarGlow.raycastTarget = false;

            // Ícone estrela à esquerda
            var starGO = new GameObject("StarIcon", typeof(RectTransform), typeof(Image));
            starGO.transform.SetParent(prestigeButton.transform, false);
            _prestigeStarRt = starGO.GetComponent<RectTransform>();
            var strt = _prestigeStarRt;
            strt.anchorMin = strt.anchorMax = strt.pivot = new Vector2(0f, 0.5f);
            strt.anchoredPosition = new Vector2(20f, 0f);
            strt.sizeDelta = new Vector2(36f, 36f);
            var stImg = starGO.GetComponent<Image>();
            stImg.sprite = UiSpriteFactory.Star(); stImg.color = GoldColor;
            stImg.raycastTarget = false;

            StartCoroutine(PulsePrestigeReady());

            var labelGO = prestigeButton.transform.Find("PrestigeButtonLabel");
            TextMeshProUGUI label = labelGO != null
                ? (labelGO.GetComponent<TextMeshProUGUI>() ?? labelGO.gameObject.AddComponent<TextMeshProUGUI>())
                : null;
            if (label == null)
            {
                var go = new GameObject("PrestigeButtonLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(prestigeButton.transform, false);
                var lrt = go.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.05f, 0f); lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                label = go.GetComponent<TextMeshProUGUI>();
            }
            label.fontSize  = 18;
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
            int gems        = GameManager.Instance.GetPrestigeGemReward();
            // Always show gem reward — when not ready, preview the fixed reward for this tier
            int previewGems = ready ? gems : Mathf.Max(1,
                (int)System.Math.Floor(5.0 * System.Math.Sqrt(
                    GameManager.Instance.GetPrestigeRequirement() /
                    GameManager.Instance.GetPrestigeBaseRequirement()) *
                    GemShop.GetGemBonus()));

            prestigeButtonLabel.text = ready
                ? $">> PRESTIGIAR  |  x{nextMult:F1}  +{gems} GEMAS <<"
                : $"PRESTIGIO #{count + 1}  |  x{nextMult:F1}  +{previewGems} GEMAS  |  meta ${NumberFormatter.Format(GameManager.Instance.GetPrestigeRequirement())}";

            var img = prestigeButton?.GetComponent<Image>();
            if (img != null)
                img.color = ready
                    ? new Color(0.55f, 0.35f, 0.02f, 0.88f)   // glass amber when ready
                    : new Color(0.055f, 0.094f, 0.165f, 0.82f); // glass navy when inactive

            if (_prestigeSheen != null)
                _prestigeSheen.color = ready
                    ? new Color(1f, 0.88f, 0.3f, 0.22f)
                    : new Color(0.5f, 0.75f, 1f, 0.07f);   // subtle blue sheen when inactive

            prestigeButtonLabel.color = ready ? new Color(1f, 0.95f, 0.70f) : Color.white;
            prestigeButtonLabel.fontSize = 17f;
        }

        // Quando o prestígio está disponível, a estrela respira (glow + escala)
        // para chamar atenção; em repouso fica estática e discreta.
        private IEnumerator PulsePrestigeReady()
        {
            float phase = 0f;
            while (true)
            {
                bool ready = GameManager.Instance != null && GameManager.Instance.CanPrestige();
                if (ready)
                {
                    phase += Time.deltaTime * 2.2f;
                    float t = (Mathf.Sin(phase) + 1f) * 0.5f; // 0..1
                    if (_prestigeStarGlow != null)
                        _prestigeStarGlow.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b,
                            Mathf.Lerp(0.15f, 0.55f, t));
                    if (_prestigeStarRt != null)
                        _prestigeStarRt.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, t);
                }
                else
                {
                    phase = 0f;
                    if (_prestigeStarGlow != null)
                        _prestigeStarGlow.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0f);
                    if (_prestigeStarRt != null)
                        _prestigeStarRt.localScale = Vector3.one;
                }
                yield return null;
            }
        }

        // ── Background Animations ─────────────────────────────────────────────

        private IEnumerator AnimatePlants(RectTransform rt)
        {
            float phase = Random.Range(0f, Mathf.PI * 2f);
            rt.pivot = new Vector2(0.5f, 0f); // sway from bottom
            while (true)
            {
                phase += Time.deltaTime * 0.6f;
                float sway = Mathf.Sin(phase) * 0.4f + Mathf.Sin(phase * 1.7f) * 0.2f;
                rt.localRotation = Quaternion.Euler(0f, 0f, sway);
                yield return null;
            }
        }

        private IEnumerator AnimateWindows(Image img)
        {
            float phase = 0f;
            while (true)
            {
                phase += Time.deltaTime * 0.3f;
                float pulse = 0.88f + Mathf.Sin(phase) * 0.06f + Mathf.Sin(phase * 2.3f) * 0.03f;
                img.color = new Color(pulse, pulse, pulse, 1f);
                yield return null;
            }
        }

        // ── Ambient light pulse (simulates clouds passing) ────────────────────
        private IEnumerator AmbientLightPulse(Transform parent)
        {
            var go = new GameObject("AmbientLight", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 0.97f, 0.88f, 0f);
            img.raycastTarget = false;

            // Place just above the floor, below characters
            go.transform.SetSiblingIndex(1);

            float phase = Random.Range(0f, Mathf.PI * 2f);
            while (true)
            {
                phase += Time.deltaTime * 0.08f;
                float alpha = (Mathf.Sin(phase) * 0.5f + 0.5f) * 0.18f
                            + (Mathf.Sin(phase * 2.7f) * 0.5f + 0.5f) * 0.08f;
                img.color = new Color(1f, 0.97f, 0.88f, alpha);
                yield return null;
            }
        }

        // ── Monitor glows (simulates screen light) ────────────────────────────
        private void SpawnMonitorGlows(Transform parent)
        {
            // Approximate monitor positions in Panel_Main local space
            var monitorPositions = new Vector2[]
            {
                // Mesas da esquerda (fileira de cima)
                new(-460f, 20f), new(-340f, 50f), new(-220f, 75f),
                // Mesas da esquerda (fileira de baixo)
                new(-420f, -60f), new(-300f, -30f),
                // Mesas da direita
                new( 320f, -30f), new( 440f, -55f), new( 540f, -75f),
            };

            var colors = new Color[]
            {
                new(0.4f, 0.7f, 1.0f, 1f), // blue screen
                new(0.3f, 0.9f, 0.5f, 1f), // green terminal
                new(0.8f, 0.6f, 1.0f, 1f), // purple IDE
                new(0.4f, 0.8f, 1.0f, 1f),
                new(0.5f, 1.0f, 0.7f, 1f),
                new(0.9f, 0.7f, 0.4f, 1f), // orange
                new(0.4f, 0.7f, 1.0f, 1f),
                new(0.6f, 0.9f, 1.0f, 1f),
            };

            for (int i = 0; i < monitorPositions.Length; i++)
            {
                var go = new GameObject("MonitorGlow", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = monitorPositions[i];
                rt.sizeDelta = new Vector2(50f, 32f);
                var img = go.GetComponent<Image>();
                img.sprite = UiSpriteFactory.RoundedBox();
                img.type = Image.Type.Sliced;
                img.color = new Color(colors[i].r, colors[i].g, colors[i].b, 0.7f);
                img.raycastTarget = false;
                go.transform.SetSiblingIndex(1);
                StartCoroutine(AnimateMonitor(img, colors[i]));
            }
        }

        private IEnumerator AnimateMonitor(Image img, Color baseColor)
        {
            float phase = Random.Range(0f, Mathf.PI * 2f);
            float blinkTimer = Random.Range(3f, 8f);
            while (true)
            {
                phase += Time.deltaTime * 0.4f;
                blinkTimer -= Time.deltaTime;

                float alpha = 0.6f + Mathf.Sin(phase) * 0.2f;

                // Occasional blink (simulates screen refresh)
                if (blinkTimer <= 0f)
                {
                    img.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
                    yield return new WaitForSeconds(0.12f);
                    img.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.05f);
                    yield return new WaitForSeconds(0.07f);
                    img.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f);
                    yield return new WaitForSeconds(0.1f);
                    blinkTimer = Random.Range(2f, 6f);
                }

                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }

        // ── Dust particles ────────────────────────────────────────────────────
        private IEnumerator SpawnDustParticles(Transform parent)
        {
            yield return new WaitForSeconds(2f);
            while (true)
            {
                SpawnOneDust(parent);
                yield return new WaitForSeconds(Random.Range(0.3f, 1.0f));
            }
        }

        private void SpawnOneDust(Transform parent)
        {
            var go = new GameObject("Dust", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            float size = Random.Range(3f, 8f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(
                Random.Range(-500f, 500f),
                Random.Range(-180f, 80f));
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.Circle();
            img.color = new Color(1f, 0.95f, 0.8f, 0f);
            img.raycastTarget = false;
            go.transform.SetSiblingIndex(2);
            StartCoroutine(AnimateDust(rt, img));
        }

        private IEnumerator AnimateDust(RectTransform rt, Image img)
        {
            float lifetime = Random.Range(4f, 9f);
            float elapsed  = 0f;
            Vector2 startPos = rt.anchoredPosition;
            Vector2 drift = new Vector2(Random.Range(-15f, 15f), Random.Range(20f, 50f));
            float maxAlpha = Random.Range(0.25f, 0.55f);

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;
                // Fade in then out
                float alpha = t < 0.2f ? (t / 0.2f) * maxAlpha
                            : t > 0.8f ? ((1f - t) / 0.2f) * maxAlpha
                            : maxAlpha;
                img.color = new Color(1f, 0.95f, 0.8f, alpha);
                rt.anchoredPosition = startPos + drift * t;
                // Gentle wobble
                rt.anchoredPosition += new Vector2(Mathf.Sin(elapsed * 2.1f) * 3f, 0f);
                yield return null;
            }
            Destroy(rt.gameObject);
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
            rt.sizeDelta = new Vector2(220f, 50f);
            double amount = GameManager.Instance.MoneyPerSecond * FloatBurstInterval;
            go.GetComponent<FloatingText>().Init($"+${NumberFormatter.Format(amount)}", NeonGreen, 26f);
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

            var stale = panelMain.Find("StatsCard");
            if (stale != null) DestroyImmediate(stale.gameObject);

            TMP_FontAsset font = GetCachedFont();

            // 4 pílulas EMPILHADAS NA VERTICAL, ancoradas no canto inferior-esquerdo
            // do Panel_Main, logo acima do botão BATALHAR (que fica em y 30..190).
            const float pillW = 212f, pillH = 62f, gap = 7f;

            // Unified navy background across all pills — only the accent bar and
            // label carry color, which reads far more polished than mixed tints.
            var pillBg = new Color(0.075f, 0.122f, 0.200f, 0.96f);
            var pillData = new (string label, Color labelColor, Color bgColor)[]
            {
                ("POR SEGUNDO",   NeonCyan,                          pillBg),
                ("MULTIPLICADOR", new Color(1f, 0.85f, 0.32f, 1f),   pillBg),
                ("TOTAL GANHO",   new Color(0.55f, 0.78f, 1f, 1f),   pillBg),
                ("PRESTIGIO",     NeonOrange,                        pillBg),
            };

            var textRefs = new TextMeshProUGUI[4];

            // Container único (barra glass) atrás das 4 pílulas, agora em coluna.
            // As pílulas ficam transparentes sobre esta barra contínua, separadas
            // por divisórias horizontais sutis.
            const float barPad = 8f;
            float totalH = 4f * pillH + 3f * gap;
            var barGO = new GameObject("StatBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(panelMain, false);
            var barRT = barGO.GetComponent<RectTransform>();
            barRT.anchorMin = barRT.anchorMax = barRT.pivot = new Vector2(1f, 1f);
            barRT.anchoredPosition = new Vector2(-18f, -8f); // canto superior-direito do Panel_Main
            barRT.sizeDelta = new Vector2(pillW + barPad * 2f, totalH + barPad * 2f);
            var barImg = barGO.GetComponent<Image>();
            barImg.sprite = Rounded(); barImg.type = Image.Type.Sliced;
            barImg.color = new Color(0.055f, 0.094f, 0.165f, 0.92f); // glass navy contínuo
            barImg.raycastTarget = false;

            for (int i = 0; i < 4; i++)
            {
                var d = pillData[i];
                float y = i * (pillH + gap); // empilha de cima para baixo

                var pill = new GameObject($"StatPill{i}", typeof(RectTransform), typeof(Image));
                pill.transform.SetParent(barGO.transform, false);
                var prt = pill.GetComponent<RectTransform>();
                prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 1f);
                prt.anchoredPosition = new Vector2(barPad, -barPad - y);
                prt.sizeDelta = new Vector2(pillW, pillH);
                var pImg = pill.GetComponent<Image>();
                pImg.color = new Color(0f, 0f, 0f, 0f); // transparente: usa a barra contínua atrás
                pImg.raycastTarget = false;

                // Divisória horizontal sutil entre as pílulas (exceto antes da 1ª)
                if (i > 0)
                {
                    var div = new GameObject("Div", typeof(RectTransform), typeof(Image));
                    div.transform.SetParent(barGO.transform, false);
                    var drt = div.GetComponent<RectTransform>();
                    drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(0f, 1f);
                    drt.anchoredPosition = new Vector2(barPad + pillW * 0.05f, -barPad - y + gap * 0.5f);
                    drt.sizeDelta = new Vector2(pillW * 0.9f, 1f);
                    var divImg = div.GetComponent<Image>();
                    divImg.color = new Color(1f, 1f, 1f, 0.08f);
                    divImg.raycastTarget = false;
                }

                // Acento colorido lateral — pílula arredondada inset (não flush na borda)
                var acc = new GameObject("A", typeof(RectTransform), typeof(Image));
                acc.transform.SetParent(pill.transform, false);
                var art = acc.GetComponent<RectTransform>();
                art.anchorMin = new Vector2(0f, 0.18f); art.anchorMax = new Vector2(0f, 0.82f);
                art.pivot = new Vector2(0f, 0.5f);
                art.sizeDelta = new Vector2(5f, 0f);
                art.anchoredPosition = new Vector2(8f, 0f);
                var accImg = acc.GetComponent<Image>();
                accImg.sprite = Rounded(); accImg.type = Image.Type.Sliced;
                accImg.color = d.labelColor; accImg.raycastTarget = false;

                // Valor (linha de cima, grande)
                var valGO = new GameObject("V", typeof(RectTransform), typeof(TextMeshProUGUI));
                valGO.transform.SetParent(pill.transform, false);
                var vrt = valGO.GetComponent<RectTransform>();
                vrt.anchorMin = new Vector2(0f, 0.40f); vrt.anchorMax = Vector2.one;
                vrt.offsetMin = new Vector2(16f, 2f); vrt.offsetMax = new Vector2(-6f, -4f);
                var vtmp = valGO.GetComponent<TextMeshProUGUI>();
                vtmp.fontSize = 22f; vtmp.fontStyle = FontStyles.Bold; vtmp.color = Color.white;
                vtmp.alignment = TextAlignmentOptions.MidlineLeft;
                vtmp.textWrappingMode = TextWrappingModes.NoWrap;
                vtmp.overflowMode = TextOverflowModes.Ellipsis;
                vtmp.raycastTarget = false;
                if (font != null) vtmp.font = font;
                textRefs[i] = vtmp;

                // Label (linha de baixo, pequena, colorida)
                var lblGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
                lblGO.transform.SetParent(pill.transform, false);
                var lrt = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = new Vector2(1f, 0.42f);
                lrt.offsetMin = new Vector2(16f, 4f); lrt.offsetMax = new Vector2(-6f, 0f);
                var ltmp = lblGO.GetComponent<TextMeshProUGUI>();
                ltmp.text = d.label; ltmp.fontSize = 11.5f; ltmp.fontStyle = FontStyles.Bold;
                ltmp.color = new Color(d.labelColor.r, d.labelColor.g, d.labelColor.b, 0.75f);
                ltmp.alignment = TextAlignmentOptions.MidlineLeft;
                ltmp.raycastTarget = false;
                if (font != null) ltmp.font = font;
            }

            statMpsText      = textRefs[0];
            statMultText     = textRefs[1];
            statTotalText    = textRefs[2];
            statPrestigeText = textRefs[3];

            RefreshMainStats();
        }

        private void RefreshMainStats()
        {
            if (statMpsText == null) return;
            statMpsText.text = $"+{NumberFormatter.Format(GameManager.Instance.MoneyPerSecond)}/s";
            double mult = CharacterManager.Instance.GetTotalMultiplier()
                          * GameManager.Instance.PrestigeMultiplier
                          * GemShop.GetPrestigeBonus() * GemShop.GetProductionMult();
            statMultText.text  = $"x{NumberFormatter.Format(mult)} mult";
            statTotalText.text = $"${NumberFormatter.Format(GameManager.Instance.TotalEarned)} total";
            int pc = GameManager.Instance.PrestigeCount;
            statPrestigeText.text = pc > 0 ? $"x{pc} prestígio" : "sem prestígio";
        }

        // ── Próximo Desbloqueio ───────────────────────────────────────────────

        private void SetupNextUnlockBanner() { } // removed — pulse happens on the card itself

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
            // Don't interrupt an active combat session with a random event popup.
            if (_battlePanel != null && _battlePanel.gameObject.activeSelf) return;
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

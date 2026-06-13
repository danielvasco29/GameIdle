using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class StatsPanel : MonoBehaviour
    {
        private static readonly Color NavyDark    = new(0.055f, 0.094f, 0.165f, 0.95f);
        private static readonly Color NavyRow0    = new(0.075f, 0.120f, 0.200f, 1f);
        private static readonly Color NavyRow1    = new(0.055f, 0.094f, 0.165f, 1f);
        private static readonly Color GoldColor   = new(1f,     0.808f, 0.227f, 1f);
        private static readonly Color GreenColor  = new(0.247f, 0.749f, 0.353f, 1f);
        private static readonly Color TextPrimary = new(0.933f, 0.953f, 0.980f, 1f);
        private static readonly Color TextSec     = new(0.624f, 0.698f, 0.788f, 1f);

        private TextMeshProUGUI _valTaps;
        private TextMeshProUGUI _valHires;
        private TextMeshProUGUI _valPrestiges;
        private TextMeshProUGUI _valKills;
        private TextMeshProUGUI _valBossKills;
        private TextMeshProUGUI _valCycle;
        private TextMeshProUGUI _valWave;
        private TextMeshProUGUI _valMoney;
        private TextMeshProUGUI _valGems;
        private TextMeshProUGUI _valAchievements;

        // Referências do gráfico de donut (conquistas)
        private Image           _donutFill;
        private TextMeshProUGUI _donutLabel;
        private TextMeshProUGUI _donutSub;

        // Referências do gráfico de barras (atividade: cliques/monstros/etc.)
        private const int BarCount = 4;
        private readonly Image[]           _barFills  = new Image[BarCount];
        private readonly TextMeshProUGUI[] _barValues = new TextMeshProUGUI[BarCount];

        private void Awake()
        {
            BuildUI();
            gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshStats();
        }

        public void Close() => gameObject.SetActive(false);

        public void RefreshStats()
        {
            var gm = GameManager.Instance;
            var cm = CombatManager.Instance;

            _valTaps.text        = gm != null ? gm.LifetimeTapCount.ToString("N0")      : "0";
            _valHires.text       = gm != null ? gm.LifetimeHireCount.ToString("N0")     : "0";
            _valPrestiges.text   = gm != null ? gm.LifetimePrestigeCount.ToString("N0") : "0";
            _valKills.text       = gm != null ? gm.LifetimeKillCount.ToString("N0")     : "0";
            _valBossKills.text   = gm != null ? gm.LifetimeBossKillCount.ToString("N0") : "0";
            _valCycle.text       = (cm?.Cycle ?? 1).ToString();
            _valWave.text        = (cm?.Wave  ?? 1).ToString();
            _valMoney.text       = gm != null ? "$" + NumberFormatter.Format(gm.TotalEarned) : "$0";
            _valGems.text        = gm != null ? gm.LifetimeGemsEarned.ToString("N0") : "0";

            int unlocked = AchievementManager.GetSaved().Count;
            int total    = AchievementManager.All.Length;
            _valAchievements.text = $"{unlocked} / {total}";

            // Atualiza gráfico de donut
            float pct = total > 0 ? (float)unlocked / total : 0f;
            _donutFill.fillAmount = pct;
            _donutLabel.text = $"{unlocked}/{total}";
            _donutSub.text   = $"{Mathf.RoundToInt(pct * 100f)}%";

            // Atualiza gráfico de barras (cliques, monstros, bosses, prestígios)
            long clicks    = gm != null ? gm.LifetimeTapCount       : 0;
            long kills     = gm != null ? gm.LifetimeKillCount      : 0;
            long bossKills = gm != null ? gm.LifetimeBossKillCount  : 0;
            long prestiges = gm != null ? gm.LifetimePrestigeCount  : 0;
            long[] barVals = { clicks, kills, bossKills, prestiges };

            // Normaliza pelo maior valor; mantém um mínimo visível quando > 0.
            long maxVal = 1;
            foreach (var v in barVals) if (v > maxVal) maxVal = v;
            for (int i = 0; i < _barFills.Length; i++)
            {
                if (_barFills[i] == null) continue;
                float frac = (float)barVals[i] / maxVal;
                _barFills[i].fillAmount = barVals[i] > 0 ? Mathf.Max(frac, 0.03f) : 0f;
                _barValues[i].text = barVals[i].ToString("N0");
            }
        }

        private void BuildUI()
        {
            var rt = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.03f, 0.05f);
            rt.anchorMax = new Vector2(0.97f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox();
            bg.type   = Image.Type.Sliced;
            bg.color  = NavyDark;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            const float sidePad = 14f;
            const float topPad  = 12f;
            const float titleH  = 36f;
            const float sepH    = 2f;
            const float sepGap  = 6f;
            const float closeH  = 44f;

            // ── Título ────────────────────────────────────────────────────────
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.offsetMin = new Vector2(sidePad, 0f); trt.offsetMax = new Vector2(-sidePad, 0f);
            trt.sizeDelta = new Vector2(trt.sizeDelta.x, titleH);
            trt.anchoredPosition = new Vector2(0f, -topPad);
            var titleTxt = titleGO.GetComponent<TextMeshProUGUI>();
            titleTxt.font = font; titleTxt.text = "ESTATÍSTICAS"; titleTxt.fontSize = 24f;
            titleTxt.fontStyle = FontStyles.Bold; titleTxt.color = GoldColor;
            titleTxt.alignment = TextAlignmentOptions.Center; titleTxt.raycastTarget = false;

            // ── Separador ─────────────────────────────────────────────────────
            var sepGO = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sepGO.transform.SetParent(transform, false);
            var srt = sepGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(sidePad, 0f); srt.offsetMax = new Vector2(-sidePad, 0f);
            srt.sizeDelta = new Vector2(srt.sizeDelta.x, sepH);
            srt.anchoredPosition = new Vector2(0f, -(topPad + titleH + sepGap));
            sepGO.GetComponent<Image>().color = GoldColor;
            sepGO.GetComponent<Image>().raycastTarget = false;

            float contentTop    = topPad + titleH + sepGap + sepH + sepGap;
            float contentBottom = closeH + 20f;

            // ── Botão Fechar ─────────────────────────────────────────────────
            var closeGO = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var crt = closeGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.offsetMin = new Vector2(sidePad, 12f); crt.offsetMax = new Vector2(-sidePad, 12f);
            crt.sizeDelta = new Vector2(crt.sizeDelta.x, closeH);
            crt.anchoredPosition = new Vector2(0f, 12f);
            var closeBtnImg = closeGO.GetComponent<Image>();
            closeBtnImg.sprite = UiSpriteFactory.RoundedBox();
            closeBtnImg.type = Image.Type.Sliced;
            closeBtnImg.color = new Color(0.10f, 0.16f, 0.28f, 1f);
            closeGO.GetComponent<Button>().onClick.AddListener(Close);
            var clblGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            clblGO.transform.SetParent(closeGO.transform, false);
            var clrt2 = clblGO.GetComponent<RectTransform>();
            clrt2.anchorMin = Vector2.zero; clrt2.anchorMax = Vector2.one;
            clrt2.offsetMin = Vector2.zero; clrt2.offsetMax = Vector2.zero;
            var closeTxt = clblGO.GetComponent<TextMeshProUGUI>();
            closeTxt.font = font; closeTxt.text = "FECHAR"; closeTxt.fontSize = 17f;
            closeTxt.fontStyle = FontStyles.Bold; closeTxt.color = Color.white;
            closeTxt.alignment = TextAlignmentOptions.Center; closeTxt.raycastTarget = false;

            // ── Coluna esquerda: tabela (0 → 62%) ────────────────────────────
            var tableAreaGO = new GameObject("TableArea", typeof(RectTransform));
            tableAreaGO.transform.SetParent(transform, false);
            var tableRT = tableAreaGO.GetComponent<RectTransform>();
            tableRT.anchorMin = new Vector2(0f, 0f); tableRT.anchorMax = new Vector2(0.62f, 1f);
            tableRT.offsetMin = new Vector2(sidePad, contentBottom);
            tableRT.offsetMax = new Vector2(-4f, -contentTop);

            BuildScrollTable(tableAreaGO.transform, font);

            // ── Coluna direita: gráfico (64% → 100%) ─────────────────────────
            var chartAreaGO = new GameObject("ChartArea", typeof(RectTransform));
            chartAreaGO.transform.SetParent(transform, false);
            var chartRT = chartAreaGO.GetComponent<RectTransform>();
            chartRT.anchorMin = new Vector2(0.62f, 0f); chartRT.anchorMax = new Vector2(1f, 1f);
            chartRT.offsetMin = new Vector2(4f, contentBottom);
            chartRT.offsetMax = new Vector2(-sidePad, -contentTop);

            BuildChartArea(chartAreaGO.transform, font);
        }

        private void BuildScrollTable(Transform parent, TMP_FontAsset font)
        {
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(parent, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero; scrollRT.offsetMax = Vector2.zero;
            scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 30f;

            var vpGO = new GameObject("VP", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
            vpGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            vpGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(vpGO.transform, false);
            var crt = contentGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0f, 1f);
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 0f; vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRT; scroll.content = crt;

            (string label, string ph)[] rows =
            {
                ("Cliques totais",           "0"),
                ("Funcionários contratados", "0"),
                ("Prestígios realizados",    "0"),
                ("Monstros derrotados",      "0"),
                ("Bosses derrotados",        "0"),
                ("Ciclo atual",              "1"),
                ("Onda atual",               "1"),
                ("Dinheiro ganho",           "$0"),
                ("Gemas coletadas",          "0"),
                ("Conquistas",               "0 / 0"),
            };

            var vals = new TextMeshProUGUI[rows.Length];
            for (int i = 0; i < rows.Length; i++)
                vals[i] = BuildRow(contentGO.transform, font, rows[i].label, rows[i].ph,
                                   i % 2 == 0 ? NavyRow0 : NavyRow1, i);

            _valTaps         = vals[0];
            _valHires        = vals[1];
            _valPrestiges    = vals[2];
            _valKills        = vals[3];
            _valBossKills    = vals[4];
            _valCycle        = vals[5];
            _valWave         = vals[6];
            _valMoney        = vals[7];
            _valGems         = vals[8];
            _valAchievements = vals[9];
        }

        private void BuildChartArea(Transform parent, TMP_FontAsset font)
        {
            // Título da seção
            var secLbl = new GameObject("ChartTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            secLbl.transform.SetParent(parent, false);
            var slrt = secLbl.GetComponent<RectTransform>();
            slrt.anchorMin = new Vector2(0f, 1f); slrt.anchorMax = new Vector2(1f, 1f);
            slrt.pivot = new Vector2(0.5f, 1f);
            slrt.sizeDelta = new Vector2(0f, 28f);
            slrt.anchoredPosition = new Vector2(0f, -4f);
            var slTxt = secLbl.GetComponent<TextMeshProUGUI>();
            slTxt.font = font; slTxt.text = "CONQUISTAS";
            slTxt.fontSize = 14f; slTxt.fontStyle = FontStyles.Bold;
            slTxt.color = GoldColor; slTxt.alignment = TextAlignmentOptions.Center;
            slTxt.raycastTarget = false;

            // Donut: círculo de fundo (cinza) centralizado — compacto, no topo
            var bgCircleGO = new GameObject("DonutBG", typeof(RectTransform), typeof(Image));
            bgCircleGO.transform.SetParent(parent, false);
            var bgRT = bgCircleGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.18f, 0.66f); bgRT.anchorMax = new Vector2(0.82f, 0.93f);
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            var bgImg = bgCircleGO.GetComponent<Image>();
            bgImg.sprite = UiSpriteFactory.Circle();
            bgImg.color = new Color(0.12f, 0.18f, 0.28f, 1f);
            bgImg.type = Image.Type.Filled;
            bgImg.fillMethod = Image.FillMethod.Radial360;
            bgImg.fillAmount = 1f;
            bgImg.raycastTarget = false;

            // Donut: arco de progresso (verde)
            var fillGO = new GameObject("DonutFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(parent, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0.18f, 0.66f); fillRT.anchorMax = new Vector2(0.82f, 0.93f);
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            _donutFill = fillGO.GetComponent<Image>();
            _donutFill.sprite = UiSpriteFactory.Circle();
            _donutFill.color = GreenColor;
            _donutFill.type = Image.Type.Filled;
            _donutFill.fillMethod = Image.FillMethod.Radial360;
            _donutFill.fillOrigin = (int)Image.Origin360.Top;
            _donutFill.fillAmount = 0f;
            _donutFill.raycastTarget = false;

            // Buraco do donut (círculo central cobrindo o centro)
            var holeGO = new GameObject("DonutHole", typeof(RectTransform), typeof(Image));
            holeGO.transform.SetParent(parent, false);
            var holeRT = holeGO.GetComponent<RectTransform>();
            holeRT.anchorMin = new Vector2(0.31f, 0.72f); holeRT.anchorMax = new Vector2(0.69f, 0.87f);
            holeRT.offsetMin = Vector2.zero; holeRT.offsetMax = Vector2.zero;
            var holeImg = holeGO.GetComponent<Image>();
            holeImg.sprite = UiSpriteFactory.Circle();
            holeImg.color = NavyDark;
            holeImg.raycastTarget = false;

            // Número central (X/Y)
            var lblGO = new GameObject("DonutLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(parent, false);
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0.1f, 0.78f); lblRT.anchorMax = new Vector2(0.9f, 0.86f);
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            _donutLabel = lblGO.GetComponent<TextMeshProUGUI>();
            _donutLabel.font = font; _donutLabel.text = "0/0";
            _donutLabel.fontSize = 18f; _donutLabel.fontStyle = FontStyles.Bold;
            _donutLabel.color = TextPrimary; _donutLabel.alignment = TextAlignmentOptions.Center;
            _donutLabel.raycastTarget = false;

            // Percentual abaixo do número
            var subGO = new GameObject("DonutSub", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(parent, false);
            var subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.1f, 0.73f); subRT.anchorMax = new Vector2(0.9f, 0.79f);
            subRT.offsetMin = Vector2.zero; subRT.offsetMax = Vector2.zero;
            _donutSub = subGO.GetComponent<TextMeshProUGUI>();
            _donutSub.font = font; _donutSub.text = "0%";
            _donutSub.fontSize = 13f; _donutSub.color = GreenColor;
            _donutSub.alignment = TextAlignmentOptions.Center; _donutSub.raycastTarget = false;

            // Legendas compactas lado a lado (verde = desbloqueadas, cinza = bloqueadas)
            BuildLegendRow(parent, font, new Vector2(0.05f, 0.595f), new Vector2(0.52f, 0.655f),
                           GreenColor, "Desbloq.");
            BuildLegendRow(parent, font, new Vector2(0.52f, 0.595f), new Vector2(0.98f, 0.655f),
                           new Color(0.25f, 0.32f, 0.42f, 1f), "Bloq.");

            // ── Gráfico de barras: ATIVIDADE ─────────────────────────────────
            // Compara totais acumulados (cliques, monstros, bosses, prestígios).
            // Não há histórico temporal salvo, então usamos barras comparativas
            // em vez de linhas — cada barra é normalizada pelo maior valor.
            var barTitle = new GameObject("BarTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            barTitle.transform.SetParent(parent, false);
            var btRT = barTitle.GetComponent<RectTransform>();
            btRT.anchorMin = new Vector2(0f, 0.52f); btRT.anchorMax = new Vector2(1f, 0.585f);
            btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
            var btTxt = barTitle.GetComponent<TextMeshProUGUI>();
            btTxt.font = font; btTxt.text = "ATIVIDADE";
            btTxt.fontSize = 14f; btTxt.fontStyle = FontStyles.Bold;
            btTxt.color = GoldColor; btTxt.alignment = TextAlignmentOptions.Center;
            btTxt.raycastTarget = false;

            // 4 barras horizontais, de cima para baixo
            (string label, Color color)[] bars =
            {
                ("Cliques",    new Color(0.40f, 0.80f, 1.00f, 1f)),
                ("Monstros",   GreenColor),
                ("Bosses",     GoldColor),
                ("Prestígios", new Color(0.78f, 0.55f, 1.00f, 1f)),
            };
            const float barTop = 0.48f, barH = 0.085f, barGap = 0.025f;
            for (int i = 0; i < BarCount; i++)
            {
                float yMax = barTop - i * (barH + barGap);
                float yMin = yMax - barH;
                BuildBar(parent, font, i, new Vector2(0.04f, yMin), new Vector2(0.96f, yMax),
                         bars[i].label, bars[i].color);
            }
        }

        // Constrói uma barra horizontal: rótulo | trilho com preenchimento | valor.
        private void BuildBar(Transform parent, TMP_FontAsset font, int index,
                              Vector2 aMin, Vector2 aMax, string label, Color color)
        {
            var go = new GameObject($"Bar_{index}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Rótulo (esquerda)
            var lblGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(go.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0.34f, 1f);
            lrt.offsetMin = new Vector2(2f, 0f); lrt.offsetMax = new Vector2(-4f, 0f);
            var ltmp = lblGO.GetComponent<TextMeshProUGUI>();
            ltmp.font = font; ltmp.text = label; ltmp.fontSize = 13f;
            ltmp.fontStyle = FontStyles.Bold; ltmp.color = TextSec;
            ltmp.alignment = TextAlignmentOptions.MidlineLeft;
            ltmp.overflowMode = TextOverflowModes.Ellipsis; ltmp.raycastTarget = false;

            // Trilho (fundo da barra)
            var trackGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackGO.transform.SetParent(go.transform, false);
            var trRT = trackGO.GetComponent<RectTransform>();
            trRT.anchorMin = new Vector2(0.34f, 0.15f); trRT.anchorMax = new Vector2(1f, 0.85f);
            trRT.offsetMin = Vector2.zero; trRT.offsetMax = Vector2.zero;
            var trackImg = trackGO.GetComponent<Image>();
            trackImg.sprite = UiSpriteFactory.RoundedBox(); trackImg.type = Image.Type.Sliced;
            trackImg.color = new Color(1f, 1f, 1f, 0.08f); trackImg.raycastTarget = false;

            // Preenchimento da barra
            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(trackGO.transform, false);
            var fRT = fillGO.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
            var fillImg = fillGO.GetComponent<Image>();
            fillImg.sprite = UiSpriteFactory.RoundedBox(); fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f; fillImg.color = color; fillImg.raycastTarget = false;
            _barFills[index] = fillImg;

            // Valor (sobre o trilho, alinhado à direita)
            var valGO = new GameObject("V", typeof(RectTransform), typeof(TextMeshProUGUI));
            valGO.transform.SetParent(trackGO.transform, false);
            var vRT = valGO.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero; vRT.anchorMax = Vector2.one;
            vRT.offsetMin = new Vector2(6f, 0f); vRT.offsetMax = new Vector2(-6f, 0f);
            var vtmp = valGO.GetComponent<TextMeshProUGUI>();
            vtmp.font = font; vtmp.text = "0"; vtmp.fontSize = 12f;
            vtmp.fontStyle = FontStyles.Bold; vtmp.color = TextPrimary;
            vtmp.alignment = TextAlignmentOptions.MidlineRight; vtmp.raycastTarget = false;
            _barValues[index] = vtmp;
        }

        private void BuildLegendRow(Transform parent, TMP_FontAsset font, Vector2 aMin, Vector2 aMax, Color dotColor, string label)
        {
            var go = new GameObject("Legend", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var dotGO = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dotGO.transform.SetParent(go.transform, false);
            var drt = dotGO.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0.2f); drt.anchorMax = new Vector2(0f, 0.8f);
            drt.offsetMin = new Vector2(4f, 0f); drt.offsetMax = new Vector2(14f, 0f);
            var dotImg = dotGO.GetComponent<Image>();
            dotImg.sprite = UiSpriteFactory.Circle();
            dotImg.color = dotColor; dotImg.raycastTarget = false;

            var txtGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var trt2 = txtGO.GetComponent<RectTransform>();
            trt2.anchorMin = new Vector2(0f, 0f); trt2.anchorMax = new Vector2(1f, 1f);
            trt2.offsetMin = new Vector2(20f, 0f); trt2.offsetMax = Vector2.zero;
            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.font = font; tmp.text = label; tmp.fontSize = 13f;
            tmp.color = TextSec; tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        private TextMeshProUGUI BuildRow(Transform parent, TMP_FontAsset font,
                                         string label, string placeholder, Color bgColor, int index)
        {
            const float rowH = 60f;
            const float padH = 12f;

            var rowGO = new GameObject($"Row_{index}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowGO.transform.SetParent(parent, false);
            rowGO.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
            var le = rowGO.GetComponent<LayoutElement>();
            le.preferredHeight = rowH; le.flexibleWidth = 1f;
            rowGO.GetComponent<Image>().color = bgColor;

            var hlgGO = new GameObject("H", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            hlgGO.transform.SetParent(rowGO.transform, false);
            var hlgRT = hlgGO.GetComponent<RectTransform>();
            hlgRT.anchorMin = Vector2.zero; hlgRT.anchorMax = Vector2.one;
            hlgRT.offsetMin = new Vector2(padH, 0f); hlgRT.offsetMax = new Vector2(-padH, 0f);
            var hlg = hlgGO.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true; hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = false;

            var lblGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            lblGO.transform.SetParent(hlgGO.transform, false);
            lblGO.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var lblTxt = lblGO.GetComponent<TextMeshProUGUI>();
            lblTxt.font = font; lblTxt.text = label; lblTxt.fontSize = 16f;
            lblTxt.color = TextSec; lblTxt.alignment = TextAlignmentOptions.MidlineLeft;
            lblTxt.overflowMode = TextOverflowModes.Ellipsis; lblTxt.raycastTarget = false;

            var valGO = new GameObject("V", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            valGO.transform.SetParent(hlgGO.transform, false);
            var valLE = valGO.GetComponent<LayoutElement>();
            valLE.preferredWidth = 110f; valLE.minWidth = 80f;
            var valTxt = valGO.GetComponent<TextMeshProUGUI>();
            valTxt.font = font; valTxt.text = placeholder; valTxt.fontSize = 18f;
            valTxt.fontStyle = FontStyles.Bold; valTxt.color = TextPrimary;
            valTxt.alignment = TextAlignmentOptions.MidlineRight; valTxt.raycastTarget = false;

            return valTxt;
        }
    }
}

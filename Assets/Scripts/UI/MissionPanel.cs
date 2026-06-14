using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Painel de missões diárias — construído em runtime, abre sobre tudo.
    public class MissionPanel : MonoBehaviour
    {
        private readonly MissionRow[] _rows = new MissionRow[DailyMissionSystem.All.Length];

        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.85f);
        private static readonly Color NavyCard  = new(0.106f, 0.169f, 0.275f, 1f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GreenColor= new(0.247f, 0.749f, 0.353f, 1f);
        private static readonly Color BlueColor = new(0.4f, 0.9f, 1f, 1f);
        private static readonly Color GrayColor = new(0.35f, 0.40f, 0.50f, 1f);

        private class MissionRow
        {
            public TextMeshProUGUI nameText, progressText;
            public Image bar;
            public Button claimBtn;
            public Image claimBtnImg;
            public TextMeshProUGUI claimBtnText;
        }

        private void Awake() => BuildUI();

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(660f, 500f);
            rt.anchoredPosition = Vector2.zero;

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox();
            bg.type   = Image.Type.Sliced;
            bg.color  = new Color(0.086f, 0.137f, 0.220f, 0.85f);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            // Título
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.85f); trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16f, 0f); trt.offsetMax = new Vector2(-16f, -8f);
            var ttmp = titleGO.GetComponent<TextMeshProUGUI>();
            ttmp.text = "MISSÕES"; ttmp.fontSize = 27; ttmp.fontStyle = FontStyles.Bold;
            ttmp.color = GoldColor; ttmp.alignment = TextAlignmentOptions.Center; ttmp.raycastTarget = false;
            if (font != null) ttmp.font = font;

            // Linhas de missão
            float rowH = 0.76f / DailyMissionSystem.All.Length;
            for (int i = 0; i < DailyMissionSystem.All.Length; i++)
            {
                float yMin = 0.10f + (DailyMissionSystem.All.Length - 1 - i) * rowH;
                float yMax = yMin + rowH - 0.01f;
                _rows[i] = BuildRow(i, new Vector2(0f, yMin), new Vector2(1f, yMax), font);
            }

            // Botão Fechar
            var closeGO = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var crt = closeGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(1f, 0.10f);
            crt.offsetMin = new Vector2(16f, 8f); crt.offsetMax = new Vector2(-16f, -4f);
            closeGO.GetComponent<Image>().sprite = UiSpriteFactory.RoundedBox();
            closeGO.GetComponent<Image>().type   = Image.Type.Sliced;
            closeGO.GetComponent<Image>().color  = new Color(0.10f, 0.16f, 0.28f, 1f); // navy
            closeGO.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            var cLbl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            cLbl.transform.SetParent(closeGO.transform, false);
            var clrt = cLbl.GetComponent<RectTransform>();
            clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one; clrt.offsetMin = clrt.offsetMax = Vector2.zero;
            var ctmp = cLbl.GetComponent<TextMeshProUGUI>();
            ctmp.text = "FECHAR"; ctmp.fontSize = 16; ctmp.fontStyle = FontStyles.Bold;
            ctmp.color = Color.white; ctmp.alignment = TextAlignmentOptions.Center; ctmp.raycastTarget = false;
            if (font != null) ctmp.font = font;
        }

        private MissionRow BuildRow(int index, Vector2 aMin, Vector2 aMax, TMP_FontAsset font)
        {
            var mission = DailyMissionSystem.All[index];
            var row = new MissionRow();

            var rowGO = new GameObject($"Row{index}", typeof(RectTransform), typeof(Image));
            rowGO.transform.SetParent(transform, false);
            var rrt = rowGO.GetComponent<RectTransform>();
            rrt.anchorMin = aMin; rrt.anchorMax = aMax;
            rrt.offsetMin = new Vector2(12f, 4f); rrt.offsetMax = new Vector2(-12f, -4f);
            rowGO.GetComponent<Image>().color = NavyCard;
            rowGO.GetComponent<Image>().sprite = UiSpriteFactory.RoundedBox();
            rowGO.GetComponent<Image>().type   = Image.Type.Sliced;
            rowGO.GetComponent<Image>().raycastTarget = false;

            // Nome da missão
            var nameGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(rowGO.transform, false);
            var nrt = nameGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0.55f); nrt.anchorMax = new Vector2(0.65f, 1f);
            nrt.offsetMin = new Vector2(10f, 0f); nrt.offsetMax = Vector2.zero;
            row.nameText = nameGO.GetComponent<TextMeshProUGUI>();
            row.nameText.text = mission.name; row.nameText.fontSize = 24; row.nameText.fontStyle = FontStyles.Bold;
            row.nameText.color = Color.white; row.nameText.alignment = TextAlignmentOptions.MidlineLeft;
            row.nameText.raycastTarget = false; if (font != null) row.nameText.font = font;

            // Progresso texto
            var progGO = new GameObject("Prog", typeof(RectTransform), typeof(TextMeshProUGUI));
            progGO.transform.SetParent(rowGO.transform, false);
            var prrt = progGO.GetComponent<RectTransform>();
            prrt.anchorMin = new Vector2(0f, 0.05f); prrt.anchorMax = new Vector2(0.65f, 0.55f);
            prrt.offsetMin = new Vector2(10f, 0f); prrt.offsetMax = Vector2.zero;
            row.progressText = progGO.GetComponent<TextMeshProUGUI>();
            row.progressText.fontSize = 19; row.progressText.color = new Color(0.82f, 0.89f, 1f);
            row.progressText.alignment = TextAlignmentOptions.MidlineLeft; row.progressText.raycastTarget = false;
            if (font != null) row.progressText.font = font;

            // Barra de progresso (fundo)
            var barBGGO = new GameObject("BarBG", typeof(RectTransform), typeof(Image));
            barBGGO.transform.SetParent(rowGO.transform, false);
            var bbrT = barBGGO.GetComponent<RectTransform>();
            bbrT.anchorMin = new Vector2(0f, 0f); bbrT.anchorMax = new Vector2(0.65f, 0f);
            bbrT.offsetMin = new Vector2(10f, 4f); bbrT.offsetMax = new Vector2(0f, 10f);
            barBGGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f); barBGGO.GetComponent<Image>().raycastTarget = false;
            var barGO = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(barBGGO.transform, false);
            var barRT = barGO.GetComponent<RectTransform>();
            barRT.anchorMin = Vector2.zero; barRT.anchorMax = Vector2.one; barRT.offsetMin = barRT.offsetMax = Vector2.zero;
            row.bar = barGO.GetComponent<Image>();
            row.bar.type = Image.Type.Filled; row.bar.fillMethod = Image.FillMethod.Horizontal;
            row.bar.color = GreenColor; row.bar.raycastTarget = false;

            // Botão de recompensa
            var btnGO = new GameObject("Claim", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(rowGO.transform, false);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.67f, 0.1f); btnRT.anchorMax = new Vector2(1f, 0.9f);
            btnRT.offsetMin = new Vector2(4f, 0f); btnRT.offsetMax = new Vector2(-8f, 0f);
            row.claimBtnImg = btnGO.GetComponent<Image>();
            row.claimBtnImg.sprite = UiSpriteFactory.RoundedBox(); row.claimBtnImg.type = Image.Type.Sliced;
            int cap = index;
            row.claimBtn = btnGO.GetComponent<Button>();
            row.claimBtn.onClick.AddListener(() => { DailyMissionSystem.TryClaim(cap); RefreshRows(); });
            var bLbl = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            bLbl.transform.SetParent(btnGO.transform, false);
            var blrt = bLbl.GetComponent<RectTransform>();
            blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one; blrt.offsetMin = blrt.offsetMax = Vector2.zero;
            row.claimBtnText = bLbl.GetComponent<TextMeshProUGUI>();
            row.claimBtnText.fontSize = 20; row.claimBtnText.fontStyle = FontStyles.Bold;
            row.claimBtnText.alignment = TextAlignmentOptions.Center; row.claimBtnText.raycastTarget = false;
            if (font != null) row.claimBtnText.font = font;

            return row;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshRows();
        }

        private void RefreshRows()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                var r = _rows[i];
                if (r == null) continue;
                var m = DailyMissionSystem.All[i];
                long prog = DailyMissionSystem.GetProgress(i);
                bool complete = DailyMissionSystem.IsComplete(i);
                bool claimed  = DailyMissionSystem.IsClaimed(i);

                // Nome/meta mudam a cada nivel (missoes infinitas) → atualiza sempre.
                r.nameText.text = $"{m.name}  <size=70%><color=#9fb2c9>Nv.{m.tier}</color></size>";

                r.progressText.text = m.target >= 10000
                    ? $"${NumberFormatter.Format(prog)} / ${NumberFormatter.Format(m.target)}"
                    : $"{prog} / {m.target}";
                r.bar.fillAmount = Mathf.Clamp01((float)prog / m.target);

                if (claimed)
                {
                    r.claimBtnImg.color = new Color(0.09f, 0.13f, 0.20f, 1f); // navy apagado
                    r.claimBtnText.text = "Coletado";
                    r.claimBtnText.color = new Color(0.55f, 0.6f, 0.7f, 1f);
                    r.claimBtn.interactable = false;
                }
                else if (complete)
                {
                    // Navy + texto dourado (acento) quando pronto para coletar
                    r.claimBtnImg.color = new Color(0.10f, 0.16f, 0.28f, 1f);
                    r.claimBtnText.text = $"+{m.gemReward} gemas";
                    r.claimBtnText.color = GoldColor;
                    r.claimBtn.interactable = true;
                }
                else
                {
                    r.claimBtnImg.color = new Color(0.10f, 0.16f, 0.28f, 1f); // navy
                    r.claimBtnText.text = $"+{m.gemReward}";
                    r.claimBtnText.color = new Color(0.6f, 0.65f, 0.75f, 1f);
                    r.claimBtn.interactable = false;
                }
            }
        }
    }
}

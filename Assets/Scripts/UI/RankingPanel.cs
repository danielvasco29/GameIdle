using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class RankingPanel : MonoBehaviour
    {
        private const string RankingKey = "GameIdle_Ranking";
        private const int MaxRecords = 20;

        private Transform rowsContainer;

        private void Awake()
        {
            BuildUI();
            gameObject.SetActive(false);
        }

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f);
            rt.anchorMax = new Vector2(0.95f, 0.95f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // Fix 6: RoundedBox sprite + sliced + new background color
            var bg = gameObject.AddComponent<Image>();
            bg.sprite = UiSpriteFactory.RoundedBox();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.05f, 0.08f, 0.16f, 0.85f);

            // Title bar
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var trt = titleGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(0f, -48f); trt.offsetMax = Vector2.zero;
            var ttmp = titleGO.GetComponent<TextMeshProUGUI>();
            ttmp.text = "RANKING DE PRESTÍGIOS";
            ttmp.fontSize = 24;
            ttmp.fontStyle = FontStyles.Bold;
            ttmp.alignment = TextAlignmentOptions.Center;
            ttmp.color = new Color(1f, 0.84f, 0f);
            ttmp.raycastTarget = false;

            // Separator
            var sepGO = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sepGO.transform.SetParent(transform, false);
            var sept = sepGO.GetComponent<RectTransform>();
            sept.anchorMin = new Vector2(0f, 1f); sept.anchorMax = new Vector2(1f, 1f);
            sept.offsetMin = new Vector2(12f, -52f); sept.offsetMax = new Vector2(-12f, -48f);
            sepGO.GetComponent<Image>().color = new Color(1f, 0.84f, 0f, 0.4f);

            // Scroll area
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(transform, false);
            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.14f); srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(8f, 0f); srt.offsetMax = new Vector2(-8f, -56f);

            var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vprt = vpGO.GetComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero; vprt.anchorMax = Vector2.one;
            vprt.offsetMin = vprt.offsetMax = Vector2.zero;
            vpGO.GetComponent<Mask>().showMaskGraphic = false;
            vpGO.GetComponent<Image>().color = Color.white;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(vpGO.transform, false);
            var crt = contentGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 3f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowsContainer = contentGO.transform;

            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.viewport = vprt; sr.content = crt;
            sr.horizontal = false; sr.vertical = true;
            sr.scrollSensitivity = 30f;

            // Fix 3: Close button — navy color + RoundedBox sprite sliced
            var closeBtnGO = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGO.transform.SetParent(transform, false);
            var cbrt = closeBtnGO.GetComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(0.25f, 0f); cbrt.anchorMax = new Vector2(0.75f, 0.12f);
            cbrt.offsetMin = new Vector2(0f, 4f); cbrt.offsetMax = new Vector2(0f, -4f);
            var closeBtnImg = closeBtnGO.GetComponent<Image>();
            closeBtnImg.sprite = UiSpriteFactory.RoundedBox();
            closeBtnImg.type = Image.Type.Sliced;
            closeBtnImg.color = new Color(0.10f, 0.16f, 0.28f, 1f);
            closeBtnGO.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));

            var clblGO = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            clblGO.transform.SetParent(closeBtnGO.transform, false);
            var clrt = clblGO.GetComponent<RectTransform>();
            clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
            clrt.offsetMin = clrt.offsetMax = Vector2.zero;
            var cltmp = clblGO.GetComponent<TextMeshProUGUI>();
            cltmp.text = "FECHAR";
            cltmp.fontSize = 15;
            cltmp.fontStyle = FontStyles.Bold;
            cltmp.alignment = TextAlignmentOptions.Center;
            cltmp.color = Color.white;
            cltmp.raycastTarget = false;
        }

        public void Open()
        {
            RefreshRows();
            gameObject.SetActive(true);
        }

        private void RefreshRows()
        {
            for (int i = rowsContainer.childCount - 1; i >= 0; i--)
                Destroy(rowsContainer.GetChild(i).gameObject);

            var data = Load();
            data.records.Sort((a, b) => b.prestigeCount.CompareTo(a.prestigeCount));

            AddRow(rank: 0, pos: "#", name: "JOGADOR", count: "PREST.", kills: "KILLS", boss: "BOSS", date: "DATA", header: true);
            if (data.records.Count == 0)
            {
                AddEmptyMessage();
                return;
            }
            for (int i = 0; i < data.records.Count; i++)
            {
                var r = data.records[i];
                AddRow(rank: i + 1, pos: $"{i + 1}.", name: r.playerName,
                       count: $"#{r.prestigeCount}", kills: $"{r.killCount}", boss: $"{r.bossKillCount}", date: r.date);
            }
        }

        private void AddRow(int rank, string pos, string name, string count, string kills, string boss, string date, bool header = false)
        {
            var rowGO = new GameObject($"Row{rank}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(rowsContainer, false);
            rowGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 48f);

            rowGO.GetComponent<Image>().color = header
                ? new Color(0.10f, 0.16f, 0.30f, 1f)
                : (rank % 2 == 0
                    ? new Color(0.10f, 0.16f, 0.26f, 1f)
                    : new Color(0.075f, 0.12f, 0.20f, 1f));

            var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(6, 6, 3, 3);
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;

            Color textColor = header ? new Color(1f, 0.84f, 0f) : Color.white;
            FontStyles style = FontStyles.Bold;
            int fontSize = header ? 15 : 16;

            AddCell(rowGO.transform, pos,   36f, textColor, style, fontSize);
            AddCell(rowGO.transform, name,   0f, textColor, style, fontSize, flexible: true);
            AddCell(rowGO.transform, count, 70f, textColor, style, fontSize);
            AddCell(rowGO.transform, kills, 62f, textColor, style, fontSize);
            AddCell(rowGO.transform, boss,  56f, textColor, style, fontSize);
            AddCell(rowGO.transform, date,  84f, textColor, style, fontSize);
        }

        private static void AddCell(Transform parent, string text, float width, Color color,
                                    FontStyles style, int fontSize, bool flexible = false)
        {
            var go = new GameObject("C", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            if (flexible) le.flexibleWidth = 1f;
            else { le.minWidth = width; le.preferredWidth = width; }
        }

        private void AddEmptyMessage()
        {
            var go = new GameObject("Empty", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(rowsContainer, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 50f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = "Nenhum prestígio ainda.\nFaça seu primeiro reset!";
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.6f, 0.6f);
            tmp.raycastTarget = false;
        }

        // Fix 1: Deduplication — update existing record if new prestigeCount is higher
        public static void AddRecord(int prestigeCount)
        {
            var data = Load();
            const string playerName = "Jogador";
            int kills     = GameManager.Instance != null ? GameManager.Instance.LifetimeKillCount     : 0;
            int bossKills = GameManager.Instance != null ? GameManager.Instance.LifetimeBossKillCount : 0;
            int existingIndex = data.records.FindIndex(r => r.playerName == playerName);
            if (existingIndex >= 0)
            {
                if (prestigeCount > data.records[existingIndex].prestigeCount)
                {
                    data.records[existingIndex].prestigeCount = prestigeCount;
                    data.records[existingIndex].killCount     = kills;
                    data.records[existingIndex].bossKillCount = bossKills;
                    data.records[existingIndex].date = DateTime.Now.ToString("dd/MM/yy");
                }
            }
            else
            {
                data.records.Add(new PrestigeRecord
                {
                    playerName    = playerName,
                    prestigeCount = prestigeCount,
                    killCount     = kills,
                    bossKillCount = bossKills,
                    date          = DateTime.Now.ToString("dd/MM/yy")
                });
            }
            data.records.Sort((a, b) => b.prestigeCount.CompareTo(a.prestigeCount));
            if (data.records.Count > MaxRecords)
                data.records.RemoveRange(MaxRecords, data.records.Count - MaxRecords);
            Save(data);
        }

        private static RankingData Load()
        {
            var json = PlayerPrefs.GetString(RankingKey, "");
            if (string.IsNullOrEmpty(json)) return new RankingData();
            try { return JsonUtility.FromJson<RankingData>(json) ?? new RankingData(); }
            catch { return new RankingData(); }
        }

        private static void Save(RankingData data)
        {
            PlayerPrefs.SetString(RankingKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}

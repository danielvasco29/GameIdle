using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Gerencia avatares de funcionários que caminham pelo escritório (Panel_Main).
    // Chame Init(panelMain) logo após SetupTapButton, e SyncWorkers() quando os
    // personagens forem atualizados.
    public class OfficeWorkerManager : MonoBehaviour
    {
        private RectTransform _panel;
        private readonly List<WorkerAvatar> _workers = new();

        // Área de caminhada dentro do Panel_Main (em espaço local do panel)
        private static readonly Vector2 WalkMin = new(-600f, -320f);
        private static readonly Vector2 WalkMax = new( 600f,  160f);

        public void Init(RectTransform panelMain)
        {
            _panel = panelMain;
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharactersUpdated += SyncWorkers;
            SyncWorkers();
        }

        private void OnDestroy()
        {
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharactersUpdated -= SyncWorkers;
        }

        public void SyncWorkers()
        {
            if (_panel == null) return;

            var chars = CharacterManager.Instance.GetAllCharacters();

            // Coleta personagens ativos (level > 0), limitado a 10 avatares
            var active = new List<CharacterInstance>();
            foreach (var c in chars)
                if (c.level > 0) active.Add(c);
            if (active.Count > 10) active = active.GetRange(0, 10);

            // Remove avatares a mais
            while (_workers.Count > active.Count)
            {
                var w = _workers[_workers.Count - 1];
                _workers.RemoveAt(_workers.Count - 1);
                if (w != null) Destroy(w.gameObject);
            }

            // Adiciona avatares faltando
            for (int i = _workers.Count; i < active.Count; i++)
            {
                var avatar = SpawnWorker(active[i]);
                _workers.Add(avatar);
            }
        }

        private WorkerAvatar SpawnWorker(CharacterInstance ci)
        {
            var go = new GameObject("Worker_" + ci.data.characterName,
                typeof(RectTransform), typeof(Image), typeof(WorkerAvatar));
            go.transform.SetParent(_panel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(48f, 48f);
            rt.anchoredPosition = RandomPos();

            // Fundo circular colorido
            var bg = go.GetComponent<Image>();
            bg.sprite = UiSpriteFactory.Circle();
            bg.color  = new Color(ci.data.tintColor.r, ci.data.tintColor.g, ci.data.tintColor.b, 0.9f);
            bg.raycastTarget = false;

            // Portrait dentro (child com Mask)
            var maskGO = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGO.transform.SetParent(go.transform, false);
            var mrt = maskGO.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = mrt.offsetMax = Vector2.zero;
            var mImg = maskGO.GetComponent<Image>();
            mImg.sprite = UiSpriteFactory.Circle();
            mImg.raycastTarget = false;
            maskGO.GetComponent<Mask>().showMaskGraphic = false;

            var portGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portGO.transform.SetParent(maskGO.transform, false);
            var prt = portGO.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var portImg = portGO.GetComponent<Image>();
            portImg.raycastTarget = false;

            var tex = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterId}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterName}");
            if (tex != null)
                portImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            var avatar = go.GetComponent<WorkerAvatar>();
            avatar.Init(rt, WalkMin, WalkMax);
            return avatar;
        }

        private static Vector2 RandomPos() =>
            new(Random.Range(WalkMin.x, WalkMax.x), Random.Range(WalkMin.y, WalkMax.y));
    }

    // Comportamento individual de cada avatar: caminha para um alvo, espera, repete.
    public class WorkerAvatar : MonoBehaviour
    {
        private RectTransform _rt;
        private Vector2 _min, _max;

        public void Init(RectTransform rt, Vector2 min, Vector2 max)
        {
            _rt = rt; _min = min; _max = max;
            StartCoroutine(Wander());
        }

        private IEnumerator Wander()
        {
            while (true)
            {
                // Espera aleatória antes de andar
                yield return new WaitForSeconds(Random.Range(0.5f, 2.5f));

                Vector2 target = new(Random.Range(_min.x, _max.x), Random.Range(_min.y, _max.y));
                float speed    = Random.Range(55f, 95f); // px/s
                float dist     = Vector2.Distance(_rt.anchoredPosition, target);
                float duration = dist / speed;

                // Flip horizontal conforme direção
                bool goingRight = target.x > _rt.anchoredPosition.x;
                _rt.localScale = new Vector3(goingRight ? 1f : -1f, 1f, 1f);

                Vector2 start = _rt.anchoredPosition;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _rt.anchoredPosition = Vector2.Lerp(start, target, elapsed / duration);
                    yield return null;
                }
                _rt.anchoredPosition = target;
                _rt.localScale = Vector3.one;
            }
        }
    }
}

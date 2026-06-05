using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class OfficeWorkerManager : MonoBehaviour
    {
        private RectTransform _panel;
        private readonly List<WorkerAvatar> _workers = new();

        // Fixed desk positions in Panel_Main local space (isometric layout)
        private static readonly Vector2[] DeskPositions =
        {
            new(-430f, -55f),
            new(-290f,  -5f),
            new(-150f,  25f),
            new(-390f,-135f),
            new(-250f, -90f),
            new(  90f,-100f),
            new( 230f, -55f),
            new( 370f, -85f),
            new( 490f,-125f),
            new( 560f, -95f),
        };

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
            var active = new List<CharacterInstance>();
            foreach (var c in chars)
                if (c.level > 0) active.Add(c);
            if (active.Count > DeskPositions.Length) active = active.GetRange(0, DeskPositions.Length);

            while (_workers.Count > active.Count)
            {
                var w = _workers[_workers.Count - 1];
                _workers.RemoveAt(_workers.Count - 1);
                if (w != null) Destroy(w.gameObject);
            }

            for (int i = _workers.Count; i < active.Count; i++)
            {
                var avatar = SpawnWorker(active[i], i);
                _workers.Add(avatar);
            }
        }

        private WorkerAvatar SpawnWorker(CharacterInstance ci, int deskIndex)
        {
            var go = new GameObject("Worker_" + ci.data.characterName,
                typeof(RectTransform), typeof(Image), typeof(WorkerAvatar));
            go.transform.SetParent(_panel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(54f, 54f);
            rt.anchoredPosition = DeskPositions[deskIndex];

            var bg = go.GetComponent<Image>();
            bg.sprite = UiSpriteFactory.Circle();
            bg.color  = new Color(ci.data.tintColor.r, ci.data.tintColor.g, ci.data.tintColor.b, 0.92f);
            bg.raycastTarget = false;

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
            avatar.Init(rt, DeskPositions[deskIndex]);
            return avatar;
        }
    }

    public class WorkerAvatar : MonoBehaviour
    {
        private RectTransform _rt;
        private Vector2 _desk;

        public void Init(RectTransform rt, Vector2 deskPos)
        {
            _rt = rt;
            _desk = deskPos;
            _rt.anchoredPosition = deskPos;
            StartCoroutine(WorkCycle());
        }

        private IEnumerator WorkCycle()
        {
            // Stagger startup so not all workers animate in sync
            yield return new WaitForSeconds(Random.Range(0f, 2f));

            while (true)
            {
                // Work at desk for 15-35 seconds
                float workDuration = Random.Range(15f, 35f);
                yield return StartCoroutine(WorkAtDesk(workDuration));

                // Take a short break — walk a few pixels away and back
                yield return StartCoroutine(TakeBreak());
            }
        }

        private IEnumerator WorkAtDesk(float duration)
        {
            float elapsed = 0f;
            float bobSpeed  = Random.Range(1.8f, 2.6f);  // cycles per second
            float bobAmp    = Random.Range(2.2f, 3.5f);  // pixels
            float tiltAmp   = Random.Range(0.8f, 1.4f);  // degrees

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed * bobSpeed * Mathf.PI * 2f;
                // Subtle bob (typing motion)
                float bobY = Mathf.Sin(t) * bobAmp;
                // Very slight tilt (leaning into screen)
                float tiltZ = Mathf.Sin(t * 0.5f) * tiltAmp;
                _rt.anchoredPosition = _desk + new Vector2(0f, bobY);
                _rt.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
                yield return null;
            }

            // Reset to neutral
            _rt.anchoredPosition = _desk;
            _rt.localRotation = Quaternion.identity;
        }

        private IEnumerator TakeBreak()
        {
            // Pick a nearby "break" spot (small offset from desk)
            Vector2 breakSpot = _desk + new Vector2(
                Random.Range(-60f, 60f),
                Random.Range(-30f, 30f));

            // Walk to break spot
            yield return StartCoroutine(WalkTo(breakSpot, 70f));

            // Wait briefly
            yield return new WaitForSeconds(Random.Range(1.5f, 3.5f));

            // Walk back to desk
            yield return StartCoroutine(WalkTo(_desk, 70f));
        }

        private IEnumerator WalkTo(Vector2 target, float speed)
        {
            float dist = Vector2.Distance(_rt.anchoredPosition, target);
            if (dist < 1f) yield break;

            float duration = dist / speed;
            Vector2 start = _rt.anchoredPosition;
            float elapsed = 0f;

            bool goRight = target.x >= start.x;
            _rt.localScale = new Vector3(goRight ? 1f : -1f, 1f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Smooth step for natural movement
                float smooth = t * t * (3f - 2f * t);
                _rt.anchoredPosition = Vector2.Lerp(start, target, smooth);
                // Slight vertical bob while walking
                _rt.anchoredPosition += new Vector2(0f, Mathf.Sin(elapsed * 12f) * 2f);
                yield return null;
            }

            _rt.anchoredPosition = target;
            _rt.localScale = Vector3.one;
            _rt.localRotation = Quaternion.identity;
        }
    }
}

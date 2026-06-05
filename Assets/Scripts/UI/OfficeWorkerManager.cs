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

        // Roaming bounds in Panel_Main local space
        private static readonly Rect RoamBounds = new Rect(-560f, -200f, 1120f, 220f);

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
            if (active.Count > 10) active = active.GetRange(0, 10);

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

        private WorkerAvatar SpawnWorker(CharacterInstance ci, int index)
        {
            // Root container
            var go = new GameObject("Worker_" + ci.data.characterName,
                typeof(RectTransform), typeof(WorkerAvatar));
            go.transform.SetParent(_panel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(56f, 80f); // wider to fit body + legs

            // Spread initial positions
            float startX = RoamBounds.xMin + (index + 0.5f) * (RoamBounds.width / 10f);
            rt.anchoredPosition = new Vector2(startX, Random.Range(RoamBounds.yMin, RoamBounds.yMax));

            // ── Body circle ──────────────────────────────────────────────────
            var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGO.transform.SetParent(go.transform, false);
            var bodyRt = bodyGO.GetComponent<RectTransform>();
            bodyRt.anchorMin = bodyRt.anchorMax = bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.anchoredPosition = Vector2.zero;
            bodyRt.sizeDelta = new Vector2(52f, 52f);

            var bgImg = bodyGO.GetComponent<Image>();
            bgImg.sprite = UiSpriteFactory.Circle();
            bgImg.color = new Color(ci.data.tintColor.r, ci.data.tintColor.g, ci.data.tintColor.b, 0.92f);
            bgImg.raycastTarget = false;

            // Portrait mask
            var maskGO = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGO.transform.SetParent(bodyGO.transform, false);
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
            if (tex == null) tex = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterName}");
            if (tex != null)
                portImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            // ── Legs ─────────────────────────────────────────────────────────
            Color legColor = new Color(
                Mathf.Clamp01(ci.data.tintColor.r * 0.55f),
                Mathf.Clamp01(ci.data.tintColor.g * 0.55f),
                Mathf.Clamp01(ci.data.tintColor.b * 0.55f), 1f);

            var legL = MakeLeg(go.transform, "LegL", legColor, new Vector2(-10f, -48f));
            var legR = MakeLeg(go.transform, "LegR", legColor, new Vector2( 10f, -48f));

            // ── Foot dots ────────────────────────────────────────────────────
            Color footColor = new Color(0.18f, 0.12f, 0.08f, 1f);
            var footL = MakeFoot(go.transform, "FootL", footColor, new Vector2(-10f, -72f));
            var footR = MakeFoot(go.transform, "FootR", footColor, new Vector2( 10f, -72f));

            // ── Shadow ───────────────────────────────────────────────────────
            var shadowGO = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            shadowGO.transform.SetParent(go.transform, false);
            shadowGO.transform.SetAsFirstSibling();
            var srt2 = shadowGO.GetComponent<RectTransform>();
            srt2.anchorMin = srt2.anchorMax = srt2.pivot = new Vector2(0.5f, 0f);
            srt2.anchoredPosition = new Vector2(0f, -74f);
            srt2.sizeDelta = new Vector2(42f, 12f);
            var sImg = shadowGO.GetComponent<Image>();
            sImg.sprite = UiSpriteFactory.Circle();
            sImg.color = new Color(0f, 0f, 0f, 0.22f);
            sImg.raycastTarget = false;

            var avatar = go.GetComponent<WorkerAvatar>();
            avatar.Init(rt, bodyRt,
                legL, legR, footL, footR,
                srt2, RoamBounds);
            return avatar;
        }

        private static RectTransform MakeLeg(Transform parent, string name, Color color, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(9f, 22f);
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.RoundedBox();
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        private static RectTransform MakeFoot(Transform parent, string name, Color color, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(13f, 7f);
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.RoundedBox();
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }
    }

    public class WorkerAvatar : MonoBehaviour
    {
        private RectTransform _rt;        // root
        private RectTransform _bodyRt;    // body circle (for tilt)
        private RectTransform _legL, _legR;
        private RectTransform _footL, _footR;
        private RectTransform _shadow;
        private Rect _bounds;

        // Walking state
        private Vector2 _target;
        private float _walkSpeed;
        private bool _walking;
        private float _stepPhase;       // 0..2PI cycling per step
        private float _idleTimer;
        private float _idleDuration;

        // Stagger
        private bool _started;

        public void Init(RectTransform rt, RectTransform bodyRt,
            RectTransform legL, RectTransform legR,
            RectTransform footL, RectTransform footR,
            RectTransform shadow, Rect bounds)
        {
            _rt = rt; _bodyRt = bodyRt;
            _legL = legL; _legR = legR;
            _footL = footL; _footR = footR;
            _shadow = shadow;
            _bounds = bounds;
            StartCoroutine(Lifecycle());
        }

        private IEnumerator Lifecycle()
        {
            // Stagger startup
            yield return new WaitForSeconds(Random.Range(0f, 3f));
            _started = true;

            PickNewTarget();

            while (true)
            {
                if (_walking)
                {
                    yield return StartCoroutine(WalkToTarget());
                    // Arrived — idle briefly
                    _walking = false;
                    _idleDuration = Random.Range(2f, 6f);
                    _idleTimer = 0f;
                    SetIdlePose();
                }
                else
                {
                    _idleTimer += Time.deltaTime;
                    if (_idleTimer >= _idleDuration)
                        PickNewTarget();
                }
                yield return null;
            }
        }

        private void PickNewTarget()
        {
            _target = new Vector2(
                Random.Range(_bounds.xMin + 30f, _bounds.xMax - 30f),
                Random.Range(_bounds.yMin + 10f, _bounds.yMax - 10f));
            _walkSpeed = Random.Range(55f, 85f);
            _walking = true;
        }

        private IEnumerator WalkToTarget()
        {
            Vector2 start = _rt.anchoredPosition;
            float dist = Vector2.Distance(start, _target);
            if (dist < 4f) yield break;

            float duration = dist / _walkSpeed;
            float elapsed = 0f;
            bool facingRight = _target.x >= start.x;

            // Flip root to face walking direction
            _rt.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);

            // Step frequency: ~2.5 steps/sec each leg → full cycle 2 steps
            float stepFreq = 2.5f; // cycles per second (one cycle = both legs)
            _stepPhase = 0f;

            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                _stepPhase += dt * stepFreq * Mathf.PI * 2f;

                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = t * t * (3f - 2f * t);
                _rt.anchoredPosition = Vector2.Lerp(start, _target, smooth);

                AnimateWalk(_stepPhase);
                yield return null;
            }

            _rt.anchoredPosition = _target;
            _rt.localScale = Vector3.one;
        }

        private void AnimateWalk(float phase)
        {
            // Leg swing: left and right are half-cycle apart
            float legSwingAmp = 12f;   // degrees rotation
            float legLiftAmp  = 7f;    // pixels up
            float bodyBobAmp  = 2.5f;  // pixels vertical
            float bodyTiltAmp = 4f;    // degrees tilt

            float sinL = Mathf.Sin(phase);
            float sinR = Mathf.Sin(phase + Mathf.PI); // opposite

            // Leg rotation (swings forward/back like real walking)
            _legL.localRotation = Quaternion.Euler(0f, 0f, sinL * legSwingAmp);
            _legR.localRotation = Quaternion.Euler(0f, 0f, sinR * legSwingAmp);

            // Foot position: lifted when leg is forward
            float liftL = Mathf.Max(0f, sinL) * legLiftAmp;
            float liftR = Mathf.Max(0f, sinR) * legLiftAmp;
            _footL.anchoredPosition = new Vector2(-10f, -72f + liftL);
            _footR.anchoredPosition = new Vector2( 10f, -72f + liftR);

            // Foot squash when on ground
            float scaleFL = 1f + Mathf.Max(0f, -sinL) * 0.25f;
            float scaleFR = 1f + Mathf.Max(0f, -sinR) * 0.25f;
            _footL.localScale = new Vector3(scaleFL, 1f / scaleFL, 1f);
            _footR.localScale = new Vector3(scaleFR, 1f / scaleFR, 1f);

            // Body bob (rises when a foot is off ground)
            float bob = Mathf.Abs(Mathf.Sin(phase)) * bodyBobAmp;
            _bodyRt.anchoredPosition = new Vector2(0f, bob);

            // Body tilt side to side
            _bodyRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * bodyTiltAmp);

            // Shadow shrinks when body is raised
            float shadowScale = 1f - bob * 0.03f;
            _shadow.localScale = new Vector3(shadowScale, 0.7f + bob * 0.02f, 1f);
        }

        private void SetIdlePose()
        {
            if (_legL) _legL.localRotation = Quaternion.identity;
            if (_legR) _legR.localRotation = Quaternion.identity;
            if (_footL) { _footL.anchoredPosition = new Vector2(-10f, -72f); _footL.localScale = Vector3.one; }
            if (_footR) { _footR.anchoredPosition = new Vector2( 10f, -72f); _footR.localScale = Vector3.one; }
            if (_bodyRt) { _bodyRt.anchoredPosition = Vector2.zero; _bodyRt.localRotation = Quaternion.identity; }
            if (_shadow) _shadow.localScale = Vector3.one;
            StartCoroutine(IdleBreath());
        }

        private IEnumerator IdleBreath()
        {
            float phase = Random.Range(0f, Mathf.PI * 2f);
            float end = Time.time + _idleDuration;
            while (Time.time < end && !_walking)
            {
                phase += Time.deltaTime * 1.2f;
                float breath = Mathf.Sin(phase) * 1.2f;
                if (_bodyRt) _bodyRt.anchoredPosition = new Vector2(0f, breath);
                if (_bodyRt) _bodyRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * 0.8f);
                yield return null;
            }
        }
    }
}

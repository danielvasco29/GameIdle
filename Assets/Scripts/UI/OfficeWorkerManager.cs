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

        private static readonly Rect RoamBounds = new Rect(-500f, -180f, 1000f, 200f);

        // Fixed spread positions so workers don't cluster — 10 spots across the office
        public static readonly Vector2[] SpreadPositions =
        {
            new(-420f, -120f), new(-280f,  -80f), new(-140f,  -50f), new(  0f, -100f),
            new( 140f,  -60f), new( 280f, -110f), new( 400f,  -80f), new(-360f, -160f),
            new( 200f, -150f), new( 460f, -140f),
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
            if (active.Count > 10) active = active.GetRange(0, 10);

            while (_workers.Count > active.Count)
            {
                var w = _workers[_workers.Count - 1];
                _workers.RemoveAt(_workers.Count - 1);
                if (w != null) Destroy(w.gameObject);
            }

            for (int i = _workers.Count; i < active.Count; i++)
                _workers.Add(SpawnWorker(active[i], i));
        }

        // Try to load sprite sheet frames (8 frames, each 1/8 of texture width)
        private static Sprite[] LoadFrames(CharacterInstance ci)
        {
            var tex = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterId}");
            if (tex == null) tex = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterName}");
            if (tex == null) return null;

            int fh = tex.height;

            // Sprite sheet: width is at least 2x height
            if (tex.width <= tex.height * 2)
                return new[] { Sprite.Create(tex, new Rect(0, 0, tex.width, fh), new Vector2(0.5f, 0.5f)) };

            // Determine frame count: try 8, but support other counts too
            int frameCount = 8;
            if (tex.width % 6 == 0 && tex.width / 6 == fh) frameCount = 6;
            else if (tex.width % 4 == 0 && tex.width / 4 == fh) frameCount = 4;

            int fw = tex.width / frameCount;
            var frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
                frames[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, fh), new Vector2(0.5f, 0.5f));
            return frames;
        }

        private WorkerAvatar SpawnWorker(CharacterInstance ci, int index)
        {
            var frames = LoadFrames(ci);

            var go = new GameObject("Worker_" + ci.data.characterName,
                typeof(RectTransform), typeof(WorkerAvatar));
            go.transform.SetParent(_panel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(90f, 110f);

            // Start at spread position so workers don't overlap
            var startPos = SpreadPositions[index % SpreadPositions.Length];
            startPos += new Vector2(Random.Range(-15f, 15f), Random.Range(-10f, 10f));
            rt.anchoredPosition = startPos;

            bool hasSpriteSheet = frames != null && frames.Length > 1;

            if (hasSpriteSheet)
            {
                // ── Sprite-sheet character ────────────────────────────────────
                // Shadow first (lowest sibling)
                var shadowGO = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                shadowGO.transform.SetParent(go.transform, false);
                shadowGO.transform.SetAsFirstSibling();
                var srt = shadowGO.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(0f, -38f);
                srt.sizeDelta = new Vector2(44f, 10f);
                var sImg = shadowGO.GetComponent<Image>();
                sImg.sprite = UiSpriteFactory.Circle();
                sImg.color = new Color(0f, 0f, 0f, 0.25f);
                sImg.raycastTarget = false;

                // Mask container — clips sprite to circle, hides white bg from non-transparent sheets
                var maskGO = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
                maskGO.transform.SetParent(go.transform, false);
                var maskRt = maskGO.GetComponent<RectTransform>();
                maskRt.anchorMin = maskRt.anchorMax = maskRt.pivot = new Vector2(0.5f, 0.5f);
                maskRt.anchoredPosition = Vector2.zero;
                maskRt.sizeDelta = new Vector2(80f, 80f);
                var maskImg = maskGO.GetComponent<Image>();
                maskImg.sprite = UiSpriteFactory.Circle();
                maskImg.raycastTarget = false;
                maskGO.GetComponent<Mask>().showMaskGraphic = false;

                var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(Image));
                bodyGO.transform.SetParent(maskGO.transform, false);
                var bodyRt = bodyGO.GetComponent<RectTransform>();
                bodyRt.anchorMin = Vector2.zero; bodyRt.anchorMax = Vector2.one;
                bodyRt.offsetMin = bodyRt.offsetMax = Vector2.zero;
                // Use maskRt as the transform we animate (it moves with the character)
                bodyRt = maskRt;

                var bodyImg = bodyGO.GetComponent<Image>();
                bodyImg.sprite = frames[0];
                bodyImg.raycastTarget = false;
                bodyImg.preserveAspect = true;

                var avatar = go.GetComponent<WorkerAvatar>();
                avatar.InitSheet(rt, bodyRt, bodyImg, frames, srt, RoamBounds);
            }
            else
            {
                // ── Procedural avatar: sprite flutuante com pernas, sem círculo ──
                var shadowGO = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                shadowGO.transform.SetParent(go.transform, false);
                shadowGO.transform.SetAsFirstSibling();
                var srt = shadowGO.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(0f, -43f);
                srt.sizeDelta = new Vector2(42f, 10f);
                var sImg = shadowGO.GetComponent<Image>();
                sImg.sprite = UiSpriteFactory.Circle();
                sImg.color = new Color(0f, 0f, 0f, 0.22f);
                sImg.raycastTarget = false;

                Color legColor = new Color(
                    Mathf.Clamp01(ci.data.tintColor.r * 0.6f),
                    Mathf.Clamp01(ci.data.tintColor.g * 0.6f),
                    Mathf.Clamp01(ci.data.tintColor.b * 0.6f), 1f);
                var legL = MakeLeg(go.transform, "LegL", legColor, new Vector2(-10f, -14f));
                var legR = MakeLeg(go.transform, "LegR", legColor, new Vector2( 10f, -14f));

                Color footColor = new Color(0.18f, 0.12f, 0.08f, 1f);
                var footL = MakeFoot(go.transform, "FootL", footColor, new Vector2(-10f, -36f));
                var footR = MakeFoot(go.transform, "FootR", footColor, new Vector2( 10f, -36f));

                // Body: tinted circle only if no portrait available
                var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(Image));
                bodyGO.transform.SetParent(go.transform, false);
                var bodyRt = bodyGO.GetComponent<RectTransform>();
                bodyRt.anchorMin = bodyRt.anchorMax = bodyRt.pivot = new Vector2(0.5f, 0.5f);
                bodyRt.anchoredPosition = new Vector2(0f, 22f);
                bodyRt.sizeDelta = new Vector2(66f, 66f);

                bool hasPortrait = frames != null && frames.Length == 1;
                if (hasPortrait)
                {
                    // Show portrait clipped to circle — no white background ring
                    var bodyImg2 = bodyGO.GetComponent<Image>();
                    bodyImg2.color = new Color(0f, 0f, 0f, 0f); // transparent body

                    var maskGO2 = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
                    maskGO2.transform.SetParent(bodyGO.transform, false);
                    var mrt2 = maskGO2.GetComponent<RectTransform>();
                    mrt2.anchorMin = Vector2.zero; mrt2.anchorMax = Vector2.one;
                    mrt2.offsetMin = mrt2.offsetMax = Vector2.zero;
                    var mImg2 = maskGO2.GetComponent<Image>();
                    mImg2.sprite = UiSpriteFactory.Circle();
                    mImg2.raycastTarget = false;
                    maskGO2.GetComponent<Mask>().showMaskGraphic = false;

                    var portGO2 = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                    portGO2.transform.SetParent(maskGO2.transform, false);
                    var prt2 = portGO2.GetComponent<RectTransform>();
                    prt2.anchorMin = Vector2.zero; prt2.anchorMax = Vector2.one;
                    prt2.offsetMin = prt2.offsetMax = Vector2.zero;
                    var portImg2 = portGO2.GetComponent<Image>();
                    portImg2.sprite = frames[0];
                    portImg2.raycastTarget = false;
                    portImg2.preserveAspect = true;
                }
                else
                {
                    // No image — use tinted circle (small, subtle)
                    var bodyImg2 = bodyGO.GetComponent<Image>();
                    bodyImg2.sprite = UiSpriteFactory.Circle();
                    bodyImg2.color = new Color(
                        ci.data.tintColor.r, ci.data.tintColor.g, ci.data.tintColor.b, 0.80f);
                    bodyImg2.raycastTarget = false;
                }

                var avatar = go.GetComponent<WorkerAvatar>();
                avatar.InitProcedural(rt, bodyRt, legL, legR, footL, footR, srt, RoamBounds);
            }

            return go.GetComponent<WorkerAvatar>();
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
        // Shared
        private RectTransform _rt;
        private RectTransform _shadow;
        private Rect _bounds;
        private Vector2 _target;
        private float _walkSpeed;
        private bool _walking;
        private float _idleTimer;
        private float _idleDuration;

        // Sprite-sheet mode
        private bool _sheetMode;
        private Image _bodyImg;
        private Sprite[] _frames;
        private RectTransform _bodyRt;
        private float _frameTimer;
        private int _frameIndex;
        private const float WalkFps = 10f;
        private const float IdleFps = 3f;

        // Procedural mode
        private RectTransform _legL, _legR, _footL, _footR;
        private RectTransform _bodyProcRt;
        private float _stepPhase;

        // ── Init ─────────────────────────────────────────────────────────────

        public void InitSheet(RectTransform rt, RectTransform bodyRt, Image bodyImg,
            Sprite[] frames, RectTransform shadow, Rect bounds)
        {
            _rt = rt; _bodyRt = bodyRt; _bodyImg = bodyImg;
            _frames = frames; _shadow = shadow; _bounds = bounds;
            _sheetMode = true;
            StartCoroutine(Lifecycle());
        }

        public void InitProcedural(RectTransform rt, RectTransform bodyRt,
            RectTransform legL, RectTransform legR,
            RectTransform footL, RectTransform footR,
            RectTransform shadow, Rect bounds)
        {
            _rt = rt; _bodyProcRt = bodyRt;
            _legL = legL; _legR = legR;
            _footL = footL; _footR = footR;
            _shadow = shadow; _bounds = bounds;
            _sheetMode = false;
            StartCoroutine(Lifecycle());
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private IEnumerator Lifecycle()
        {
            yield return new WaitForSeconds(Random.Range(0f, 3f));
            PickNewTarget();

            while (true)
            {
                if (_walking)
                {
                    yield return StartCoroutine(WalkToTarget());
                    _walking = false;
                    _idleDuration = Random.Range(2f, 6f);
                    _idleTimer = 0f;
                    SetIdlePose();
                }
                else
                {
                    _idleTimer += Time.deltaTime;
                    if (!_sheetMode) TickIdleBreath();
                    if (_idleTimer >= _idleDuration) PickNewTarget();
                }
                yield return null;
            }
        }

        private int _lastTargetIndex = -1;

        private void PickNewTarget()
        {
            var positions = OfficeWorkerManager.SpreadPositions;
            // Pick a different position from last one to avoid staying in place
            int idx;
            do { idx = Random.Range(0, positions.Length); }
            while (idx == _lastTargetIndex && positions.Length > 1);
            _lastTargetIndex = idx;
            _target = positions[idx] + new Vector2(Random.Range(-25f, 25f), Random.Range(-15f, 15f));
            _walkSpeed = Random.Range(55f, 85f);
            _walking = true;
        }

        // ── Walk ─────────────────────────────────────────────────────────────

        private IEnumerator WalkToTarget()
        {
            Vector2 start = _rt.anchoredPosition;
            float dist = Vector2.Distance(start, _target);
            if (dist < 4f) yield break;

            float duration = dist / _walkSpeed;
            float elapsed = 0f;
            bool facingRight = _target.x >= start.x;
            _rt.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);

            _stepPhase = 0f;
            float stepFreq = 2.5f;

            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                _stepPhase += dt * stepFreq * Mathf.PI * 2f;

                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = t * t * (3f - 2f * t);
                _rt.anchoredPosition = Vector2.Lerp(start, _target, smooth);

                if (_sheetMode) TickSheetWalk(dt);
                else AnimateWalk(_stepPhase);

                yield return null;
            }

            _rt.anchoredPosition = _target;
            _rt.localScale = Vector3.one;
        }

        // ── Sprite-sheet animation ────────────────────────────────────────────

        private void TickSheetWalk(float dt)
        {
            if (_frames == null || _frames.Length <= 1) return;
            _frameTimer += dt;
            if (_frameTimer >= 1f / WalkFps)
            {
                _frameTimer = 0f;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                _bodyImg.sprite = _frames[_frameIndex];
            }

            // Slight body bob based on frame
            float bob = Mathf.Sin(_frameIndex * Mathf.PI / 4f) * 2f;
            _bodyRt.anchoredPosition = new Vector2(0f, bob);
        }

        // ── Procedural animation ──────────────────────────────────────────────

        private void AnimateWalk(float phase)
        {
            float legSwingAmp = 12f;
            float legLiftAmp  = 7f;
            float bodyBobAmp  = 2.5f;
            float bodyTiltAmp = 4f;

            float sinL = Mathf.Sin(phase);
            float sinR = Mathf.Sin(phase + Mathf.PI);

            _legL.localRotation = Quaternion.Euler(0f, 0f, sinL * legSwingAmp);
            _legR.localRotation = Quaternion.Euler(0f, 0f, sinR * legSwingAmp);

            float liftL = Mathf.Max(0f, sinL) * legLiftAmp;
            float liftR = Mathf.Max(0f, sinR) * legLiftAmp;
            _footL.anchoredPosition = new Vector2(-10f, -36f + liftL);
            _footR.anchoredPosition = new Vector2( 10f, -36f + liftR);

            float scaleFL = 1f + Mathf.Max(0f, -sinL) * 0.25f;
            float scaleFR = 1f + Mathf.Max(0f, -sinR) * 0.25f;
            _footL.localScale = new Vector3(scaleFL, 1f / scaleFL, 1f);
            _footR.localScale = new Vector3(scaleFR, 1f / scaleFR, 1f);

            float bob = Mathf.Abs(Mathf.Sin(phase)) * bodyBobAmp;
            _bodyProcRt.anchoredPosition = new Vector2(0f, bob);
            _bodyProcRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * bodyTiltAmp);

            float shadowScale = 1f - bob * 0.03f;
            _shadow.localScale = new Vector3(shadowScale, 0.7f + bob * 0.02f, 1f);
        }

        private float _idleBreathPhase;

        private void TickIdleBreath()
        {
            _idleBreathPhase += Time.deltaTime * 1.2f;
            float breath = Mathf.Sin(_idleBreathPhase) * 1.2f;
            if (_bodyProcRt)
            {
                _bodyProcRt.anchoredPosition = new Vector2(0f, breath);
                _bodyProcRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_idleBreathPhase * 0.5f) * 0.8f);
            }
        }

        private void SetIdlePose()
        {
            if (_sheetMode)
            {
                if (_frames != null && _frames.Length > 0)
                    _bodyImg.sprite = _frames[0];
                _frameIndex = 0;
                if (_bodyRt) _bodyRt.anchoredPosition = Vector2.zero;
            }
            else
            {
                if (_legL) _legL.localRotation = Quaternion.identity;
                if (_legR) _legR.localRotation = Quaternion.identity;
                if (_footL) { _footL.anchoredPosition = new Vector2(-10f, -36f); _footL.localScale = Vector3.one; }
                if (_footR) { _footR.anchoredPosition = new Vector2( 10f, -36f); _footR.localScale = Vector3.one; }
                if (_bodyProcRt) { _bodyProcRt.anchoredPosition = Vector2.zero; _bodyProcRt.localRotation = Quaternion.identity; }
                if (_shadow) _shadow.localScale = Vector3.one;
            }
        }
    }
}

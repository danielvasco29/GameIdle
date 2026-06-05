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

        // Posições atualmente reservadas por algum worker (evita dois no mesmo spot).
        internal static readonly HashSet<int> ClaimedTargets = new();

        // Fixed spread positions so workers don't cluster — 10 spots across the office
        // 10 spots espalhados pelos landmarks e pelo chão aberto. Evita de
        // propósito o canto inferior-direito (mesa colada no botão TRABALHAR).
        // Com a reserva 1-por-spot, os personagens não se sobrepõem.
        // Coordenadas calibradas pelos landmarks reais do escritório (mapeadas a
        // partir do screenshot). y é "para cima"; o centro do chão fica perto de (20,55).
        public static readonly Vector2[] SpreadPositions =
        {
            // mesas (em pé trabalhando)
            new(-556f, 261f), // mesa esquerda (cima) — cadeira 1
            new(-420f, 235f), // mesa esquerda (cima) — cadeira 2
            new(-505f, -79f), // mesa esquerda (baixo) — cadeira 1
            new(-403f, -96f), // mesa esquerda (baixo) — cadeira 2
            // landmarks (em pé, na frente do objeto — sem animação de uso)
            new(-344f, 250f), // em frente ao bebedouro de água
            new( 350f,  10f), // em frente ao sofá (passando da mesinha de centro)
            // chão aberto (em pé, circulando)
            new(  20f,  55f), // centro
            new(-150f, 120f), // centro-fundo
            new( 180f,  20f), // centro-direita
            new(-200f, -120f), // frente-esquerda
            new(  -4f, 232f), // em frente à estante / lousa
        };

        // Quais spots são mesas (o personagem fica mais tempo "trabalhando").
        public static readonly bool[] IsSeat =
        {
            true,  true,  true,  true,
            false, false,
            false, false, false, false,
            false,
        };

        public void Init(RectTransform panelMain)
        {
            _panel = panelMain;
            ClaimedTargets.Clear(); // limpa reservas de uma sessão anterior (editor replay)
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

            // Strip baked-in white/gray/checkerboard backgrounds via flood-fill.
            tex = SpriteBackgroundRemover.Process(tex);

            int fh = tex.height;

            // Sprite sheet: width is at least 2x height
            if (tex.width < tex.height * 2)
                return new[] { Sprite.Create(tex, new Rect(0, 0, tex.width, fh), new Vector2(0.5f, 0.5f)) };

            // Auto-detect frame count from aspect ratio (width / height rounded)
            int frameCount = Mathf.Max(2, Mathf.RoundToInt((float)tex.width / tex.height));

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
            rt.sizeDelta = new Vector2(150f, 172f);

            // Start at exact spread position — no jitter to prevent overlap
            rt.anchoredPosition = SpreadPositions[index % SpreadPositions.Length];

            bool hasSpriteSheet = frames != null && frames.Length > 1;

            if (hasSpriteSheet)
            {
                // ── Sprite-sheet character ────────────────────────────────────
                // Sombra suave no chão
                var shadowGO = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                shadowGO.transform.SetParent(go.transform, false);
                var shRt = shadowGO.GetComponent<RectTransform>();
                shRt.anchorMin = shRt.anchorMax = shRt.pivot = new Vector2(0.5f, 0.5f);
                shRt.anchoredPosition = new Vector2(0f, -67f);
                shRt.sizeDelta = new Vector2(80f, 18f);
                var shImg = shadowGO.GetComponent<Image>();
                shImg.sprite = UiSpriteFactory.Circle();
                shImg.color = new Color(0f, 0f, 0f, 0.20f);
                shImg.raycastTarget = false;

                // Corpo direto — o shader descarta o fundo branco, sem círculo nem máscara
                var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(Image));
                bodyGO.transform.SetParent(go.transform, false);
                var bodyRt2 = bodyGO.GetComponent<RectTransform>();
                bodyRt2.anchorMin = bodyRt2.anchorMax = bodyRt2.pivot = new Vector2(0.5f, 0.5f);
                bodyRt2.anchoredPosition = Vector2.zero;
                bodyRt2.sizeDelta = new Vector2(150f, 150f);

                var bodyImg = bodyGO.GetComponent<Image>();
                bodyImg.sprite = frames[0];
                bodyImg.raycastTarget = false;
                bodyImg.preserveAspect = true;

                var avatar = go.GetComponent<WorkerAvatar>();
                avatar.InitSheet(rt, bodyRt2, bodyImg, frames, null, RoamBounds);
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
                bodyRt.anchoredPosition = new Vector2(0f, 28f);
                bodyRt.sizeDelta = new Vector2(88f, 88f);

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

            var wa = go.GetComponent<WorkerAvatar>();
            wa.ClaimSpawn(index % SpreadPositions.Length);
            wa.SetFacingDefault(FacesLeftByDefault(ci));
            return wa;
        }

        // Alguns sprites foram desenhados virados para a esquerda; para esses o
        // espelhamento precisa ser invertido (senão andam "de costas").
        private static bool FacesLeftByDefault(CharacterInstance ci)
        {
            string nm = (ci.data.characterName ?? "").Trim().ToUpper();
            string id = (ci.data.characterId ?? "").Trim().ToUpper();
            return nm == "CEO" || id == "CEO";
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

        // Sentar / trabalhar
        private bool _seatedTarget; // o destino atual é uma cadeira?
        private bool _seated;       // está sentado agora?
        private float _workPhase;

        // Orientação padrão do sprite (alguns foram desenhados virados p/ esquerda).
        private bool _faceLeftDefault;
        public void SetFacingDefault(bool faceLeft) => _faceLeftDefault = faceLeft;
        // Sinal do scale.x para o personagem encarar a direção desejada.
        private float FaceX(bool right) => (right ^ _faceLeftDefault) ? 1f : -1f;

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
                    _seated = _seatedTarget;
                    // Na mesa: fica mais tempo "trabalhando"; em pé livre: pausa curta.
                    _idleDuration = _seated ? Random.Range(7f, 14f) : Random.Range(2f, 6f);
                    _idleTimer = 0f;
                    SetIdlePose(); // sempre em pé (sprites só têm pose em pé)
                }
                else
                {
                    _idleTimer += Time.deltaTime;
                    if (_seated) TickWorking();
                    else if (!_sheetMode) TickIdleBreath();
                    if (_idleTimer >= _idleDuration) PickNewTarget();
                }
                yield return null;
            }
        }

        private int _lastTargetIndex = -1;
        private int _claimedIndex = -1;

        // Reserva o spot inicial para que nenhum outro worker o tome.
        public void ClaimSpawn(int index)
        {
            _claimedIndex = index;
            _lastTargetIndex = index;
            OfficeWorkerManager.ClaimedTargets.Add(index);
        }

        private void OnDestroy()
        {
            if (_claimedIndex >= 0)
                OfficeWorkerManager.ClaimedTargets.Remove(_claimedIndex);
        }

        private void PickNewTarget()
        {
            var positions = OfficeWorkerManager.SpreadPositions;
            var claimed = OfficeWorkerManager.ClaimedTargets;

            // Libera o spot atual antes de procurar um novo livre.
            if (_claimedIndex >= 0) claimed.Remove(_claimedIndex);

            // Escolhe um spot que não esteja reservado por outro worker (e != atual).
            int idx = -1;
            int tries = 0;
            do
            {
                int cand = Random.Range(0, positions.Length);
                if (cand != _lastTargetIndex && !claimed.Contains(cand)) { idx = cand; break; }
            }
            while (++tries < 40);

            // Fallback: se tudo reservado, pega qualquer um diferente do atual.
            if (idx < 0)
            {
                do { idx = Random.Range(0, positions.Length); }
                while (idx == _lastTargetIndex && positions.Length > 1);
            }

            _claimedIndex = idx;
            _lastTargetIndex = idx;
            claimed.Add(idx);
            _seatedTarget = OfficeWorkerManager.IsSeat[idx];

            // Cadeira: sem jitter (encaixa na cadeira); em pé: leve variação.
            var jitter = _seatedTarget ? Vector2.zero
                                       : new Vector2(Random.Range(-10f, 10f), Random.Range(-8f, 8f));
            _target = positions[idx] + jitter;
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
            _rt.localScale = new Vector3(FaceX(facingRight), 1f, 1f);

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
            _rt.localScale = new Vector3(FaceX(facingRight), 1f, 1f);
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

        // Micro-movimento de "digitando" em pé na mesa (leve balanço do corpo).
        private void TickWorking()
        {
            _workPhase += Time.deltaTime * 3.2f;
            float b = Mathf.Sin(_workPhase) * 1.3f;
            if (_sheetMode)
            {
                if (_bodyRt) _bodyRt.anchoredPosition = new Vector2(0f, b);
            }
            else if (_bodyProcRt)
            {
                _bodyProcRt.anchoredPosition = new Vector2(0f, Mathf.Abs(b) * 0.6f);
                _bodyProcRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_workPhase * 0.6f) * 1.4f);
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

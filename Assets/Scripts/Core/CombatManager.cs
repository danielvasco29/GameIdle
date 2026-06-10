using System;
using System.Collections;
using UnityEngine;

namespace GameIdle
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        // ── Monster definitions ───────────────────────────────────────────────
        public enum MonsterType { Bug, Virus, Hacker, Boss }

        public struct MonsterDef
        {
            public MonsterType type;
            public string displayName;
            public string spritePath;    // Resources path
            public double baseHp;
            public double baseReward;
        }

        private static readonly MonsterDef[] Defs = {
            new() { type = MonsterType.Bug,    displayName = "BUG GIGANTE",    spritePath = "Monsters/monster_bug",    baseHp = 80,   baseReward = 60   },
            new() { type = MonsterType.Virus,  displayName = "VIRUS MALIGNO",  spritePath = "Monsters/monster_virus",  baseHp = 200,  baseReward = 200  },
            new() { type = MonsterType.Hacker, displayName = "HACKER SOMBRIO", spritePath = "Monsters/monster_hacker", baseHp = 500,  baseReward = 600  },
            new() { type = MonsterType.Boss,   displayName = "DEADLINE DRAGON",spritePath = "Monsters/monster_boss",   baseHp = 2000, baseReward = 3000 },
        };

        // ── State ─────────────────────────────────────────────────────────────
        public bool IsActive    { get; private set; }
        public double CurrentHp { get; private set; }
        public double MaxHp     { get; private set; }
        public int    Wave      { get; private set; } = 1;
        public int    Cycle     { get; private set; } = 1;  // increases after wave 10
        public MonsterDef CurrentDef { get; private set; }

        // Auto-attack from workers (damage per second)
        private float _autoAttackTimer;
        private float _autoAttackInterval = 2.5f;
        private bool  _betweenWaves;
        private float _betweenWavesTimer;
        private const float BetweenWavesDelay = 2.5f;

        // Events
        public event Action<double, double> OnHpChanged;   // (currentHp, maxHp)
        public event Action<MonsterDef>     OnWaveStarted;
        public event Action<double>         OnMonsterDied; // reward
        public event Action<double>         OnPlayerDamage;// dmg dealt

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            IsActive = true;
            StartWave(1);
        }

        private void Update()
        {
            if (!IsActive || _betweenWaves) return;

            _autoAttackTimer += Time.deltaTime;
            if (_autoAttackTimer >= _autoAttackInterval)
            {
                _autoAttackTimer = 0f;
                double autoDmg = CalcAutoAttackDamage();
                if (autoDmg > 0) DealDamage(autoDmg, fromPlayer: false);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void PlayerAttack()
        {
            if (!IsActive || _betweenWaves) return;
            double dmg = CalcPlayerDamage();
            DealDamage(dmg, fromPlayer: true);
            OnPlayerDamage?.Invoke(dmg);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void StartWave(int wave)
        {
            Wave = wave;
            _betweenWaves = false;
            _autoAttackTimer = 0f;

            var def = DefForWave(wave);
            CurrentDef = def;

            double mult = 1.0 + (Cycle - 1) * 0.8 + (wave - 1) * 0.15;
            MaxHp     = Math.Floor(def.baseHp  * mult);
            CurrentHp = MaxHp;

            OnWaveStarted?.Invoke(CurrentDef);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        private void DealDamage(double dmg, bool fromPlayer)
        {
            CurrentHp -= dmg;
            if (CurrentHp < 0) CurrentHp = 0;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);

            if (CurrentHp <= 0)
                StartCoroutine(KillMonster());
        }

        private IEnumerator KillMonster()
        {
            double mult  = 1.0 + (Cycle - 1) * 0.8 + (Wave - 1) * 0.15;
            double reward = Math.Floor(CurrentDef.baseReward * mult);

            GameManager.Instance.AddMoney(reward);
            OnMonsterDied?.Invoke(reward);

            _betweenWaves = true;
            yield return new WaitForSeconds(BetweenWavesDelay);

            int nextWave = Wave + 1;
            if (nextWave > 10)
            {
                nextWave = 1;
                Cycle++;
            }
            StartWave(nextWave);
        }

        private double CalcPlayerDamage()
        {
            // Base 5% of monster max HP, scaled by tap power
            double tapPower = GameManager.Instance != null
                ? Math.Max(1, Math.Log10(GameManager.Instance.Money + 10) * 2)
                : 1;
            return Math.Max(1, MaxHp * 0.05 * tapPower);
        }

        private double CalcAutoAttackDamage()
        {
            // Workers deal 1% of max HP per tick, scaled by hired count
            int workers = 0;
            if (CharacterManager.Instance != null)
                foreach (var c in CharacterManager.Instance.GetAllCharacters())
                    if (c.purchaseCount > 0) workers++;
            if (workers == 0) return 0;
            return Math.Max(1, MaxHp * 0.01 * workers);
        }

        private static MonsterDef DefForWave(int wave)
        {
            if (wave == 10) return Defs[3]; // Boss
            if (wave >= 7)  return Defs[2]; // Hacker
            if (wave >= 4)  return Defs[1]; // Virus
            return Defs[0];                  // Bug
        }
    }
}

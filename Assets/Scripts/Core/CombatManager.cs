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

        // ── Upgrade Levels ────────────────────────────────────────────────────
        public static int SwordLevel { get; private set; }   // 0-10
        public static int ArmorLevel { get; private set; }   // 0-5
        public static int FrostLevel { get; private set; }   // 0-3

        // Upgrade costs (base)
        private const double SwordBaseCost  = 500.0;
        private const double ArmorBaseCost  = 1000.0;
        private const double FrostBaseCost  = 5000.0;
        private const double PotionBaseCost = 2000.0;

        // Upgrade max levels
        public const int SwordMax  = 10;
        public const int ArmorMax  = 5;
        public const int FrostMax  = 3;

        // Potion (active ability)
        public static bool  PotionUnlocked   { get; private set; }
        public static bool  PotionActive     { get; private set; }
        public static float PotionRemaining  { get; private set; }
        public static float PotionCooldown   { get; private set; }
        private const float PotionDuration   = 30f;
        private const float PotionCooldownDuration = 300f; // 5 min

        // ── Upgrade Multipliers ───────────────────────────────────────────────
        public static double GetPlayerDamageMultiplier()   => 1.0 + SwordLevel * 0.25;
        public static double GetAutoAttackInterval()       => Math.Max(1.0, 2.5 - ArmorLevel * 0.15 * 2.5);
        public static double GetBossExtraDamage()          => FrostLevel * 0.20;
        public static double GetPotionMultiplier()         => PotionActive ? 2.0 : 1.0;

        public static double GetSwordCost()  => Math.Floor(SwordBaseCost  * Math.Pow(2.5, SwordLevel));
        public static double GetArmorCost()  => Math.Floor(ArmorBaseCost  * Math.Pow(2.5, ArmorLevel));
        public static double GetFrostCost()  => Math.Floor(FrostBaseCost  * Math.Pow(3.0, FrostLevel));
        public static double GetPotionCost() => PotionBaseCost;

        // ── Upgrade Methods ───────────────────────────────────────────────────
        public static void UpgradeSword()
        {
            if (SwordLevel >= SwordMax || GameManager.Instance == null) return;
            double cost = GetSwordCost();
            if (GameManager.Instance.Money < cost) return;
            GameManager.Instance.AddMoney(-cost);
            SwordLevel++;
            PlayerPrefs.SetInt("cbt_sword", SwordLevel);
            PlayerPrefs.Save();
        }

        public static void UpgradeArmor()
        {
            if (ArmorLevel >= ArmorMax || GameManager.Instance == null) return;
            double cost = GetArmorCost();
            if (GameManager.Instance.Money < cost) return;
            GameManager.Instance.AddMoney(-cost);
            ArmorLevel++;
            if (Instance != null)
                Instance._autoAttackInterval = (float)GetAutoAttackInterval();
            PlayerPrefs.SetInt("cbt_armor", ArmorLevel);
            PlayerPrefs.Save();
        }

        public static void UpgradeFrost()
        {
            if (FrostLevel >= FrostMax || GameManager.Instance == null) return;
            double cost = GetFrostCost();
            if (GameManager.Instance.Money < cost) return;
            GameManager.Instance.AddMoney(-cost);
            FrostLevel++;
            PlayerPrefs.SetInt("cbt_frost", FrostLevel);
            PlayerPrefs.Save();
        }

        public static void BuyPotion()
        {
            if (PotionUnlocked || GameManager.Instance == null) return;
            double cost = GetPotionCost();
            if (GameManager.Instance.Money < cost) return;
            GameManager.Instance.AddMoney(-cost);
            PotionUnlocked = true;
            PlayerPrefs.SetInt("cbt_potion", 1);
            PlayerPrefs.Save();
        }

        public static void ActivatePotion()
        {
            if (!PotionUnlocked || PotionActive || PotionCooldown > 0f) return;
            PotionActive    = true;
            PotionRemaining = PotionDuration;
        }

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
            // Load upgrade levels
            SwordLevel    = PlayerPrefs.GetInt("cbt_sword",  0);
            ArmorLevel    = PlayerPrefs.GetInt("cbt_armor",  0);
            FrostLevel    = PlayerPrefs.GetInt("cbt_frost",  0);
            PotionUnlocked = PlayerPrefs.GetInt("cbt_potion", 0) == 1;

            _autoAttackInterval = (float)GetAutoAttackInterval();

            IsActive = true;
            StartWave(1);
        }

        private void Update()
        {
            // Potion timer
            if (PotionActive)
            {
                PotionRemaining -= Time.deltaTime;
                if (PotionRemaining <= 0f)
                {
                    PotionActive   = false;
                    PotionCooldown = PotionCooldownDuration;
                }
            }
            else if (PotionCooldown > 0f)
            {
                PotionCooldown -= Time.deltaTime;
                if (PotionCooldown < 0f) PotionCooldown = 0f;
            }

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
            SoundManager.Get().PlayHit();
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

            if (def.type == MonsterType.Boss)
                SoundManager.Get().PlayBossRoar();

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
            bool isBoss = CurrentDef.type == MonsterType.Boss;
            double mult   = 1.0 + (Cycle - 1) * 0.8 + (Wave - 1) * 0.15;
            double reward = Math.Floor(CurrentDef.baseReward * mult);

            // Kill tracking
            GameManager.Instance.AddMoney(reward);
            GameManager.Instance.RegisterKill(isBoss);
            DailyMissionSystem.RegisterKill();
            AchievementManager.CheckAll();

            // SFX
            SoundManager.Get().PlayDeath();

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
            // Base 5% of monster max HP, scaled by tap power + sword upgrade + potion
            double tapPower = GameManager.Instance != null
                ? Math.Max(1, Math.Log10(GameManager.Instance.Money + 10) * 2)
                : 1;
            double baseDmg = Math.Max(1, MaxHp * 0.05 * tapPower);
            double frostMult = (CurrentDef.type == MonsterType.Boss) ? (1.0 + GetBossExtraDamage()) : 1.0;
            return baseDmg * GetPlayerDamageMultiplier() * GetPotionMultiplier() * frostMult;
        }

        private double CalcAutoAttackDamage()
        {
            // Workers deal 1% of max HP per tick, scaled by hired count + potion
            int workers = 0;
            if (CharacterManager.Instance != null)
                foreach (var c in CharacterManager.Instance.GetAllCharacters())
                    if (c.level > 0) workers++;
            if (workers == 0) return 0;
            double frostMult = (CurrentDef.type == MonsterType.Boss) ? (1.0 + GetBossExtraDamage()) : 1.0;
            return Math.Max(1, MaxHp * 0.01 * workers) * GetPotionMultiplier() * frostMult;
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

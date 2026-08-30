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
            // Cycle 1 roster
            new() { type = MonsterType.Bug,    displayName = "BUG GIGANTE",    spritePath = "Monsters/monster_bug",         baseHp = 800,    baseReward = 90    },
            new() { type = MonsterType.Virus,  displayName = "VIRUS MALIGNO",  spritePath = "Monsters/monster_virus",       baseHp = 2_000,  baseReward = 260   },
            new() { type = MonsterType.Hacker, displayName = "HACKER SOMBRIO", spritePath = "Monsters/monster_hacker",      baseHp = 5_000,  baseReward = 800   },
            new() { type = MonsterType.Boss,   displayName = "DEADLINE DRAGON",spritePath = "Monsters/monster_boss",        baseHp = 20_000, baseReward = 3_500 },
            // Cycle 2+ roster (animated sprite sheets)
            new() { type = MonsterType.Bug,    displayName = "WORM CORRUPTO",  spritePath = "Monsters/monster_worm_sheet",  baseHp = 1_200,  baseReward = 140   },
            new() { type = MonsterType.Virus,  displayName = "GOLEM DE DADOS", spritePath = "Monsters/monster_golem_sheet", baseHp = 3_500,  baseReward = 460   },
            new() { type = MonsterType.Hacker, displayName = "ESPECTRO CYBER", spritePath = "Monsters/monster_ghost_sheet", baseHp = 8_000,  baseReward = 1_250 },
        };

        // ── Upgrade Levels ────────────────────────────────────────────────────
        public static int SwordLevel   { get; private set; }  // 0-10
        public static int ArmorLevel   { get; private set; }  // 0-5
        public static int FrostLevel   { get; private set; }  // 0-3
        public static int CriticoLevel { get; private set; }  // 0-5

        // Upgrade costs (base)
        private const double SwordBaseCost   = 300.0;
        private const double ArmorBaseCost   = 600.0;
        private const double FrostBaseCost   = 3000.0;
        private const double PotionBaseCost  = 1200.0;
        private const double CriticoBaseCost = 800.0;

        // Upgrade max levels
        public const int SwordMax   = 10;
        public const int ArmorMax   = 5;
        public const int FrostMax   = 3;
        public const int CriticoMax = 5;

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
        public static float  GetCriticoChance()            => CriticoLevel * 0.10f; // +10% per level

        public static double GetSwordCost()   => Math.Floor(SwordBaseCost   * Math.Pow(2.2, SwordLevel));
        public static double GetArmorCost()   => Math.Floor(ArmorBaseCost   * Math.Pow(2.2, ArmorLevel));
        public static double GetFrostCost()   => Math.Floor(FrostBaseCost   * Math.Pow(3.0, FrostLevel));
        public static double GetPotionCost()  => PotionBaseCost;
        public static double GetCriticoCost() => Math.Floor(CriticoBaseCost * Math.Pow(2.5, CriticoLevel));

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

        public static void UpgradeCritico()
        {
            if (CriticoLevel >= CriticoMax || GameManager.Instance == null) return;
            double cost = GetCriticoCost();
            if (GameManager.Instance.Money < cost) return;
            GameManager.Instance.AddMoney(-cost);
            CriticoLevel++;
            PlayerPrefs.SetInt("cbt_critico", CriticoLevel);
            PlayerPrefs.Save();
        }

        public static void ActivatePotion()
        {
            if (!PotionUnlocked || PotionActive || PotionCooldown > 0f) return;
            PotionActive    = true;
            PotionRemaining = PotionDuration;
        }

        // Recompensa base da wave sem depender da instancia (usado no calculo offline,
        // que roda antes do Start do CombatManager).
        public static double GetBaseRewardFor(int cycle, int wave)
        {
            if (wave == 10) return Defs[3].baseReward;
            int offset = (cycle % 2 == 0) ? 4 : 0;
            if (wave >= 7) return Defs[offset + 2].baseReward;
            if (wave >= 4) return Defs[offset + 1].baseReward;
            return Defs[offset].baseReward;
        }

        // Carrega os niveis vindos do save JSON. Se o save ainda nao tem nada
        // (jogo antigo), cai pro PlayerPrefs para nao perder progresso.
        public static void LoadUpgradeLevels(int sword, int armor, int frost, int critico, bool potion)
        {
            bool saveVazio = sword == 0 && armor == 0 && frost == 0 && critico == 0 && !potion;
            if (saveVazio)
            {
                sword   = PlayerPrefs.GetInt("cbt_sword",   0);
                armor   = PlayerPrefs.GetInt("cbt_armor",   0);
                frost   = PlayerPrefs.GetInt("cbt_frost",   0);
                critico = PlayerPrefs.GetInt("cbt_critico", 0);
                potion  = PlayerPrefs.GetInt("cbt_potion",  0) == 1;
            }

            SwordLevel     = Math.Clamp(sword,   0, SwordMax);
            ArmorLevel     = Math.Clamp(armor,   0, ArmorMax);
            FrostLevel     = Math.Clamp(frost,   0, FrostMax);
            CriticoLevel   = Math.Clamp(critico, 0, CriticoMax);
            PotionUnlocked = potion;

            if (Instance != null)
                Instance._autoAttackInterval = (float)GetAutoAttackInterval();
        }

        public static void ResetPersistentProgress()
        {
            SwordLevel = 0;
            ArmorLevel = 0;
            FrostLevel = 0;
            CriticoLevel = 0;
            PotionUnlocked = false;
            PotionActive = false;
            PotionRemaining = 0f;
            PotionCooldown = 0f;

            PlayerPrefs.DeleteKey("cbt_sword");
            PlayerPrefs.DeleteKey("cbt_armor");
            PlayerPrefs.DeleteKey("cbt_frost");
            PlayerPrefs.DeleteKey("cbt_potion");
            PlayerPrefs.DeleteKey("cbt_critico");
            PlayerPrefs.Save();

            if (Instance != null)
                Instance._autoAttackInterval = (float)GetAutoAttackInterval();
        }

        // ── State ─────────────────────────────────────────────────────────────
        public bool IsActive    { get; private set; }
        public double CurrentHp { get; private set; }
        public double MaxHp     { get; private set; }
        public int    Wave      { get; private set; } = 1;
        public int    Cycle     { get; set; } = 1;  // increases after wave 10; set on load
        public MonsterDef CurrentDef { get; private set; }

        // Combo tracking
        public int   ComboCount  { get; private set; }
        public float ComboTimer  { get; private set; }  // seconds since last tap
        private const float ComboWindow = 2.0f;         // reset if no tap within 2s
        private const int   ComboMax    = 10;

        public event Action<int> OnComboChanged;  // fires with new combo count

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
            // Niveis ja foram carregados por GameManager.ApplySaveData (save JSON).
            _autoAttackInterval = (float)GetAutoAttackInterval();

            // Restore combat progress from save
            if (GameManager.Instance != null)
            {
                Cycle = Math.Max(1, GameManager.Instance.SavedCombatCycle);
                int wave = Math.Max(1, Math.Min(10, GameManager.Instance.SavedCombatWave));
                IsActive = true;
                StartWave(wave);
            }
            else
            {
                IsActive = true;
                StartWave(1);
            }
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

            if (ComboCount > 0)
            {
                ComboTimer += Time.deltaTime;
                if (ComboTimer >= ComboWindow)
                {
                    ComboCount = 0;
                    ComboTimer = 0f;
                    OnComboChanged?.Invoke(0);
                }
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
            ComboCount = Math.Min(ComboCount + 1, ComboMax);
            ComboTimer = 0f;
            OnComboChanged?.Invoke(ComboCount);
            AchievementManager.CheckCombo(ComboCount);
            double dmg = CalcPlayerDamage();
            DealDamage(dmg, fromPlayer: true);
            SoundManager.Get().PlayHit();
            OnPlayerDamage?.Invoke(dmg);
        }

        // Reinicia o combate do zero — chamado pelo HardReset.
        public void ForceReset()
        {
            Cycle = 1;
            StartWave(1);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void StartWave(int wave)
        {
            Wave = wave;
            _betweenWaves = false;
            _autoAttackTimer = 0f;

            var def = DefForWave(wave);
            CurrentDef = def;

            double mult = 1.0 + (Cycle - 1) * 1.2 + (wave - 1) * 0.20;
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
            double rewardMult = 1.0 + (Cycle - 1) * 1.2 + (Wave - 1) * 0.20;
            double reward = Math.Floor(CurrentDef.baseReward * rewardMult * GemShop.GetKillBonusMult());

            // Kill tracking
            GameManager.Instance.AddMoney(reward);
            GameManager.Instance.RegisterKill(isBoss);
            DailyMissionSystem.RegisterKill();
            AchievementManager.CheckAll();

            // Reset combo on kill
            ComboCount = 0;
            ComboTimer = 0f;
            OnComboChanged?.Invoke(0);

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
            double moneyPower = GameManager.Instance != null
                ? Math.Min(8.0, Math.Max(1.0, Math.Log10(GameManager.Instance.Money + 10)))
                : 1.0;
            double cyclePower = 1.0 + (Cycle - 1) * 0.08;
            double baseDmg = (18.0 + moneyPower * 6.0) * cyclePower;
            double frostMult = (CurrentDef.type == MonsterType.Boss) ? (1.0 + GetBossExtraDamage()) : 1.0;
            double dmg = baseDmg * GetPlayerDamageMultiplier() * GetPotionMultiplier() * frostMult;
            // Critical hit
            if (CriticoLevel > 0 && UnityEngine.Random.value < GetCriticoChance())
                dmg *= 2.0;
            double comboMult = 1.0 + ComboCount * 0.10; // +10% per combo hit
            dmg *= comboMult;
            return dmg;
        }

        private double CalcAutoAttackDamage()
        {
            // Workers deal flat damage so stronger monsters actually take longer.
            int workers = 0;
            if (CharacterManager.Instance != null)
                foreach (var c in CharacterManager.Instance.GetAllCharacters())
                    if (c.level > 0) workers++;
            if (workers == 0) return 0;
            double officePower = GameManager.Instance != null
                ? Math.Min(10.0, Math.Max(1.0, Math.Log10(GameManager.Instance.MoneyPerSecond + 10)))
                : 1.0;
            double baseDmg = workers * (4.0 + officePower * 1.5) * (1.0 + (Cycle - 1) * 0.05);
            double frostMult = (CurrentDef.type == MonsterType.Boss) ? (1.0 + GetBossExtraDamage()) : 1.0;
            return Math.Max(1, baseDmg) * GetPotionMultiplier() * frostMult * GemShop.GetCombatShieldMult();
        }

        private MonsterDef DefForWave(int wave)
        {
            if (wave == 10) return Defs[3]; // Boss always on wave 10
            // Odd cycles (1,3,5…) use Bug/Virus/Hacker; even cycles (2,4,6…) use Worm/Golem/Ghost
            bool altRoster = (Cycle % 2 == 0);
            int offset = altRoster ? 4 : 0;
            if (wave >= 7) return Defs[offset + 2];
            if (wave >= 4) return Defs[offset + 1];
            return Defs[offset];
        }
    }
}

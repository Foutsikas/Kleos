using Godot;
using System;

public partial class BattleSystem : Node
{
    // --- Singleton ---

    public static BattleSystem Instance { get; private set; }

    // --- C# Events (not Godot signals -- plain C# classes as parameters) ---

    public event Action<BattleContext> BattleStarted;
    public event Action<int> RoundStarted;
    public event Action<BattleLogEntry> HeroAttackOccurred;
    public event Action<BattleLogEntry> EnemyAttackOccurred;
    public event Action<BattleResult> BattleEnded;

    // --- State ---

    private BattleContext currentContext;
    private float enemyCurrentHP;
    private float enemyMaxHP;
    private int currentRound;
    private bool isBattleActive;

    // --- Battle Stats (tracked for result screen) ---

    private int totalRounds;
    private int heroCritsLanded;
    private int heroDodgesPerformed;
    private float heroHPAtEnd;
    private float enemyHPAtEnd;

    // --- Timing ---

    private float speedMultiplier = 1.0f;

    // Base delay between attacks within a round (seconds)
    private const float BaseAttackDelay = 0.4f;
    // Delay between rounds (seconds)
    private const float BaseRoundDelay = 0.4f;
    // Pause before first round starts (seconds)
    private const float BaseSetupPause = 0.8f;
    // Pause after final blow before result screen (seconds)
    private const float BaseFinalBlowPause = 0.6f;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;

        // Listen for random encounters
        RandomEncounterManager.Instance.EncounterTriggered += OnRandomEncounterTriggered;
    }

    // -------------------------------------------------------------------------
    // Public API: Start Battle
    // -------------------------------------------------------------------------

    public void StartDungeonBattle(DungeonData dungeon, int layerIndex)
    {
        if (isBattleActive)
        {
            GD.Print("[BattleSystem] Battle already in progress, ignoring request.");
            return;
        }

        DungeonLayer layer = dungeon.GetLayer(layerIndex);
        if (layer == null)
        {
            GD.PrintErr($"[BattleSystem] Invalid layer index {layerIndex} for {dungeon.DungeonName}.");
            return;
        }

        var context = BattleContext.CreateDungeonBattle(dungeon, layerIndex, layer);
        BeginBattle(context);
    }

    public void StartRandomEncounterBattle(EnemyData enemy, string poolName)
    {
        if (isBattleActive)
        {
            GD.Print("[BattleSystem] Battle already in progress, ignoring request.");
            return;
        }

        var context = BattleContext.CreateRandomEncounter(enemy, poolName);
        BeginBattle(context);
    }

    // -------------------------------------------------------------------------
    // Public API: Speed Control
    // -------------------------------------------------------------------------

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(1.0f, multiplier);
    }

    // -------------------------------------------------------------------------
    // Core Battle Flow
    // -------------------------------------------------------------------------

    private async void BeginBattle(BattleContext context)
    {
        currentContext = context;
        isBattleActive = true;

        // Initialize enemy HP
        enemyMaxHP = context.Enemy.Health;
        enemyCurrentHP = enemyMaxHP;

        // Restore hero HP before battle
        HeroManager.Instance.RestoreFullHP();

        // Reset tracking
        currentRound = 0;
        totalRounds = 0;
        heroCritsLanded = 0;
        heroDodgesPerformed = 0;
        speedMultiplier = 1.0f;

        GD.Print("===========================================");
        GD.Print($"[BattleSystem] BATTLE START: {context.HeaderText}");
        GD.Print($"  Hero: HP {HeroManager.Instance.GetCurrentHP()}/{HeroManager.Instance.GetMaxHP()} | " +
                 $"DMG {HeroManager.Instance.GetDamage():F1} | " +
                 $"Dodge {HeroManager.Instance.GetDodgeChance() * 100:F1}% | " +
                 $"Crit {HeroManager.Instance.GetCritChance() * 100:F1}%");
        GD.Print($"  Enemy: {context.Enemy.EnemyName} | " +
                 $"HP {enemyCurrentHP}/{enemyMaxHP} | " +
                 $"DPS {context.Enemy.DamagePerSecond} | " +
                 $"Rate {context.Enemy.AttackRate}");
        GD.Print("-------------------------------------------");

        // Notify UI
        BattleStarted?.Invoke(context);

        // Setup pause before combat begins
        await ToSignal(GetTree().CreateTimer(BaseSetupPause / speedMultiplier), SceneTreeTimer.SignalName.Timeout);

        if (!isBattleActive) return;

        // Run combat round by round
        await RunCombatAsync();
    }

    private async System.Threading.Tasks.Task RunCombatAsync()
    {
        while (isBattleActive)
        {
            currentRound++;
            totalRounds = currentRound;

            GD.Print($"  --- Round {currentRound} ---");
            RoundStarted?.Invoke(currentRound);

            // Hero attacks first
            var heroEntry = ExecuteHeroAttack();
            HeroAttackOccurred?.Invoke(heroEntry);

            // Check if enemy is dead
            if (enemyCurrentHP <= 0)
            {
                heroHPAtEnd = HeroManager.Instance.GetCurrentHP();
                enemyHPAtEnd = 0;

                // Final blow pause
                await ToSignal(GetTree().CreateTimer(BaseFinalBlowPause / speedMultiplier), SceneTreeTimer.SignalName.Timeout);

                if (!isBattleActive) return;
                OnHeroVictory();
                return;
            }

            // Delay between hero and enemy attack
            await ToSignal(GetTree().CreateTimer(BaseAttackDelay / speedMultiplier), SceneTreeTimer.SignalName.Timeout);

            if (!isBattleActive) return;

            // Enemy retaliates
            var enemyEntry = ExecuteEnemyAttack();
            EnemyAttackOccurred?.Invoke(enemyEntry);

            // Check if hero is dead
            if (!HeroManager.Instance.IsAlive())
            {
                heroHPAtEnd = 0;
                enemyHPAtEnd = enemyCurrentHP;

                // Final blow pause
                await ToSignal(GetTree().CreateTimer(BaseFinalBlowPause / speedMultiplier), SceneTreeTimer.SignalName.Timeout);

                if (!isBattleActive) return;
                OnHeroDefeat();
                return;
            }

            // Delay between rounds
            await ToSignal(GetTree().CreateTimer(BaseRoundDelay / speedMultiplier), SceneTreeTimer.SignalName.Timeout);

            if (!isBattleActive) return;
        }
    }

    // -------------------------------------------------------------------------
    // Hero Attack
    // -------------------------------------------------------------------------

    private BattleLogEntry ExecuteHeroAttack()
    {
        float baseDamage = HeroManager.Instance.GetDamage();
        bool isCrit = HeroManager.Instance.RollCrit();

        float finalDamage = baseDamage;
        if (isCrit)
        {
            float critMultiplier = HeroManager.Instance.GetCritMultiplier();
            finalDamage = baseDamage * critMultiplier;
            heroCritsLanded++;
        }

        // Apply damage to enemy
        enemyCurrentHP -= finalDamage;
        if (enemyCurrentHP < 0) enemyCurrentHP = 0;

        // Build log entry
        var entry = new BattleLogEntry
        {
            IsHeroAction = true,
            Damage = finalDamage,
            IsCritical = isCrit,
            IsDodge = false,
            ActorName = "Hero",
            TargetName = currentContext.Enemy.EnemyName,
            TargetCurrentHP = enemyCurrentHP,
            TargetMaxHP = enemyMaxHP
        };

        if (isCrit)
            GD.Print($"  Hero CRIT! {finalDamage:F1} damage -> {currentContext.Enemy.EnemyName} " +
                     $"({enemyCurrentHP:F0}/{enemyMaxHP:F0} HP)");
        else
            GD.Print($"  Hero attacks for {finalDamage:F1} damage -> {currentContext.Enemy.EnemyName} " +
                     $"({enemyCurrentHP:F0}/{enemyMaxHP:F0} HP)");

        return entry;
    }

    // -------------------------------------------------------------------------
    // Enemy Attack
    // -------------------------------------------------------------------------

    private BattleLogEntry ExecuteEnemyAttack()
    {
        // Check dodge first
        bool dodged = HeroManager.Instance.RollDodge();

        if (dodged)
        {
            heroDodgesPerformed++;

            var dodgeEntry = new BattleLogEntry
            {
                IsHeroAction = false,
                Damage = 0,
                IsCritical = false,
                IsDodge = true,
                ActorName = currentContext.Enemy.EnemyName,
                TargetName = "Hero",
                TargetCurrentHP = HeroManager.Instance.GetCurrentHP(),
                TargetMaxHP = HeroManager.Instance.GetMaxHP()
            };

            GD.Print($"  {currentContext.Enemy.EnemyName} attacks -> Hero DODGES!");
            return dodgeEntry;
        }

        // Calculate enemy damage
        float damagePerHit = currentContext.Enemy.DamagePerSecond / currentContext.Enemy.AttackRate;

        HeroManager.Instance.TakeDamage(damagePerHit);

        var entry = new BattleLogEntry
        {
            IsHeroAction = false,
            Damage = damagePerHit,
            IsCritical = false,
            IsDodge = false,
            ActorName = currentContext.Enemy.EnemyName,
            TargetName = "Hero",
            TargetCurrentHP = HeroManager.Instance.GetCurrentHP(),
            TargetMaxHP = HeroManager.Instance.GetMaxHP()
        };

        GD.Print($"  {currentContext.Enemy.EnemyName} attacks for {damagePerHit:F1} damage -> Hero " +
                 $"({HeroManager.Instance.GetCurrentHP():F0}/{HeroManager.Instance.GetMaxHP():F0} HP)");

        return entry;
    }

    // -------------------------------------------------------------------------
    // Victory
    // -------------------------------------------------------------------------

    private void OnHeroVictory()
    {
        isBattleActive = false;

        // Calculate reward
        BattleRewardResult reward = DungeonRewardCalculator.CalculateReward(currentContext);

        // Grant kleos
        KleosManager.Instance.AddKleos(reward.FinalReward);

        // Post-victory healing
        float healPercent = UpgradeManager.Instance.GetFlat(ModifierType.PostVictoryHealPercent);
        if (healPercent > 0)
        {
            float healAmount = HeroManager.Instance.GetMaxHP() * healPercent;
            HeroManager.Instance.RestoreHP(healAmount);
        }

        // Advance dungeon if dungeon battle
        if (currentContext.Source == BattleSource.Dungeon)
        {
            DungeonManager.Instance.OnLayerCleared(
                currentContext.Dungeon.DungeonId, currentContext.LayerIndex);
        }

        // Build result
        var result = new BattleResult
        {
            IsVictory = true,
            Context = currentContext,
            Reward = reward,
            TotalRounds = totalRounds,
            HeroCritsLanded = heroCritsLanded,
            HeroDodgesPerformed = heroDodgesPerformed,
            HeroHPRemaining = heroHPAtEnd,
            HeroMaxHP = HeroManager.Instance.GetMaxHP(),
            EnemyHPRemaining = 0
        };

        GD.Print("-------------------------------------------");
        GD.Print($"  VICTORY! Defeated {currentContext.Enemy.EnemyName} in {totalRounds} rounds.");
        GD.Print($"  Reward: {reward.FinalReward} kleos" +
                 (reward.WasLucky ? $" (lucky {reward.LuckMultiplier}x!)" : ""));
        GD.Print($"  Hero HP remaining: {heroHPAtEnd:F0}/{HeroManager.Instance.GetMaxHP():F0}");
        GD.Print($"  Crits: {heroCritsLanded} | Dodges: {heroDodgesPerformed}");
        if (currentContext.Source == BattleSource.Dungeon)
            GD.Print($"  Dungeon: {currentContext.Dungeon.DungeonName} Layer {currentContext.LayerIndex} cleared.");
        GD.Print("===========================================");

        BattleEnded?.Invoke(result);
    }

    // -------------------------------------------------------------------------
    // Defeat
    // -------------------------------------------------------------------------

    private void OnHeroDefeat()
    {
        isBattleActive = false;

        // Restore hero to full HP after defeat
        HeroManager.Instance.RestoreFullHP();

        var result = new BattleResult
        {
            IsVictory = false,
            Context = currentContext,
            Reward = null,
            TotalRounds = totalRounds,
            HeroCritsLanded = heroCritsLanded,
            HeroDodgesPerformed = heroDodgesPerformed,
            HeroHPRemaining = 0,
            HeroMaxHP = HeroManager.Instance.GetMaxHP(),
            EnemyHPRemaining = enemyHPAtEnd
        };

        GD.Print("-------------------------------------------");
        GD.Print($"  DEFEAT. {currentContext.Enemy.EnemyName} prevails after {totalRounds} rounds.");
        GD.Print($"  Enemy HP remaining: {enemyHPAtEnd:F0}/{enemyMaxHP:F0}");
        GD.Print($"  Crits landed: {heroCritsLanded} | Dodges: {heroDodgesPerformed}");
        GD.Print("===========================================");

        BattleEnded?.Invoke(result);
    }

    // -------------------------------------------------------------------------
    // Random Encounter Handler
    // -------------------------------------------------------------------------

    private void OnRandomEncounterTriggered(EnemyData enemy, string poolName)
    {
        StartRandomEncounterBattle(enemy, poolName);
    }

    // -------------------------------------------------------------------------
    // Query
    // -------------------------------------------------------------------------

    public bool IsBattleInProgress()
    {
        return isBattleActive;
    }

    public BattleContext GetCurrentContext()
    {
        return currentContext;
    }
}

public class BattleLogEntry
{
    public bool IsHeroAction { get; set; }
    public float Damage { get; set; }
    public bool IsCritical { get; set; }
    public bool IsDodge { get; set; }
    public string ActorName { get; set; }
    public string TargetName { get; set; }
    public float TargetCurrentHP { get; set; }
    public float TargetMaxHP { get; set; }
}

public class BattleResult
{
    public bool IsVictory { get; set; }
    public BattleContext Context { get; set; }
    public BattleRewardResult Reward { get; set; }
    public int TotalRounds { get; set; }
    public int HeroCritsLanded { get; set; }
    public int HeroDodgesPerformed { get; set; }
    public float HeroHPRemaining { get; set; }
    public float HeroMaxHP { get; set; }
    public float EnemyHPRemaining { get; set; }
}
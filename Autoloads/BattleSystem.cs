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
    private StatusEffectManager heroEffects;
    private StatusEffectManager enemyEffects;
    private AbilityResolver heroAbilityResolver;
    private AbilityResolver enemyAbilityResolver;

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

    private BattleTextLibrary textLibrary;

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

        textLibrary = GD.Load<BattleTextLibrary>("res://Resources/BattleText/battle_text_library.tres");

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
        if (layerIndex < 0 || layerIndex >= dungeon.Layers.Count)
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

        InitializeStatusEffects();

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

    private void InitializeStatusEffects()
    {
        heroEffects = new StatusEffectManager();
        enemyEffects = new StatusEffectManager();

        // Hero DoT/HoT callbacks
        heroEffects.OnDamageTaken += (dmg) =>
        {
            HeroManager.Instance.TakeDamage(dmg);
        };

        heroEffects.OnHealingReceived += (heal) =>
        {
            HeroManager.Instance.RestoreHP(heal);
        };

        // Enemy DoT/HoT callbacks
        enemyEffects.OnDamageTaken += (dmg) =>
        {
            enemyCurrentHP -= dmg;
            if (enemyCurrentHP < 0f) enemyCurrentHP = 0f;
        };

        enemyEffects.OnHealingReceived += (heal) =>
        {
            enemyCurrentHP = Mathf.Min(enemyCurrentHP + heal, enemyMaxHP);
        };

        // Ability Resolvers
        heroAbilityResolver = new AbilityResolver();
        enemyAbilityResolver = new AbilityResolver();
    }

    private async System.Threading.Tasks.Task RunCombatAsync()
    {
        while (isBattleActive)
        {
            currentRound++;
            totalRounds = currentRound;

            GD.Print($"  --- Round {currentRound} ---");
            RoundStarted?.Invoke(currentRound);

            // Process status effects at round start (DoTs, HoTs, duration ticks)
            heroEffects.ProcessStartOfRound();
            enemyEffects.ProcessStartOfRound();

            // Check if DoT killed either combatant
            if (enemyCurrentHP <= 0)
            {
                heroHPAtEnd = HeroManager.Instance.GetCurrentHP();
                enemyHPAtEnd = 0;
                await ToSignal(GetTree().CreateTimer(BaseFinalBlowPause / speedMultiplier), SceneTreeTimer.SignalName.Timeout);
                if (!isBattleActive) return;
                OnHeroVictory();
                return;
            }
            if (!HeroManager.Instance.IsAlive())
            {
                heroHPAtEnd = 0;
                enemyHPAtEnd = enemyCurrentHP;
                await ToSignal(GetTree().CreateTimer(BaseFinalBlowPause / speedMultiplier), SceneTreeTimer.SignalName.Timeout);
                if (!isBattleActive) return;
                OnHeroDefeat();
                return;
            }

            // Advance ability cooldowns
            heroAbilityResolver.AdvanceRound();
            enemyAbilityResolver.AdvanceRound();

            // Hero turn
            if (heroEffects.IsStunned())
            {
                var stunEntry = new BattleLogEntry
                {
                    IsHeroAction = true,
                    Damage = 0,
                    IsCritical = false,
                    IsDodge = false,
                    ActorName = "Hero",
                    TargetName = currentContext.Enemy.EnemyName,
                    TargetCurrentHP = enemyCurrentHP,
                    TargetMaxHP = enemyMaxHP
                };
                GD.Print("  Hero is stunned and cannot act!");
                HeroAttackOccurred?.Invoke(stunEntry);
            }
            else
            {
                // Resolve hero ability (no hero abilities yet in Phase 2,
                // but the plumbing is ready for Phase 4)
                float heroHPPercent = HeroManager.Instance.GetCurrentHP()
                    / HeroManager.Instance.GetMaxHP();
                float enemyHPPercent = enemyCurrentHP / enemyMaxHP;

                CombatAbility heroAbility = heroAbilityResolver.ResolveAbility(
                    null, heroHPPercent, enemyHPPercent,
                    heroEffects, enemyEffects);

                if (heroAbility != null)
                {
                    ExecuteAbility(heroAbility, true);
                    if (!heroAbility.ReplacesAttack)
                    {
                        var heroEntry = ExecuteHeroAttack();
                        HeroAttackOccurred?.Invoke(heroEntry);
                    }
                }
                else
                {
                    var heroEntry = ExecuteHeroAttack();
                    HeroAttackOccurred?.Invoke(heroEntry);
                }
            }

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

            // Enemy turn
            if (enemyEffects.IsStunned())
            {
                var stunEntry = new BattleLogEntry
                {
                    IsHeroAction = false,
                    Damage = 0,
                    IsCritical = false,
                    IsDodge = false,
                    ActorName = currentContext.Enemy.EnemyName,
                    TargetName = "Hero",
                    TargetCurrentHP = HeroManager.Instance.GetCurrentHP(),
                    TargetMaxHP = HeroManager.Instance.GetMaxHP()
                };
                GD.Print($"  {currentContext.Enemy.EnemyName} is stunned!");
                EnemyAttackOccurred?.Invoke(stunEntry);
            }
            else
            {
                // Resolve enemy ability
                float heroHPPercent = HeroManager.Instance.GetCurrentHP()
                    / HeroManager.Instance.GetMaxHP();
                float enemyHPPercent = enemyCurrentHP / enemyMaxHP;

                CombatAbility enemyAbility = enemyAbilityResolver.ResolveAbility(
                    currentContext.Enemy.Abilities,
                    enemyHPPercent, heroHPPercent,
                    enemyEffects, heroEffects);

                if (enemyAbility != null)
                {
                    ExecuteAbility(enemyAbility, false);
                    if (!enemyAbility.ReplacesAttack)
                    {
                        var enemyEntry = ExecuteEnemyAttack();
                        EnemyAttackOccurred?.Invoke(enemyEntry);
                    }
                }
                else
                {
                    var enemyEntry = ExecuteEnemyAttack();
                    EnemyAttackOccurred?.Invoke(enemyEntry);
                }
            }

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

            // End-of-round processing
            heroEffects.ProcessEndOfRound();
            enemyEffects.ProcessEndOfRound();

            // Delay between rounds
            await ToSignal(GetTree().CreateTimer(BaseRoundDelay / speedMultiplier), SceneTreeTimer.SignalName.Timeout);

            if (!isBattleActive) return;
        }
    }

    private void ClearStatusEffects()
    {
        if (heroEffects != null) heroEffects.ClearAll();
        if (enemyEffects != null) enemyEffects.ClearAll();
        heroEffects = null;
        enemyEffects = null;
        if (heroAbilityResolver != null) heroAbilityResolver.Reset();
        if (enemyAbilityResolver != null) enemyAbilityResolver.Reset();
        heroAbilityResolver = null;
        enemyAbilityResolver = null;
    }

    // -------------------------------------------------------------------------
    // Hero Attack
    // -------------------------------------------------------------------------

    private BattleLogEntry ExecuteHeroAttack()
    {
        // Base damage from hero stats
        float baseDamage = HeroManager.Instance.GetDamage();

        // Apply status effect damage modifiers
        float modifiedDamage = heroEffects.GetModifiedDamage(baseDamage);

        // Crit check: modified chance, enemy crit immunity
        float baseCritChance = HeroManager.Instance.GetCritChance();
        float modifiedCritChance = heroEffects.GetModifiedCritChance(baseCritChance);
        bool canCrit = !enemyEffects.HasCritImmunity();
        bool isCrit = canCrit && GD.Randf() < modifiedCritChance;

        float finalDamage = modifiedDamage;
        if (isCrit)
        {
            float critMultiplier = HeroManager.Instance.GetCritMultiplier();
            finalDamage = modifiedDamage * critMultiplier;
            heroCritsLanded++;
        }

        // Shield absorption on enemy
        finalDamage = enemyEffects.AbsorbDamage(finalDamage);

        // Apply damage to enemy
        enemyCurrentHP -= finalDamage;
        if (enemyCurrentHP < 0) enemyCurrentHP = 0;

        // Damage reflect: enemy reflects a portion back to hero
        float reflectPercent = enemyEffects.GetReflectPercent();
        if (reflectPercent > 0f)
        {
            float reflected = finalDamage * reflectPercent;
            HeroManager.Instance.TakeDamage(reflected);
            GD.Print($"  Damage reflected! {reflected:F1} returned to Hero.");
        }

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
        // Dodge check with status modifiers
        float baseDodge = HeroManager.Instance.GetDodgeChance();
        float modifiedDodge = heroEffects.GetModifiedDodgeChance(baseDodge);
        bool dodged = GD.Randf() < modifiedDodge;

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

        // Enemy damage with status modifiers
        float baseDamagePerHit = currentContext.Enemy.DamagePerSecond
            / currentContext.Enemy.AttackRate;
        float modifiedDamage = enemyEffects.GetModifiedDamage(baseDamagePerHit);

        // Shield absorption on hero
        float finalDamage = heroEffects.AbsorbDamage(modifiedDamage);

        // Apply damage through HeroManager
        HeroManager.Instance.TakeDamage(finalDamage);

        // Damage reflect: hero reflects a portion back to enemy
        float reflectPercent = heroEffects.GetReflectPercent();
        if (reflectPercent > 0f)
        {
            float reflected = finalDamage * reflectPercent;
            enemyCurrentHP -= reflected;
            if (enemyCurrentHP < 0f) enemyCurrentHP = 0f;
            GD.Print($"  Attack reflected! {reflected:F1} returned to {currentContext.Enemy.EnemyName}.");
        }

        var entry = new BattleLogEntry
        {
            IsHeroAction = false,
            Damage = finalDamage,
            IsCritical = false,
            IsDodge = false,
            ActorName = currentContext.Enemy.EnemyName,
            TargetName = "Hero",
            TargetCurrentHP = HeroManager.Instance.GetCurrentHP(),
            TargetMaxHP = HeroManager.Instance.GetMaxHP()
        };

        GD.Print($"  {currentContext.Enemy.EnemyName} attacks for {finalDamage:F1} damage -> Hero " +
                 $"({HeroManager.Instance.GetCurrentHP():F0}/{HeroManager.Instance.GetMaxHP():F0} HP)");

        return entry;
    }

    private void ExecuteAbility(CombatAbility ability, bool isHeroAbility)
    {
        string casterName = isHeroAbility ? "Hero" : currentContext.Enemy.EnemyName;

        // Log ability name (centered, ability color)
        string colorHex = ability.AbilityColor.ToHtml(false);
        string abilityNameLine = $"[color=#{colorHex}]{ability.AbilityName.ToUpper()}[/color]";
        var nameEntry = new BattleLogEntry
        {
            IsHeroAction = isHeroAbility,
            Damage = 0,
            IsCritical = false,
            IsDodge = false,
            ActorName = casterName,
            TargetName = isHeroAbility ? currentContext.Enemy.EnemyName : "Hero",
            TargetCurrentHP = isHeroAbility ? enemyCurrentHP : HeroManager.Instance.GetCurrentHP(),
            TargetMaxHP = isHeroAbility ? enemyMaxHP : HeroManager.Instance.GetMaxHP(),
            RichTextOverride = abilityNameLine,
            AlignCenter = true
        };
        if (isHeroAbility)
            HeroAttackOccurred?.Invoke(nameEntry);
        else
            EnemyAttackOccurred?.Invoke(nameEntry);

        GD.Print($"  {casterName} uses {ability.AbilityName}!");

        // Log cast flavor text
        if (!string.IsNullOrEmpty(ability.CastFlavorText))
        {
            var flavorEntry = new BattleLogEntry
            {
                IsHeroAction = isHeroAbility,
                Damage = 0,
                IsCritical = false,
                IsDodge = false,
                ActorName = casterName,
                TargetName = isHeroAbility ? currentContext.Enemy.EnemyName : "Hero",
                TargetCurrentHP = isHeroAbility ? enemyCurrentHP : HeroManager.Instance.GetCurrentHP(),
                TargetMaxHP = isHeroAbility ? enemyMaxHP : HeroManager.Instance.GetMaxHP(),
                RichTextOverride = ability.CastFlavorText
            };
            if (isHeroAbility)
                HeroAttackOccurred?.Invoke(flavorEntry);
            else
                EnemyAttackOccurred?.Invoke(flavorEntry);

            GD.Print($"    \"{ability.CastFlavorText}\"");
        }

        // Process each effect
        for (int i = 0; i < ability.Effects.Count; i++)
        {
            ProcessAbilityEffect(ability.Effects[i], isHeroAbility);
        }
    }

    private void ProcessAbilityEffect(AbilityEffect effect, bool isHeroAbility)
    {
        // Determine caster and target managers
        StatusEffectManager casterEffects = isHeroAbility ? heroEffects : enemyEffects;
        StatusEffectManager targetEffects = isHeroAbility ? enemyEffects : heroEffects;
        string casterName = isHeroAbility ? "Hero" : currentContext.Enemy.EnemyName;
        string targetName = isHeroAbility ? currentContext.Enemy.EnemyName : "Hero";

        switch (effect.EffectType)
        {
            case AbilityEffectType.DealDamage:
                ApplyAbilityDamage(effect.Value, isHeroAbility);
                break;

            case AbilityEffectType.ApplyStatus:
                ApplyStatusFromAbility(effect, targetEffects, casterName);
                break;

            case AbilityEffectType.ApplySelfStatus:
                ApplyStatusFromAbility(effect, casterEffects, casterName);
                break;

            case AbilityEffectType.HealSelf:
                ApplyAbilityHeal(effect.Value, isHeroAbility, toSelf: true);
                break;

            case AbilityEffectType.HealTarget:
                ApplyAbilityHeal(effect.Value, isHeroAbility, toSelf: false);
                break;

            case AbilityEffectType.RemoveDebuff:
                targetEffects.RemoveEffect(effect.StatusType);
                GD.Print($"    Removed {effect.StatusType} from target.");
                break;

            case AbilityEffectType.RemoveBuff:
                targetEffects.RemoveEffect(effect.StatusType);
                GD.Print($"    Removed {effect.StatusType} from target.");
                break;
        }
    }

    private void ApplyAbilityDamage(float damage, bool isHeroAbility)
    {
        if (isHeroAbility)
        {
            // Hero ability damages enemy
            float finalDamage = enemyEffects.AbsorbDamage(damage);
            enemyCurrentHP -= finalDamage;
            if (enemyCurrentHP < 0f) enemyCurrentHP = 0f;
            GD.Print($"    Ability deals {finalDamage:F1} damage to {currentContext.Enemy.EnemyName}.");
        }
        else
        {
            // Enemy ability damages hero
            float finalDamage = heroEffects.AbsorbDamage(damage);
            HeroManager.Instance.TakeDamage(finalDamage);
            GD.Print($"    Ability deals {finalDamage:F1} damage to Hero.");
        }
    }

    private void ApplyAbilityHeal(float healValue, bool isHeroAbility, bool toSelf)
    {
        // Determine who gets healed
        bool healHero = (isHeroAbility && toSelf) || (!isHeroAbility && !toSelf);

        if (healHero)
        {
            HeroManager.Instance.RestoreHP(healValue);
            GD.Print($"    Hero healed for {healValue:F1}.");
        }
        else
        {
            enemyCurrentHP = Mathf.Min(enemyCurrentHP + healValue, enemyMaxHP);
            GD.Print($"    {currentContext.Enemy.EnemyName} healed for {healValue:F1}.");
        }
    }

    private void ApplyStatusFromAbility(
    AbilityEffect effect,
    StatusEffectManager targetManager,
    string sourceId)
    {
        var status = new StatusEffect(
            effect.StatusType,
            effect.StatusName,
            effect.StatusValue,
            effect.StatusDuration,
            effect.StatusIsDebuff,
            effect.StatusMode,
            effect.StatusMaxStacks
        );
        status.ApplyFlavorText = effect.StatusApplyText;
        status.TickFlavorText = effect.StatusTickText;
        status.ExpireFlavorText = effect.StatusExpireText;

        targetManager.ApplyEffect(status, sourceId);
        GD.Print($"    Applied {effect.StatusName} ({effect.StatusValue}, " +
                 $"{effect.StatusDuration} rounds).");
    }

    // -------------------------------------------------------------------------
    // Victory
    // -------------------------------------------------------------------------

    private void OnHeroVictory()
    {
        isBattleActive = false;

        ClearStatusEffects();

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

        ClearStatusEffects();

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

    public float GetCurrentSpeedMultiplier()
    {
        return speedMultiplier;
    }

    public BattleTextLibrary GetTextLibrary()
    {
        return textLibrary;
    }

    public StatusEffectManager GetHeroEffects()
    {
        return heroEffects;
    }

    public StatusEffectManager GetEnemyEffects()
    {
        return enemyEffects;
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
    public string RichTextOverride { get; set; } = null;
    public bool AlignCenter { get; set; } = false;
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
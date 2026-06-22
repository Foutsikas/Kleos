# Kleos Architecture Reference -- Godot Edition
# KAR_Godot -- Updated June 22, 2026
# Engine: Godot 4.6.2 .NET (C#)
# Status: Combat RPG complete, abilities, status effects,
#   NumberFormatter, Deed Button Visual Evolution,
#   FlavorTextManager, Omen system, artisan rounded bulk purchase,
#   damage number popups

---

## About This Document

This is the technical architecture reference for Kleos. It documents how
each system is implemented in Godot, including class structures, signal
wiring, file paths, and Godot-specific patterns.

---

## Engine and Platform

Engine: Godot 4.6.2 .NET (stable)
Language: C# via .NET 8 SDK
Rendering: Forward+ (Vulkan or D3D12 depending on hardware)
Target: Desktop (Windows, Linux)

---

## Project Structure

```
res://
  Autoloads/
	Combat/                      (all seven combat classes live here)
	  AbilityEffect.cs           ([GlobalClass] Resource)
	  AbilityEnums.cs            (AbilityEffectType, AbilityTargetType, AbilityTrigger enums)
	  AbilityResolver.cs         (plain C# class, per-combatant ability AI)
	  CombatAbility.cs           ([GlobalClass] Resource)
	  StatusEffect.cs            (plain C# class, single effect instance)
	  StatusEffectManager.cs     (plain C# class, per-combatant effect tracker)
	  StatusEffectType.cs        (StatusEffectType and StatusEffectMode enums)
	ArtisanManager.cs            (Autoload)
	BattleContext.cs             (plain C# class, not an Autoload)
	BattleSystem.cs              (Autoload)
	DevConsole.cs                (Autoload)
	DungeonManager.cs            (Autoload)
	DungeonRewardCalculator.cs   (static utility, not an Autoload)
	FlavorTextManager.cs         (Autoload)
	HeroAbilityManager.cs        (Autoload)
	HeroManager.cs               (Autoload)
	KleosManager.cs              (Autoload)
	NumberFormatter.cs           (static utility, not an Autoload)
	RandomEncounterManager.cs    (Autoload)
	SaveData.cs                  (plain C# RefCounted save classes, not an Autoload)
	SaveManager.cs               (Autoload)
	SettingsManager.cs           (Autoload)
	UpgradeManager.cs            (Autoload)
  Resources/
	ArtisanData.cs               ([GlobalClass] Resource)
	BattleTextLibrary.cs         ([GlobalClass] Resource)
	DungeonData.cs               ([GlobalClass] Resource)
	DungeonLayer.cs              ([GlobalClass] Resource)
	EncounterPool.cs             ([GlobalClass] Resource)
	EncounterPoolEntry.cs        ([GlobalClass] Resource)
	EnemyData.cs                 ([GlobalClass] Resource)
	HeroData.cs                  ([GlobalClass] Resource)
	HeroStat.cs                  (enum)
	ModifierEffect.cs            ([GlobalClass] Resource)
	ModifierEnums.cs             (ModifierType and ModifierMode enums)
	UpgradeConfig.cs             ([GlobalClass] Resource)
	Abilities/
	  HeroAbilityDatabase.cs     ([GlobalClass] Resource)
	  Enemies/
		20 enemy ability .tres files (across 3 dungeons)
	  Hero/
		9 hero ability .tres files
		hero_ability_database.tres
	Artisans/
	  ArtisanDatabase.cs         ([GlobalClass] Resource)
	  artisan_database.tres
	  scribe.tres
	  bard.tres
	  potter.tres
	  sculptor.tres
	  playwright.tres
	  historian.tres
	BattleText/
	  battle_text_library.tres
	Dungeons/
	  DungeonDatabase.cs         ([GlobalClass] Resource)
	  dungeon_database.tres
	  forest.tres
	  brigands.tres
	  coastal.tres
	EncounterPools/
	  EncounterPoolDatabase.cs   ([GlobalClass] Resource)
	  encounter_database.tres
	  pool_forest.tres
	  pool_brigands.tres
	  pool_coastal.tres
	Enemies/
	  1. Forest/
		1.wild_dog.tres
		2.wolf.tres
		3.wolf_pack.tres
		4.large_wolf.tres
		5.large_wolf_pack.tres
		6.nemean_lion_cub.tres
		7.nemean_lion.tres
	  2. Brigands/
		1.road_thief.tres
		2.bandit_lookout.tres
		3.outlaw_peltast.tres
		4.bandit_hoplite.tres
		5.rogue_mercenary.tres
		6.outlaw_peltast_band.tres
		7.bandit_champion.tres
		8.war_hounds.tres
		9.pine_bender.tres
		10.archilestes.tres
	  3. Coastal/
		1.shore_crab.tres
		2.reef_serpent.tres
		3.drowned_sailor.tres
		4.reef_serpent_pair.tres
		5.siren_thrall.tres
		6.sea_hag.tres
		7.coastal_chimera.tres
		8.scylla_spawn.tres
		9.charybdis_maw.tres
		10.siren_queen.tres
	FlavorText/
	  FlavorTextLibrary.cs       ([GlobalClass] Resource)
	  flavor_text_library.tres
	Upgrades/
	  UpgradesDatabase.cs        ([GlobalClass] Resource, plural class name)
	  upgrade_database.tres
	  1_01_scribes_quill.tres
	  1_02_bronze_training.tres
	  ... (24 total, prefixed by tier and order)
	  3_07_coastal_plunder.tres
  Scenes/
	MainMenu/
	  main_menu.tscn
	  MainMenuController.cs
	Game/
	  main_game.tscn
	  MainGameController.cs
	  ArtisanRow.tscn
	  ArtisanRow.cs
	  DungeonRow.tscn
	  DungeonRow.cs
	  UpgradeRow.tscn
	  UpgradeRow.cs
	  BattlePanel.cs
	  TierHeader.tscn
	  AbilityRow.tscn
	  AbilityRow.cs
	  AbilitySectionHeader.tscn
	  StatusEffectDisplay.cs
	  DeedButtonEvolution.cs
```

---

## Autoload Initialization Order

Autoloads are registered in Project Settings in this exact order.
Order matters because later managers may reference earlier ones in
their _Ready() methods.

  1. SettingsManager
  2. SaveManager
  3. UpgradeManager
  4. KleosManager
  5. ArtisanManager
  6. HeroManager
  7. DungeonManager
  8. HeroAbilityManager
  9. RandomEncounterManager
  10. BattleSystem
  11. DevConsole
  12. FlavorTextManager

Each uses a static Instance property with a guard in _Ready() that
calls QueueFree() if Instance is already set. This prevents duplicate
instantiation if the autoload node somehow appears twice.

HeroAbilityManager must come after HeroManager and DungeonManager
because it subscribes to LevelUp and DungeonCompleted signals.

BattleSystem must come after RandomEncounterManager because it
subscribes to the EncounterTriggered signal in _Ready().

DevConsole is a CanvasLayer with Layer 100, so it renders above all
game UI. It has no dependencies on init order beyond needing all
managers to exist.

---

## Resource Database Loading

Config-driven managers load a single database resource and read its
exported array, rather than scanning a directory. Each database is a
[GlobalClass] Resource holding one typed array:

  ArtisanDatabase.Artisans  -- res://Resources/Artisans/artisan_database.tres
  DungeonDatabase.Dungeons  -- res://Resources/Dungeons/dungeon_database.tres
  UpgradesDatabase.Upgrades -- res://Resources/Upgrades/upgrade_database.tres  (plural class name)
  EncounterPoolDatabase.Pools -- res://Resources/EncounterPools/encounter_database.tres
  HeroAbilityDatabase.Abilities -- res://Resources/Abilities/Hero/hero_ability_database.tres

Each manager calls GD.Load<TDatabase>(path) in its LoadConfigs(), casts
the array to the untyped Godot Array it exposes, then applies its own
sort. New content is added by dropping the .tres into the matching
database's array in the Inspector. This replaced the earlier
ResourceScanner directory-scan utility, which has been removed.

---

## Autoload Managers

### KleosManager

Singleton autoload. Central authority for kleos currency.

Signals:
  KleosChanged(float amount) -- fires on any kleos modification
  KleosGained(float amount) -- fires for XP tracking (hero system)
  ProductionChanged(float amount) -- fires when passive income changes
  DeedContextChanged() -- fires when deed context text should update

State:
  currentKleos (float)
  totalKleosPerSecond (float)
  passiveAccumulator (float)

Key methods:
  DoDeed() -- adds click damage to kleos, emits signals
  AddKleos(float) -- adds kleos from any source, emits signals
  SpendKleos(float) -- deducts if affordable, returns bool
  RecalculateTotalProduction(float) -- sets total and emits signal

Click damage calculation:
  float baseDamage = 1f;
  float upgradeBonus = UpgradeManager.Instance.GetFlat(ModifierType.ClickFlat);
  return baseDamage + upgradeBonus;

Passive income in _Process():
  passiveAccumulator += totalKleosPerSecond * (float)delta;
  while passiveAccumulator >= 1.0:
    AddKleos(1f);
    passiveAccumulator -= 1f;

Save/Load:
  GetSaveData() returns KleosSaveData
  LoadFromSaveData(KleosSaveData) restores state

---

### ArtisanManager

Singleton autoload. Manages artisan purchases, ownership, production,
and unlock conditions.

Signals:
  ArtisanPurchased(string artisanId) -- fires after successful purchase
  ArtisanUnlocked(string artisanId) -- fires when a new artisan unlocks
  ProductionRecalculated(float totalKPS) -- fires after production update
  BuyMultiplierChanged(int multiplier) -- fires when the buy multiplier
    cycles; ArtisanRow refreshes and MainGameController relabels the button

State:
  ownedCounts (Dictionary, string to int)
  unlockedArtisans (List of string)
  hasInitialized (bool, guard flag)
  currentBuyMultiplier (int, default 1)
  BuyMultiplierCycle (static int array {1, 10, 100})

Config loading:
  Loads GD.Load<ArtisanDatabase>("res://Resources/Artisans/artisan_database.tres")
  and reads its Artisans array. Null-checks the database.
  SortByUnlockOrder() chains artisans by RequiredArtisanId: the artisan
  with no requirement (Scribe) comes first, then each artisan whose
  requirement is the previous one in the chain.

Key methods:
  PurchaseArtisan(ArtisanData) -- spends kleos, increments count by 1,
    recalculates production, checks for new unlocks
  PurchaseArtisan(ArtisanData, int quantity) -- bulk overload; spends the
    geometric bulk cost, adds quantity, fires ArtisanPurchased once,
    runs the unlock cascade and production recalc a single time
  IsArtisanUnlocked(ArtisanData) -- checks unlock condition
  GetOwnedCount(string artisanId) -- returns count for given artisan
  GetCurrentCost(ArtisanData) -- BaseCost * CostMultiplier ^ owned
  GetBulkCost(ArtisanData, int quantity) -- geometric series total for
    buying quantity from the current owned count; guards CostMultiplier
    of 1 with a flat-price fallback
  CanPurchase(ArtisanData) -- checks unlocked and affordable (single)
  CanPurchase(ArtisanData, int quantity) -- all-or-nothing on the bulk
    cost of the rounded quantity
  GetBuyMultiplier() -- returns currentBuyMultiplier
  CycleBuyMultiplier() -- steps to the next unlocked tier in
    BuyMultiplierCycle, wrapping; emits BuyMultiplierChanged
  IsBuyMultiplierUnlocked(int multiplier) -- x1 and x10 always true;
    x100 returns false until the "the Tireless" epithet (V0.95). Single
    point to flip when epithets ship.
  GetRoundedQuantity(ArtisanData) -- count to reach the next clean
    multiple of the current multiplier; always 1 in x1 mode
  RecalculateTotalProduction() -- sums all artisan output with modifiers
  RefreshUnlocks() -- checks all artisan unlock conditions after purchase
  GetArtisanById(string artisanId) -- returns ArtisanData by ID
  GetUnlockedCount() -- returns unlockedArtisans.Count (used by
    DeedButtonEvolution for tier calculation)

Bulk purchase notes:
  Rounded quantity: x10 buys up to the next multiple of ten, not ten
  more. GetRoundedQuantity uses ((owned / mult) + 1) * mult - owned.
  Bulk cost is first * (ratio ^ quantity - 1) / (ratio - 1), where
  first is the cost at the current owned count and ratio is CostMultiplier.
  The single-argument PurchaseArtisan and CanPurchase are retained
  unchanged for backward compatibility; the quantity overloads sit
  alongside them.

Production calculation:
  For each artisan:
    baseProd = KleosPerSecond * ownedCount
    multiplier = UpgradeManager.GetMultiplier(ArtisanProductionMultiplier)
    totalProd += baseProd * multiplier
  Calls KleosManager.RecalculateTotalProduction(totalProd)

Save/Load:
  GetSaveData() returns ArtisanSaveData (owned counts, unlocked list)
  LoadFromSaveData(ArtisanSaveData) restores state, sets guard flag

---

### UpgradeManager

Singleton autoload. Manages upgrade purchases and modifier queries.

Signals:
  UpgradePurchased(string upgradeId)
  TiersRefreshed()

State:
  purchasedUpgradeIds (Array of string)
  upgradeConfigLookup (Dictionary, string to UpgradeConfig)

Config loading:
  Loads GD.Load<UpgradesDatabase>("res://Resources/Upgrades/upgrade_database.tres")
  and reads its Upgrades array. (Class name is UpgradesDatabase, plural.)
  SortByTierAndCost() sorts by tier number first, then by cost within
  each tier. This guarantees correct display order regardless of
  filesystem ordering.
  BuildLookup() caches id-to-config dictionary after loading.

Key methods:
  GetFlat(ModifierType type) -- sums all flat modifiers of given type
    across purchased upgrades
  GetMultiplier(ModifierType type) -- multiplies all multiplier modifiers
    of given type across purchased upgrades (starts at 1.0)
  PurchaseUpgrade(string upgradeId) -- spends kleos, adds to purchased list
  IsUpgradePurchased(string upgradeId) -- checks purchase state
  CanPurchase(string upgradeId) -- checks tier gate, individual lock, cost

Lock checks:
  IsTierUnlocked(UpgradeConfig) -- checks RequiredDungeon completion
  IsIndividualLockMet(UpgradeConfig) -- checks hero level, prerequisite
    upgrade, and artisan count requirements

Save/Load:
  GetSaveData() returns UpgradeSaveData (purchased IDs list)
  LoadFromSaveData(UpgradeSaveData) restores purchased list

---

### HeroManager

Singleton autoload. Hero progression, leveling, stat allocation, and
combat stat queries.

Signals:
  LevelUp(int newLevel)
  StatsChanged()

State:
  heroData (HeroData resource instance)
  currentHP (float, runtime state, not in HeroData)

Key methods:
  AddExperience(float amount) -- applies XP multiplier, checks level up
  CheckForLevelUp() -- while loop for multi-level gains
  UpgradeStat(HeroStat stat) -- spends stat point, adjusts stat
  GetMaxHP() -- 40 + (Endurance * 5)
  GetDamage() -- 3 + (Strength * 1.0)
  GetDodgeChance() -- Cunning * 0.01, clamped to 0.3
  GetCritChance() -- (Cunning * 0.005) + (Favor * 0.01), clamped to 0.25
  GetCritMultiplier() -- 2.0 + (Favor * 0.1)
  RollDodge() -- GD.Randf() < GetDodgeChance()
  RollCrit() -- GD.Randf() < GetCritChance()
  GetXPToNextLevel() -- 1000 * Mathf.Pow(1.5, level - 1)
  TakeDamage(float) -- reduces currentHP, emits StatsChanged
  RestoreHP(float) -- increases currentHP up to max
  RestoreFullHP() -- sets currentHP to max
  IsAlive() -- currentHP > 0

XP subscription:
  _Ready() connects to KleosManager.KleosGained signal.
  All kleos from any source feeds into AddExperience().

Endurance special case:
  When upgrading Endurance, currentHP gains the 5 HP bonus immediately
  rather than staying at the old value.

Save/Load:
  GetSaveData() returns HeroSaveData (level, XP, stat points, upgrades)
  LoadFromSaveData(HeroSaveData) restores all hero state

---

### DungeonManager

Singleton autoload. Dungeon progression tracking and layer management.

Signals:
  DungeonCompleted(string dungeonId)
  LayerCleared(string dungeonId, int layerIndex)

State:
  dungeonProgress (Dictionary, string to int -- dungeon name to highest
    cleared layer)
  completedDungeons (Dictionary, string to bool)

Config loading:
  Loads GD.Load<DungeonDatabase>("res://Resources/Dungeons/dungeon_database.tres")
  and reads its Dungeons array. Null-checks the database.
  SortByProgression() chains dungeons by RequiredDungeon: dungeons with
  no RequiredDungeon come first, then each dungeon whose requirement is
  the previous one in the chain.

Key methods:
  IsDungeonUnlocked(string dungeonId) -- checks kleos and dungeon requirements
  IsDungeonCompleted(string dungeonId) -- checks completed dictionary
  GetHighestClearedLayer(string dungeonId) -- returns from progress dictionary
  GetNextLayer(string dungeonId) -- highest cleared + 1
  CanAccessLayer(string dungeonId, int layerIndex) -- sequential enforcement
  GetDungeonById(string dungeonId) -- returns DungeonData by ID
  GetLayer(string dungeonId, int index) -- returns DungeonLayer with null check
  OnLayerCleared(string dungeonId, int layerIndex) -- updates progress
  ForceCompleteDungeon(string dungeonId) -- DEV API, sets progress to
    final layer, emits DungeonCompleted then LayerCleared

Signal ordering in OnLayerCleared():
  1. Update dungeonProgress dictionary
  2. CheckDungeonCompletion() -- sets completed flag, emits DungeonCompleted
  3. Emit LayerCleared signal
  This order ensures the completed flag is set BEFORE any UI responds
  to LayerCleared. Fixed April 2026 (was previously emitting LayerCleared
  before checking completion, causing UI desync).

Progress validation:
  Players must complete layers sequentially. Cannot skip ahead.

Save/Load:
  GetSaveData() returns DungeonSaveData (progress dictionary, completed list)
  LoadFromSaveData(DungeonSaveData) restores progress

---

### RandomEncounterManager

Singleton autoload. Multi-pool random encounter system.

Signals:
  EncounterTriggered(EnemyData enemy, string poolName)

State:
  clickAccumulator (int)
  clickThreshold (int, random between 10 and 30)
  activePools (list, rebuilt when dirty flag is set)

Config loading:
  Loads GD.Load<EncounterPoolDatabase>("res://Resources/EncounterPools/encounter_database.tres")
  and reads its Pools array. Null-checks the database. Adding a new pool
  means adding its .tres to the database's Pools array in the Inspector.

Key methods:
  OnDeedClicked() -- increments counter, checks threshold
  RollNewThreshold() -- random int between configurable min/max;
	also pre-selects pendingPool for the cycle (see Omen Integration)
  RefreshPoolsIfDirty() -- rebuilds active pool list
  PickRandomPool() -- selects random pool from active pools
  PickRandomEnemy(EncounterPool) -- weighted random selection
  TryTriggerEncounter() -- fires the encounter using pendingPool

Pool activation:
  Forest pool: always active (RequiredDungeon is null)
  Other pools: active when DungeonManager.IsDungeonCompleted() returns
  true for their RequiredDungeon reference.

Subscribes to DungeonManager.DungeonCompleted to mark pools dirty.

---

### BattleSystem

Singleton autoload (position 10). Core combat engine. Async timed
turn-based combat with C# events for UI communication.

C# Events (not Godot signals -- plain C# classes as parameters):
  BattleStarted (Action<BattleContext>)
  RoundStarted (Action<int>)
  HeroAttackOccurred (Action<BattleLogEntry>)
  EnemyAttackOccurred (Action<BattleLogEntry>)
  BattleEnded (Action<BattleResult>)

C# events are used instead of Godot signals because BattleContext,
BattleLogEntry, and BattleResult are plain C# classes that are not
Variant-compatible. C# events have no such restriction and work for
C#-to-C# communication.

State:
  currentContext (BattleContext)
  enemyCurrentHP, enemyMaxHP (float)
  currentRound (int)
  isBattleActive (bool)
  speedMultiplier (float, default 1.0)
  Tracking: totalRounds, heroCritsLanded, heroDodgesPerformed,
	heroHPAtEnd, enemyHPAtEnd

Timing constants:
  BaseSetupPause: 0.8s before first round
  BaseAttackDelay: 0.4s between hero and enemy attack
  BaseRoundDelay: 0.4s between rounds
  BaseFinalBlowPause: 0.6s after killing blow
  All divided by speedMultiplier.

Key methods:
  StartDungeonBattle(DungeonData, int layerIndex) -- null check on
	GetLayer instead of bounds check after call (fixed April 2026)
  StartRandomEncounterBattle(EnemyData, string poolName)
  SetSpeedMultiplier(float)
  GetCurrentSpeedMultiplier() -- returns current speed
  IsBattleInProgress() -- returns isBattleActive
  GetCurrentContext() -- returns current BattleContext
  GetTextLibrary() -- returns loaded BattleTextLibrary

Combat flow (async):
  BeginBattle() -- creates context, resets state, invokes
	BattleStarted, awaits setup pause, calls RunCombatAsync().
  RunCombatAsync() -- while loop with await between each attack
	and round. Each await checks isBattleActive before continuing.
	Hero attacks first (ExecuteHeroAttack), checks enemy death,
	awaits attack delay, enemy retaliates (ExecuteEnemyAttack),
	checks hero death, awaits round delay.
  ExecuteHeroAttack() -- calculates damage, rolls crit, applies
	damage to enemy, returns BattleLogEntry.
  ExecuteEnemyAttack() -- rolls dodge, calculates damage per hit
	(DPS / AttackRate), applies via HeroManager.TakeDamage(),
	returns BattleLogEntry.
  OnHeroVictory() -- calculates reward, grants kleos, applies
	post-victory heal, advances dungeon layer, invokes BattleEnded.
  OnHeroDefeat() -- restores hero HP to full, invokes BattleEnded.

Text library:
  Loaded in _Ready() via GD.Load<BattleTextLibrary>(path).
  Accessible to BattlePanel via GetTextLibrary().

Signal subscription:
  _Ready() subscribes to RandomEncounterManager.EncounterTriggered
  to auto-start random encounter battles.

---

### BattleContext (Plain C# class)

File: res://Autoloads/BattleContext.cs
Not a Node or Resource. Data packet created at battle start.

Fields:
  Source (BattleSource enum: Dungeon or RandomEncounter)
  Enemy (EnemyData)
  Dungeon (DungeonData, null for random encounters)
  LayerIndex (int, -1 for random encounters)
  IsBossLayer, IsMiniBossLayer (bool)
  BaseReward (float)
  HeaderText (string)
  PoolName (string, empty for dungeon battles)

Factory methods:
  CreateDungeonBattle(DungeonData, int, DungeonLayer)
  CreateRandomEncounter(EnemyData, string poolName)

---

### DungeonRewardCalculator (Static Utility)

File: res://Autoloads/DungeonRewardCalculator.cs
Static class (not an Autoload). Pure calculation, no state.

Methods:
  CalculateReward(BattleContext) -- dispatches to correct path
  CalculateDungeonReward(BattleContext) -- deterministic
  CalculateRandomEncounterReward(BattleContext) -- RNG variance

Returns BattleRewardResult with FinalReward, BaseReward,
LuckMultiplier, and WasLucky fields.

---

### BattleLogEntry (Plain C# class, in BattleSystem.cs)

Fields:
  IsHeroAction (bool), Damage (float), IsCritical (bool),
  IsDodge (bool), ActorName (string), TargetName (string),
  TargetCurrentHP (float), TargetMaxHP (float),
  RichTextOverride (string, May 2026 -- overrides default log line),
  AlignCenter (bool, May 2026), OverrideColor (Color, May 2026)

### BattleResult (Plain C# class, in BattleSystem.cs)

Fields:
  IsVictory (bool), Context (BattleContext),
  Reward (BattleRewardResult), TotalRounds (int),
  HeroCritsLanded (int), HeroDodgesPerformed (int),
  HeroHPRemaining (float), HeroMaxHP (float),
  EnemyHPRemaining (float)

### BattleRewardResult (Plain C# class, in DungeonRewardCalculator.cs)

Fields:
  FinalReward (int), BaseReward (int),
  LuckMultiplier (int), WasLucky (bool)

---

### StatusEffectType (Enum, in Autoloads/Combat/StatusEffectType.cs) (May 2026)

Values: AttackDamageUp, AttackDamageDown, DodgeUp, DodgeDown,
  CritChanceUp, CritImmunity, Stun, DamageOverTime, HealOverTime,
  Shield, Absorb, Reflect.

### StatusEffect (Plain C# class, in Autoloads/Combat/StatusEffect.cs) (May 2026)

Fields:
  EffectType (StatusEffectType), EffectName (string), Value (float),
  Duration (int, rounds), IsDebuff (bool), Mode (StatusEffectMode),
  MaxStacks (int), CurrentStacks (int),
  ApplyFlavorText (string), ExpireFlavorText (string).

StatusEffectMode enum: Flat, Percentage.

### StatusEffectManager (Plain C# class, in Autoloads/Combat/StatusEffectManager.cs) (May 2026)

One instance per combatant (hero and enemy each have one).
Managed by BattleSystem, created fresh per battle.

Key methods:
  ApplyEffect(StatusEffect, sourceId) -- applies or stacks effect
  TickEffects() -- decrements durations, removes expired effects
  GetActiveEffects() -- returns list of active effects
  GetDamageModifier() -- sums AttackDamageUp/Down modifiers
  GetDodgeModifier() -- sums DodgeUp/Down modifiers
  GetCritModifier() -- sums CritChanceUp modifiers
  HasCritImmunity() -- checks for active CritImmunity
  IsStunned() -- checks for active Stun
  GetShieldAmount() -- returns remaining shield value
  ApplyShieldDamage(float) -- reduces shield, returns overflow

BattleSystem damage pipeline routes through StatusEffectManager:
  base damage -> modifiers -> shield absorption -> final damage.

### AbilityEnums (in Autoloads/Combat/AbilityEnums.cs) (May 2026)

AbilityEffectType: DealDamage, Heal, ApplyStatus.
AbilityTrigger: OnCooldown, OnLowHP, OnBattleStart, OnAllyDeath.
AbilityTargetType: Self, Enemy.

### AbilityEffect (Resource, in Autoloads/Combat/AbilityEffect.cs) (May 2026)

[GlobalClass] Resource. Fields:
  EffectType, Target, Value (for damage/heal),
  StatusType, StatusName, StatusValue, StatusDuration,
  StatusIsDebuff, StatusMode, StatusMaxStacks,
  StatusApplyText, StatusExpireText.

### CombatAbility (Resource, in Autoloads/Combat/CombatAbility.cs) (May 2026)

[GlobalClass] Resource.
Fields:
  AbilityId, AbilityName, AbilityDescription, AbilityColor (Color).
  Trigger (AbilityTrigger), Priority (int), UseChance (float).
  CooldownDuration (int), CurrentCooldown (int).
  ReplacesAttack (bool), OneTimeUse (bool).
  CastFlavorText (string).
  Effects (Array of AbilityEffect).

### AbilityResolver (in Autoloads/Combat/AbilityResolver.cs) (May 2026)

One instance per combatant. Initialized with a list of CombatAbility.
Evaluated each round by BattleSystem.

Key methods:
  TryGetAbility() -- iterates abilities by priority, checks trigger
	conditions and use chance, returns the first matching ability
	or null if none qualify
  AdvanceCooldowns() -- decrements cooldowns each round
  MarkUsed(CombatAbility) -- sets cooldown, removes if OneTimeUse

Priority list design (not behavior tree): abilities are sorted by
priority and evaluated in order. First match wins. Covers phase
transitions and conditional logic without architectural complexity.

### HeroAbilityManager (Autoload, May 2026)

Singleton autoload (position 8). Manages hero combat ability unlocks.

File: res://Autoloads/HeroAbilityManager.cs

Signals:
  AbilityUnlocked(string abilityId)

Config loading:
  Loads HeroAbilityDatabase resource via GD.Load<HeroAbilityDatabase>()
  (not ResourceScanner -- uses database pattern for single resource).

Three unlock paths:
  Level-based: auto-unlocked when HeroManager.LevelUp fires
  Kleos-purchased: PurchaseAbility() checks kleos and level reqs
  Dungeon reward: auto-unlocked when DungeonManager.DungeonCompleted fires

Key methods:
  IsUnlocked(string abilityId) -- checks unlock state
  GetUnlockedAbilities() -- returns Array for AbilityResolver init
  GetAllAbilities() -- returns full list for Combat Arts panel
  GetAbilityById(string) -- lookup by ID
  GetUnlockedCount() / GetTotalCount() -- counts
  PurchaseAbility(CombatAbility) -- spends kleos, unlocks ability

Save/Load:
  GetSaveData() returns HeroAbilitySaveData
  LoadFromSaveData(HeroAbilitySaveData) restores unlock state

### HeroAbilityDatabase (Resource, May 2026)

[GlobalClass] Resource. Single .tres file containing an Array of
CombatAbility references. Used by HeroAbilityManager for config
loading instead of directory scanning.

### NumberFormatter (Static Utility, May 2026)

File: res://Autoloads/NumberFormatter.cs
Static class, not a Node or Autoload.

Suffix table (short scale): K, M, B, T, Qa, Qi, Sx, Sp, Oc, No, Dc.
Scientific notation fallback beyond 999 decillion (10^36+).

Methods:
  FormatCompact(double) -- suffix display or scientific if toggled
  FormatFull(double) -- always full integer with thousand separators
  FormatCost(double) -- full below 10K, compact above

Reads SettingsManager.Instance.UseScientificNotation for toggle.

### DeedButtonEvolution (Scene Script, June 2026)

File: res://Scenes/Game/DeedButtonEvolution.cs
Attached to: DeedButtonContainer (Control node in CenterPanel)
Not an Autoload. Scene-level script.

Exports:
  Button DeedButton
  ColorRect DeedGlow

State:
  currentTier (int, -1 on init)
  transitionTween, glowTween (Tween references for cancellation)
  styleNormal, styleHover, stylePressed, styleFocus (StyleBoxFlat)

Signal subscription:
  ArtisanManager.ArtisanPurchased -> OnArtisanPurchased()
  Subscribes in _Ready(), unsubscribes in _ExitTree().

Tier calculation:
  CalculateTier() calls ArtisanManager.Instance.GetUnlockedCount().
  Maps count to tier: 0-1 = Bronze, 2-3 = Silver, 4-5 = Gold, 6 = Divine.

Visual application:
  Creates four StyleBoxFlat instances in _Ready(), assigns to button
  theme overrides (normal, hover, pressed, focus).
  ApplyTierInstant() sets all colors and border widths directly.
  TransitionToTier() uses Tween for animated color transitions.

Tier-up animation:
  PlayFlash() -- white ColorRect burst, scale 1.0 to 1.3, alpha 0.6 to 0.0
	over 0.3 seconds.
  Color tween -- StyleBoxFlat bg_color, border_color tweened over 0.5 seconds.
  Font color -- tweened via TweenMethod with lerp callback (theme color
	overrides cannot be directly tweened).

Divine glow:
  StartGlowLoop() -- infinite Tween, alpha 0.10 to 0.30, sine ease,
	1.0 second per half-cycle.
  StopGlowLoop() -- kills glow tween, called on _ExitTree or tier change.

Public method:
  ForceVisualTier(int) -- used by DevConsole deed_tier command.

---

### FlavorTextManager (Autoload, June 2026; data-driven June 12; per-pool omens June 15)

Singleton autoload (position 12). Manages temporary text display
in FlavorTextLabel below DeedContextLabel.

File: res://Autoloads/FlavorTextManager.cs

State:
  flavorLabel (Label, set by MainGameController via SetLabel())
  activeTween (Tween, current animation)
  isOmenActive (bool, omen priority flag)
  library (FlavorTextLibrary) -- loaded in _Ready() via GD.Load from
	res://Resources/FlavorText/flavor_text_library.tres, immediately
	after Instance assignment and before the ArtisanPurchased
	subscription. PrintErr on load failure; readers carry hardcoded
	single-line fallbacks for the null-library case.

Constants:
  FlavorDisplayTime: 2.5 seconds
  FadeInTime: 0.3 seconds
  FadeOutTime: 0.5 seconds
  FlavorColor: #B8A88A (muted earth tone)
  OmenColor: #C4785A (amber warning)

Data (June 12, 2026 -- data-driven):
  The former static OmenTexts array and ArtisanFlavorTexts dictionary
  are removed. Omen lines now come from EncounterPool.OmenLines (the
  per-pool path) or library.GenericOmenLines (fallback); artisan
  lines come from ArtisanData.PurchaseFlavorLines or
  library.GenericArtisanLines (fallback).

Public API:
  SetLabel(Label) -- called by MainGameController in _Ready()
  ShowFlavor(string) -- brief timed message, ignored if omen active
  ShowOmen(string) -- persistent warning, replaces current text
  ShowOmenForPool(EncounterPool) -- shows a line from the pool's
    OmenLines if non-empty, else falls back to ShowRandomOmen(). The
    per-pool omen entry point (June 15); placed directly below
    ShowRandomOmen.
  ShowRandomOmen() -- picks from library.GenericOmenLines (hardcoded
    single-line fallback if the library is null). Still the generic
    fallback and the DevConsole test path.
  ClearOmen() -- fades out omen, resets isOmenActive
  Clear() -- force clears any displayed text

Signal subscriptions:
  ArtisanManager.ArtisanPurchased -> OnArtisanPurchased()
  Subscribes in _Ready(), unsubscribes in _ExitTree().
  OnArtisanPurchased(artisanId) priority chain:
    1. ArtisanManager.GetArtisanById(artisanId).PurchaseFlavorLines
       if non-empty
    2. library.GetRandomGenericArtisanLine()
  Mirrors the EnemyData AttackLines precedent.

Animation:
  PlayFlavorSequence() -- fade in, hold, fade out, clear text
  FadeOut() -- used by ClearOmen for smooth omen dismissal
  All animations use Godot Tween. Previous tween killed before
  starting a new one to prevent overlap.

### RandomEncounterManager Omen Integration (June 2026; per-pool June 15)

Added fields:
  omenTriggerPoint (int) -- click count at which omen fires
  omenShownThisCycle (bool) -- prevents repeat omen per cycle
  pendingPool (EncounterPool) -- the pool chosen for the coming
    encounter, selected once at cycle start so the omen and the
    encounter draw from the same pool.

Modified methods:
  RollNewThreshold() -- rolls omenTriggerPoint (threshold minus
    random 3-8 offset, minimum 1), then RefreshPoolsIfDirty() and
    pendingPool = PickRandomPool().
  OnDeedClicked() -- checks omenTriggerPoint before threshold, then
    calls FlavorTextManager.ShowOmenForPool(pendingPool). On encounter
    trigger, calls FlavorTextManager.ClearOmen() before firing.
  TryTriggerEncounter() -- uses pendingPool for the encounter
    (pendingPool ?? PickRandomPool() as a null safety). Pool selection
    moved from encounter time to cycle start; a pool unlocked
    mid-cycle becomes eligible next cycle (cycles are 10-30 clicks).

---

### DevConsole

CanvasLayer autoload (position 11). Developer tool for testing.

FlavorTextManager must come after DevConsole in the autoload order.
It subscribes to ArtisanManager.ArtisanPurchased in _Ready() and
receives its label reference from MainGameController.
Builds its entire UI in code -- no .tscn file needed.

File: res://Autoloads/DevConsole.cs

Layer: 100 (renders above all game UI).
Toggle: backtick (`) key via _UnhandledKeyInput().
Command history: up/down arrow keys.

UI structure (built in _Ready()):
  PanelContainer (dark background, top of screen, 160px tall)
    VBoxContainer
      Label ("DEV CONSOLE" title)
      Label (output display, autowrap)
      LineEdit (command input, TextSubmitted signal)

Command processing:
  OnCommandSubmitted() adds to history, calls ExecuteCommand().
  ExecuteCommand() lowercases entire input, splits on spaces,
  dispatches by first token.

Commands:
  help -- lists all commands
  kleos <amount> -- calls KleosManager.AddKleos()
  level <target> -- grants XP to reach target level (single large grant)
  stat <str/end/cun/fav> <n> -- calls HeroManager.UpgradeStat() N times,
    tracks actual applied count vs requested
  clear <dungeonId> -- calls DungeonManager.ForceCompleteDungeon()
  layer <dungeonId> <count> -- calls OnLayerCleared() for next N layers
  hp <amount> -- RestoreFullHP then TakeDamage to set exact value
  pools -- prints active pool info
  save -- builds SaveData from all managers, calls SaveManager.Save()
  load -- calls SaveManager.Load(), distributes to all managers
  reset -- calls SaveManager.ResetAllSaveData()
  status -- displays kleos, KpS, hero level, HP, damage, dodge, crit
  effects -- shows active status effects in battle (May 2026)
  buff <target> <type> <val> <dur> -- applies status effect in battle (May 2026)
  testability -- adds test ability to current enemy (May 2026)
  abilities -- shows all hero abilities with unlock status (May 2026)
  unlock <abilityId> -- force-unlocks a hero ability (May 2026)
  deed_tier <0-3> -- forces deed button visual tier (June 2026)

Known limitation: ExecuteCommand lowercases entire input including
arguments. Currently safe because all dungeon IDs are lowercase.
Would need adjustment if mixed-case IDs are added later.

---

### SaveManager

Singleton autoload. JSON file persistence.

File paths:
  Save file: user://game_save.json
  Backup file: user://game_save.backup.json

Key methods:
  Save(SaveData) -- serializes to JSON via BuildJson(), creates backup,
    writes file
  Load() -- reads file via ParseJson(), falls back to backup, returns
    empty SaveData if both fail
  HasSaveData() -- checks if save file exists
  DeleteSaveData() -- deletes save and backup files
  ResetAllSaveData() -- alias for DeleteSaveData
  ResetKleosOnly() -- loads, clears kleos section, saves
  ResetArtisanData() -- loads, clears artisan section, saves
  ResetUpgradeData() -- loads, clears upgrade section, saves
  ResetDungeonData() -- loads, clears dungeon section, saves
  ResetHeroData() -- loads, clears hero section, saves

JSON uses Godot's built-in Json class with Dictionary intermediates.
Dictionary helpers convert between C# Dictionary<string, int> and Godot
Dictionary for serialization.

---

### SettingsManager

Singleton autoload. User preferences persistence.

File path: user://settings.cfg (ConfigFile format)

State:
  MusicVolume (float, 0-1)
  SFXVolume (float, 0-1)
  Fullscreen (bool)
  ResolutionIndex (int)
  UseScientificNotation (bool, default false) -- May 2026

Key methods:
  SetMusicVolume(float) -- clamps, applies, saves
  SetSfxVolume(float) -- clamps, applies, saves
  SetFullscreen(bool) -- applies, saves
  SetResolutionIndex(int) -- applies, saves
  SetScientificNotation(bool) -- saves (May 2026)
  ApplyAudio() -- converts linear to dB, sets AudioServer bus volumes
  ApplyDisplay() -- sets window mode
  ResetToDefaults() -- restores all settings to defaults
  HasSaveData() -- delegates to SaveManager.HasSaveData()
  DeleteSaveData() -- delegates to SaveManager.ResetAllSaveData()

Settings use ConfigFile stored separately from game save data.
Appropriate for preferences that should survive save deletion.

---

## Resource Classes

All resource classes use the [GlobalClass] attribute for Godot editor
visibility and [Export] properties for Inspector editing.

### ArtisanData (Resource)

Fields:
  ArtisanName (string)
  ArtisanId (string, unique identifier)
  BaseCost (float)
  KleosPerSecond (float)
  CostMultiplier (float)
  RequiredArtisanId (string, empty if always unlocked)
  RequiredArtisanCount (int)
  PurchaseFlavorLines (Array of string, exported) -- per-artisan
	purchase flavor lines consumed by FlavorTextManager; empty array
	falls back to the generic library pool.

### EnemyData (Resource)

Fields:
  EnemyName (string)
  EnemyId (string)
  EnemySprite (Texture2D)
  Health (float)
  DamagePerSecond (float)
  AttackRate (float)
  KleosReward (int)
  EncounterFlavorTexts (Array of string)
  AttackLines (Array of string, enemy-specific combat text)
  Abilities (Array of CombatAbility, enemy combat abilities)

### DungeonData (Resource)

Fields:
  DungeonName (string)
  DungeonId (string)
  Description (string)
  DungeonIcon (Texture2D)
  KleosRequirement (float)
  ArtisanRequirement (int)
  RequiredDungeon (DungeonData reference)
  Layers (Array of DungeonLayer)

Helper: GetLayer(int index) for typed access.

### DungeonLayer (Resource, separate file)

Fields:
  Enemy (EnemyData reference)
  BaseKleosReward (float)
  IsBossLayer (bool)
  IsMiniBossLayer (bool)

Split into its own file to avoid Godot typed array issues.
DungeonData.Layers uses untyped Array with GetLayer() helper.

### UpgradeConfig (Resource)

Fields:
  UpgradeName (string)
  UpgradeId (string)
  Description (string)
  Cost (float)
  Tier (int)
  RequiredDungeon (DungeonData reference)
  RequiredHeroLevel (int)
  RequiredUpgradeId (string)
  RequiredArtisanId (string)
  RequiredArtisanCount (int)
  Effects (Array of ModifierEffect)

Helper: GetEffect(int index) for typed access.

### ModifierEffect (Resource)

Fields:
  Type (ModifierType enum)
  Value (float)
  Mode (ModifierMode enum -- Flat or Multiplier)
  TargetId (string, optional for artisan-specific bonuses)

### ModifierType (Enum, in ModifierEnums.cs)

Values: ClickFlat, BattleRewardFlat, BattleCritMultiplier,
  ArtisanProductionMultiplier, XPMultiplier, HeroStrengthFlat,
  HeroMaxHPFlat, HeroDodgeChance, HeroCritChance, HeroCritMultiplier,
  PostVictoryHealPercent, BattleSpeedX2Unlocked, BattleSpeedX4Unlocked,
  StatPointsPerLevel, ClickMultiplier

### ModifierMode (Enum, in ModifierEnums.cs)

Values: Flat, Multiplier

### HeroData (Resource)

Fields:
  Level (int, default 1)
  CurrentXP (float)
  AvailableStatPoints (int)
  StrengthUpgrades, EnduranceUpgrades, CunningUpgrades, FavorUpgrades (int)
  BaseStrength (int, 10), BaseEndurance (int, 10), BaseCunning (int, 5),
  BaseFavor (int, 0)

Methods:
  GetStrength() -- base + upgrades
  GetEndurance() -- base + upgrades
  GetCunning() -- base + upgrades
  GetFavor() -- base + upgrades
  GetMaxHP(), GetDamage(), GetDodgeChance(), GetCritChance(),
  GetCritMultiplier()

Note: Combat stat calculations live in both HeroData (formulas) and
HeroManager (runtime queries with upgrade bonuses). HeroData is a pure
data resource. HeroManager handles runtime state like currentHP.

### EncounterPool (Resource)

Fields:
  PoolName (string)
  RequiredDungeon (DungeonData, null for always-active pools)
  Entries (Array of EncounterPoolEntry)
  OmenLines (Array of string, exported, June 15) -- per-pool omen
	lines shown by FlavorTextManager.ShowOmenForPool; empty array
	falls back to the generic library pool.

Helper: GetEntry(int index) for typed access.

### EncounterPoolEntry (Resource, separate file)

Fields:
  Enemy (EnemyData reference)
  Weight (float, default 1.0)

### BattleTextLibrary (Resource)

Nine string arrays for combat text pools:
  HeroAttackLines, HeroCritLines, HeroDodgeLines,
  EnemyAttackLines, EnemyAnticipationLines,
  VictoryLines, VictorySubtitles,
  DefeatLines, DefeatSubtitles, DefeatConsolations

Public getter methods with fallback strings if arrays are empty.
Enemy name methods (GetRandomEnemyAttack, GetRandomVictoryLine,
GetRandomDefeatLine, GetRandomEnemyAnticipation) use string.Format()
to replace {0} placeholders with the enemy name.

Asset: res://Resources/BattleText/battle_text_library.tres
Loaded by BattleSystem in _Ready() via GD.Load().

### FlavorTextLibrary (Resource, June 12, 2026)

Resource class for generic flavor text pools. Mirrors
BattleTextLibrary: exported Array[string] pools under ExportGroups, a
private GetRandom(pool, fallback) helper, and public accessors with
hardcoded last-resort fallbacks.

Pools:
  GenericOmenLines (eight region-neutral lines, after the four
	Forest-specific lines moved to the Forest pool June 15)
  GenericArtisanLines (four neutral fallback lines)
  MilestoneLines (reserved, empty -- V0.7 awakening sequence)

Accessors: GetRandomOmenLine(), GetRandomGenericArtisanLine(),
GetRandomMilestoneLine().

File:  res://Resources/FlavorText/FlavorTextLibrary.cs
Asset: res://Resources/FlavorText/flavor_text_library.tres
Loaded by FlavorTextManager in _Ready() via GD.Load().

Owner-specific lines take priority over this library everywhere:
ArtisanData.PurchaseFlavorLines for artisan flavor, and
EncounterPool.OmenLines for omens (June 15). The library is always the
fallback, never the first choice.

### SaveData (Plain C# classes extending RefCounted)

  SaveData -- root container
	Version (string)
	LastSaveTime (long)
	Kleos (KleosSaveData)
	Artisans (ArtisanSaveData)
	Upgrades (UpgradeSaveData)
	Dungeons (DungeonSaveData)
	Hero (HeroSaveData)
	HeroAbilities (HeroAbilitySaveData) -- May 2026

Each sub-class extends RefCounted and contains only serializable
properties (strings, ints, floats, dictionaries, arrays).

HeroAbilitySaveData fields:
  UnlockedAbilityIds (Array of string)

---

## Scenes

### Main Menu (main_menu.tscn)

Controller: MainMenuController.cs

Node tree:
  MainMenu (Control)
	Background (ColorRect)
	StartButton (Button -- invisible, full screen click target)
	CenterContainer (VBoxContainer)
	  TitleLabel (Label -- "KLEOS")
	  PromptLabel (Label -- pulsing text)
	SettingsButton (Button)
	SettingsPanel (PanelContainer, starts hidden)
	  VBoxContainer
		SettingsTitle, MusicRow, SfxRow, FullscreenToggle,
		DeleteSaveButton, CloseSettingsButton
	FadeOverlay (ColorRect)

Flow:
  _Ready() checks SaveManager.HasSaveData() for first-play detection.
  Prompt text set to "Begin Your Journey" or random returning prompt.
  PulsePrompt() animates alpha via tween loop (1.0 to 0.3 and back).
  OnStartButtonPressed() triggers FadeOutAndLoadGame().
  Settings panel blocks start clicks while open.

### Game Scene (main_game.tscn)

Controller: MainGameController.cs

Node tree:
  MainGame (Control)
	Background (ColorRect)
	RootLayout (VBoxContainer)
	  TopBar (HBoxContainer)
		HeroPortrait (PanelContainer, compact display, clickable)
		  VBoxContainer
			HeroLevelLabel
			HeroHPBar (ProgressBar)
			HeroXPBar (ProgressBar)
		KleosLabel
		ProductionLabel
	  MainPanel (HBoxContainer)
		LeftPanel (VBoxContainer)
		  TopSpacer, DungeonButton, MiddleSpacer, UpgradeButton, BottomSpacer
		CenterPanel (VBoxContainer)
		  DeedButtonContainer (Control, DeedButtonEvolution.cs)
			DeedGlow (ColorRect, behind button, starts hidden)
			DeedButton (Button)
		  DeedContextLabel
		  FlavorTextLabel (Label, managed by FlavorTextManager)
		RightPanel (PanelContainer, dark StyleBoxFlat, shrink-center vertical)
		  RightColumn (VBoxContainer)
			ArtisanHeaderLabel (Label, "Artisans")
			ArtisanScrollContainer > ArtisanList (VBoxContainer)
			FooterRow (HBoxContainer)
			  BuyMultButton (Button, pinned bottom-left)
	HeroPanel (PanelContainer, overlay, hidden)
	  VBoxContainer with stat rows, bars, upgrade buttons
	DungeonPanel (PanelContainer, overlay, hidden)
	  ScrollContainer > DungeonList (VBoxContainer)
	UpgradePanel (PanelContainer, overlay, hidden)
	  ScrollContainer > UpgradeList (VBoxContainer)
	AbilityPanel (PanelContainer, overlay, hidden) -- May 2026
	  ScrollContainer > AbilityList (VBoxContainer)
	BattlePanel (Control, overlay, hidden)
	  BattleBackground (ColorRect, Full Rect)
	  CombatArea (Control, Full Rect)
		EncounterHeaderLabel (Label, Center Top)
		HeroSection (VBoxContainer, Bottom Left)
		  HeroHPText, HeroHPBar, HeroPortrait, HeroNameLabel, HeroLevelLabel
		EnemySection (VBoxContainer, Top Right)
		  EnemyNameLabel, EnemyPortrait, EnemyHPBar, EnemyHPText
		BattleLogContainer (VBoxContainer, Center Bottom)
		  LogLine1, LogLine2, LogLine3, LogLine4
		SpeedToggleButton (Button, Top Left, hidden)
	  ResultOverlay (Control, Full Rect, hidden)
		ResultContent (VBoxContainer, Center)
		  ResultSubtitleLabel, ResultTitleLabel, ResultFlavorLabel,
		  ResultRewardLabel, ResultLuckLabel, ResultSummaryLabel,
		  ResultConsolationLabel, ResultActionButton, ViewBattleLogButton
	  PostCombatLogOverlay (Control, Full Rect, hidden)
		LogMargin (MarginContainer, Full Rect, margins 20/20/20/60)
		  PostCombatLogScroll (ScrollContainer)
			PostCombatLogList (VBoxContainer)
		BackToResultsButton (Button, Center Bottom)
	  PopupLayer (Control, Full Rect, added in code at runtime -- damage popups)
	FadeOverlay (ColorRect)

MainGameController responsibilities:
  Connects all button signals in _Ready().
  Subscribes to manager signals for display updates.
  Manages panel visibility (hero, dungeon, upgrade, ability).
  Dungeon, Upgrade, and Ability panels are mutually exclusive (ActivePanel enum).
  Spawns ArtisanRow instances into ArtisanList via PopulateArtisanList().
  Wires BuyMultButton in _Ready(): Pressed cycles the artisan buy
	multiplier, BuyMultiplierChanged relabels the button ("Buy x1" etc.).
  Spawns DungeonRow instances into DungeonList via PopulateDungeonList().
  Spawns UpgradeRow and TierHeader instances into UpgradeList via
	PopulateUpgradeList().
  Spawns AbilityRow instances into AbilityList via PopulateAbilityList().
  Wires FlavorTextLabel to FlavorTextManager via SetLabel() in _Ready().
  Refreshes kleos display, production display, deed context,
	hero portrait bars, and hero panel stats.
  Handles stat upgrade button presses.
  Fade-in on scene entry.

### ArtisanRow (ArtisanRow.tscn)

Script: ArtisanRow.cs
Root node: PanelContainer

Node tree:
  ArtisanRow (PanelContainer)
	HBoxContainer
	  ArtisanInfoContainer (VBoxContainer)
		ArtisanNameLabel (Label)
		ArtisanKPSLabel (Label)
	  ArtisanRightContainer (VBoxContainer)
		ArtisanCostLabel (Label)
		ArtisanBuyButton (Button)
		ArtisanOwnedLabel (Label)

Two visual states managed internally:

Locked state:
  Modulate set to grey (0.5, 0.5, 0.5, 0.6) -- dims entire row.
  ArtisanKPSLabel shows requirement text (e.g. "Requires 5 Scribes").
  ArtisanCostLabel and ArtisanOwnedLabel hidden.
  ArtisanBuyButton text set to "Locked", disabled.

Unlocked state:
  Modulate set to white (1, 1, 1, 1) -- full color.
  ArtisanKPSLabel shows production value.
  Cost and owned labels visible.
  Buy button disabled only when the rounded batch is unaffordable.
  Button text comes from GetBuyButtonText: "Hire" in x1 mode, "Hire N"
  in x10/x100 mode where N is the rounded quantity. SetUnlocked no longer
  hardcodes "Hire"; RefreshDisplay owns the button text.

Bulk-aware display:
  RefreshDisplay computes the rounded quantity (GetRoundedQuantity), the
  batch cost (GetBulkCost), and affordability (CanPurchase with quantity).
  The cost label shows the batch cost. OnBuyPressed buys the rounded
  quantity. OnKleosChanged updates only the button's disabled state
  (cost and quantity do not change on a kleos tick).

State transitions:
  Setup(ArtisanData) checks IsArtisanUnlocked and calls SetLocked or
  SetUnlocked accordingly.
  OnAnyArtisanPurchased(string) listens for ArtisanManager.ArtisanPurchased
  and re-checks unlock condition. If now unlocked, calls SetUnlocked().

Signal subscriptions:
  KleosManager.KleosChanged -- updates affordability of the rounded batch.
  ArtisanManager.ArtisanPurchased -- checks if this row should unlock.
  ArtisanManager.BuyMultiplierChanged -- full RefreshDisplay so cost,
    button text, and affordability track the new multiplier.

PopulateArtisanList() in MainGameController spawns all six rows
regardless of lock state. Each row manages its own appearance.

### DungeonRow (DungeonRow.tscn)

Script: DungeonRow.cs
Root node: PanelContainer

Node tree:
  DungeonRow (PanelContainer)
    VBoxContainer
      HeaderRow (HBoxContainer)
        DungeonNameLabel (Label)
        ProgressLabel (Label)
      LayerInfoLabel (Label)
      DungeonActionButton (Button)

Four visual states managed internally:

Locked state:
  Modulate set to grey (0.5, 0.5, 0.5, 0.6).
  LayerInfoLabel shows lock reason.
  DungeonActionButton text "Locked", disabled.

Available state:
  Modulate normal (1, 1, 1, 1).
  LayerInfoLabel shows "Next: Enemy Name".
  DungeonActionButton text "Enter", enabled.

In Progress state:
  Modulate normal.
  LayerInfoLabel shows next enemy with boss prefix if applicable.
  DungeonActionButton text "Continue (Layer N)", enabled.

Completed state:
  Modulate green tint (0.7, 0.85, 0.7, 1).
  LayerInfoLabel shows "All trials conquered."
  DungeonActionButton text "Completed", disabled.

Signal subscriptions:
  DungeonManager.LayerCleared -- refreshes display for this dungeon.
  DungeonManager.DungeonCompleted -- refreshes when this dungeon completes
	OR when the completed dungeon is this row's RequiredDungeon
	(so downstream dungeons unlock immediately).
  KleosManager.KleosChanged -- refreshes if dungeon has kleos requirement.

OnActionPressed() calls BattleSystem.Instance.StartDungeonBattle()
with the dungeon data and next layer index. Guards against battles
already in progress and completed dungeons. Uses IsDungeonCompleted()
check instead of bounds comparison. Progress display clamped with
Mathf.Min() to prevent exceeding total layers.

_ExitTree() unsubscribes from all signals with -= operators.

PopulateDungeonList() in MainGameController spawns one row per dungeon.

### UpgradeRow (UpgradeRow.tscn)

Script: UpgradeRow.cs
Root node: PanelContainer

Node tree:
  UpgradeRow (PanelContainer)
	VBoxContainer
	  HeaderRow (HBoxContainer)
		UpgradeNameLabel (Label)
		UpgradeCostLabel (Label)
	  DescriptionLabel (Label)
	  LockReasonLabel (Label, hidden by default)
	  UpgradeBuyButton (Button)

Five visual states managed internally:

Affordable:
  Modulate (1, 1, 1, 1). Button enabled, text "Purchase".

Unaffordable:
  Modulate (0.7, 0.7, 0.7, 0.85). Button disabled, text "Purchase".

Purchased:
  Modulate (0.7, 0.85, 0.65, 1). Button disabled, text "Purchased".

Tier Locked:
  Modulate (0.45, 0.45, 0.45, 0.6). LockReasonLabel shows dungeon
  requirement. Button disabled, text "Locked".

Individual Locked:
  Modulate (0.55, 0.45, 0.35, 0.7). LockReasonLabel shows specific
  reason (hero level, prerequisite upgrade, artisan count). Button
  disabled, text "Locked".

Lock checks:
  IsTierUnlocked() checks RequiredDungeon completion.
  TryGetIndividualLockReason() checks hero level, prerequisite upgrade,
	and artisan count. Returns lock reason string if locked.
  Dead GetIndividualLockReason() method removed (April 2026 cleanup).

Signal subscriptions:
  KleosManager.KleosChanged -- refreshes affordability.
  UpgradeManager.UpgradePurchased -- refreshes all rows (prerequisite
	checks may have changed).
  DungeonManager.DungeonCompleted -- refreshes tier-gated rows when
	their RequiredDungeon is completed (added April 2026).

_ExitTree() unsubscribes from all signals with -= operators.
Fixed April 2026: was previously using += instead of -= for
DungeonCompleted, causing a memory leak on scene reload.

OnBuyPressed() calls UpgradeManager.PurchaseUpgrade() then
ArtisanManager.RecalculateTotalProduction() to apply production
modifiers immediately.

### TierHeader (TierHeader.tscn)

No script. Simple PanelContainer with a TierLabel (Label).
Text set by MainGameController during PopulateUpgradeList().
Inserted between upgrade row groups when tier number changes.

Tier names:
  Tier 1: "Tier 1 -- Trials of the Forest"
  Tier 2: "Tier 2 -- Trials of the Road"
  Tier 3: "Tier 3 -- Trials of the Shore"

### AbilityRow (AbilityRow.tscn, May 2026)

Script: AbilityRow.cs
Root node: PanelContainer

Displays a single hero ability in the Combat Arts panel.
Spawned by MainGameController.PopulateAbilityList().

Visual elements:
  Left accent bar (ColorRect, green when unlocked, hidden when locked)
  Ability name label + type badges (HBoxContainer)
  Type badges: color-coded pills auto-determined from AbilityEffect data
	(Attack = red, Heal = green, Buff = blue, Debuff = purple, etc.)
  Description label: auto-generated from effect data
  Flavor text label: from CastFlavorText, dimmed italic
  Bottom row: unlock condition + status/purchase button

Three visual states:
  Unlocked: green left accent, full opacity
  Purchasable: dimmed, active Purchase button with kleos cost
  Locked: dimmed, "Locked" badge with unlock requirement text

Signal subscriptions:
  KleosManager.KleosChanged -- refreshes affordability
  HeroManager.LevelUp -- refreshes level requirements
  HeroAbilityManager.AbilityUnlocked -- refreshes unlock state
  DungeonManager.DungeonCompleted -- refreshes dungeon requirements

### BattlePanel (in main_game.tscn)

Script: BattlePanel.cs (extends Control, not PanelContainer)
Root node: Control with Full Rect anchors.
Not a separate .tscn -- built directly in the game scene tree.

BattlePanel is a special overlay: not toggled by a sidebar button
like Dungeon/Upgrade panels. Opened automatically by BattleSystem
when combat starts, closed by the player pressing a result button.

Uses Control as root instead of PanelContainer to avoid container
layout restrictions on children. All child positioning uses anchors.

Three sub-areas managed by visibility toggling:
  CombatArea: visible during active combat.
  ResultOverlay: visible after combat ends.
  PostCombatLogOverlay: visible when viewing full battle history.
  Only one is visible at a time.

Event subscriptions (C# events, connected in _Ready()):
  BattleSystem.BattleStarted -- opens panel, sets up displays.
  BattleSystem.HeroAttackOccurred -- updates enemy HP, pushes log.
  BattleSystem.EnemyAttackOccurred -- updates hero HP, pushes log.
  BattleSystem.RoundStarted -- reserved for future use.
  BattleSystem.BattleEnded -- shows result screen after delay.

Disconnects all events in _ExitTree().

Log system:
  Three parallel lists: logLineHistory (strings), logColorHistory
  (colors), logIsHeroAction (bools). PushLogLine() appends to all
  three and updates the 4 visible Label nodes with newest-at-bottom
  ordering and alpha fading.

Animation methods:
  AnimateAttackNudge() -- tweens portrait position toward opponent.
  AnimateDamageShake() -- tweens portrait with horizontal oscillation.
  TweenHPBar() -- tweens ProgressBar value smoothly.
  FadePanelIn/Out() -- tweens panel Modulate alpha.
  FadeInResultOverlay() -- tweens result overlay Modulate alpha.

Helper methods:
  GetEnemyAttackLine(string) -- priority chain: enemy-specific
	AttackLines array, then BattleTextLibrary generic pool,
	then hardcoded fallback.
  SetProgressBarColor() -- creates StyleBoxFlat and applies as
	theme override for ProgressBar fill color.

Damage popups (June 2026):
  PopupLayer is a transparent Control created in code by CreatePopupLayer()
  in _Ready(), added as the last child of the panel root so popups draw
  above the combat display, and set to Full Rect. MouseFilter is Ignore so
  it never intercepts result-screen input. No scene node and no new export.

  Popups anchor to the HP bar ProgressBar nodes (EnemyHPBar, HeroHPBar),
  not the portrait TextureRect nodes. The enemy portrait fills a tall
  top-right region, so anchoring to its top edge placed the number up near
  the screen header; the HP bar is a stable, well-placed reference on both
  sides. PopupSpawnYOffset is the single tuning knob for how far above the
  bar a popup begins.

  Methods:
	CreatePopupLayer() -- builds the runtime PopupLayer.
	SpawnDamagePopup(Control anchor, float amount, Color, bool emphasize)
	  -- formats the number, then spawns a label.
	SpawnWordPopup(Control anchor, string word, Color) -- spawns a word
	  label (used for "Evaded" on dodge).
	SpawnPopupLabel(...) -- shared core. Creates the Label, positions it
	  above the anchor's global rect with a small random horizontal jitter,
      tweens position up and modulate alpha to zero, frees on completion.
      Crits use a larger font and a scale punch. The tween is created from
      the label (label.CreateTween()), so freeing the label kills its tween.
    FormatPopupDamage(float) -- raw whole number below 10000,
      NumberFormatter.FormatCompact at 10000 and above.
    ClearPopups() -- frees all live popups; called on battle start and end.

  Live popups are tracked in an activePopups list. Rise duration divides by
  BattleSystem.GetCurrentSpeedMultiplier(), matching the other battle
  animations.

  Hook points: OnHeroAttack spawns over EnemyHPBar (crit color and scale
  punch when entry.IsCritical is set). OnEnemyAttack spawns over HeroHPBar
  on a hit, or the dodge word on a dodge. The RichTextOverride path
  (ability name and flavor lines) returns before the damage section, so
  ability lines never spawn a popup.

Export properties: 30 node references wired in the editor Inspector.
All exports use null checks before access for safety.

---

## Signal Wiring Summary

KleosManager.KleosChanged:
  MainGameController.RefreshKleosDisplay
  ArtisanRow.OnKleosChanged (per row)
  UpgradeRow.OnKleosChanged (per row)
  DungeonRow.OnKleosChanged (per row)
  AbilityRow.OnKleosChanged (per row, May 2026)

KleosManager.KleosGained:
  HeroManager.OnKleosGained (XP tracking)

KleosManager.ProductionChanged:
  MainGameController.RefreshProductionDisplay

KleosManager.DeedContextChanged:
  MainGameController.RefreshDeedContext

ArtisanManager.ArtisanPurchased:
  ArtisanRow.OnAnyArtisanPurchased (per row, for self-unlock)
  DeedButtonEvolution.OnArtisanPurchased (tier check, June 2026)
  FlavorTextManager.OnArtisanPurchased (flavor text, June 2026)

UpgradeManager.UpgradePurchased:
  UpgradeRow.OnAnyUpgradePurchased (per row, for prerequisite refresh)

DungeonManager.LayerCleared:
  DungeonRow.OnLayerCleared (per row, for progress refresh)

DungeonManager.DungeonCompleted:
  RandomEncounterManager.OnDungeonCompleted (marks pools dirty)
  DungeonRow.OnDungeonCompleted (per row, refreshes self and
    rows whose RequiredDungeon matches)
  UpgradeRow.OnDungeonCompleted (per row, refreshes tier gate)
  HeroAbilityManager.OnDungeonCompleted (dungeon reward unlocks, May 2026)
  AbilityRow.OnDungeonCompleted (per row, May 2026)

HeroManager.StatsChanged:
  MainGameController.RefreshHeroDisplay

HeroManager.LevelUp:
  MainGameController.OnHeroLevelUp
  HeroAbilityManager.OnLevelUp (level-based ability unlocks, May 2026)
  AbilityRow.OnLevelUp (per row, May 2026)

HeroAbilityManager.AbilityUnlocked (May 2026):
  AbilityRow.OnAbilityUnlocked (per row)

BattleSystem.BattleStarted (C# event):
  BattlePanel.OnBattleStarted

BattleSystem.HeroAttackOccurred (C# event):
  BattlePanel.OnHeroAttack

BattleSystem.EnemyAttackOccurred (C# event):
  BattlePanel.OnEnemyAttack

BattleSystem.RoundStarted (C# event):
  BattlePanel.OnRoundStarted

BattleSystem.BattleEnded (C# event):
  BattlePanel.OnBattleEnded

RandomEncounterManager.EncounterTriggered:
  BattleSystem.OnRandomEncounterTriggered

---

## Key Godot Patterns

Godot signals:
  Godot uses [Signal] delegate declarations. Signal names are derived
  from the delegate name minus "EventHandler". Emitted via EmitSignal().
  Connected via += operator on the signal property. Disconnected via -=.

C# Events for non-Variant types:
  BattleSystem uses plain C# events (Action<T>) instead of Godot signals
  because BattleContext, BattleLogEntry, and BattleResult are plain C#
  classes that are not Variant-compatible. Godot signals can only carry
  Variant types. C# events work for C#-to-C# communication.

Resource classes:
  Godot Resource classes are saved as .tres files. Created in editor via
  right-click > New Resource. Referenced by other resources or exported
  properties on nodes. [GlobalClass] attribute makes them visible in the
  Godot editor resource picker.

Autoloads:
  Godot Autoloads are nodes added to the scene tree root before any
  scene loads. They persist across scene changes automatically. Accessed
  via static Instance property.

Scene instantiation:
  PackedScene.Instantiate<T>() creates a scene instance. AddChild() places
  it in the tree. Node tree order determines render order (later children
  draw on top).

Typed array workaround:
  Godot C# does not support generic typed arrays in exported properties
  (Array<T> causes editor issues). Use untyped Array with .As<T>()
  helper methods for typed access. This affects DungeonData.Layers,
  EncounterPool.Entries, and UpgradeConfig.Effects.

Database resource loading:
  Each config-driven manager loads one database .tres via GD.Load<T>()
  and reads its exported array. New content is added by dropping a .tres
  into the matching database's array in the Inspector. Each manager
  applies its own sort after loading to guarantee display order.

Signal ordering:
  When state must be set before UI responds to a signal, run the state
  update before emitting the signal. Example: DungeonManager runs
  CheckDungeonCompletion() before emitting LayerCleared. This prevents
  UI from reading stale state during signal handlers.

Autoload registration:
  Build the C# project before registering new autoloads in Project
  Settings. If you add the autoload entry before building, Godot
  generates a UID for a script that does not exist yet, and the UID
  becomes permanently invalid.

Control vs PanelContainer for overlays:
  BattlePanel uses Control (not PanelContainer) as its root node.
  PanelContainer enforces layout constraints on children that prevent
  anchor-based positioning. Control with Full Rect anchors allows free
  child positioning.

---

## Bug Fixes Applied (April 2026)

1. RandomEncounterManager.LoadConfigs() -- changed from a hardcoded
   single-pool GD.Load to loading EncounterPoolDatabase and reading its
   Pools array (the directory-scan ResourceScanner step it briefly used
   in between has since been removed project-wide).
2. main_game.tscn -- HeroPortrait export fixed (was pointing to
   EnemySection/EnemyPortrait).
3. DungeonRow.OnDungeonCompleted -- also refreshes when completed
   dungeon is this row's RequiredDungeon.
4. UpgradeRow -- added DungeonCompleted subscription for tier-gate
   refresh.
5. UpgradeRow._ExitTree() -- fixed += to -= for DungeonCompleted
   (was causing memory leak on scene reload).
6. DungeonManager.OnLayerCleared -- CheckDungeonCompletion runs
   BEFORE LayerCleared signal emission (was causing UI desync).
7. BattleSystem.StartDungeonBattle -- null check on GetLayer
   instead of bounds check after call.
8. battle_text_library.tres -- filled 5 empty EnemyAttackLines.
9. UpgradeRow -- removed dead GetIndividualLockReason() method,
   cleaned ShowIndividualLocked.
10. DungeonRow cleared count -- clamped with Mathf.Min() to
    prevent exceeding totalLayers.
11. DungeonRow.OnActionPressed -- uses IsDungeonCompleted() check
    instead of bounds comparison.

---

## What Is Not Yet Implemented

Prestige/meta-progression system (Echo/Arete mechanics)

---

END OF KAR GODOT

# Kleos Architecture Reference -- Godot Edition
# KAR_Godot -- Updated April 23, 2026
# Engine: Godot 4.6.2 .NET (C#)
# Status: Port in progress -- core systems and UI complete, battle system pending

---

## About This Document

This is the technical architecture reference for the Godot port of Kleos.
It documents how each system is implemented in Godot, including class
structures, signal wiring, file paths, and Godot-specific patterns.

The Unity KAR (KAR_Updated_2026-03-20.md) remains the reference for
Unity-specific implementation. This document is independent and does not
duplicate Unity content. Only Godot architecture is recorded here.

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
    SettingsManager.cs
    SaveManager.cs
    UpgradeManager.cs
    KleosManager.cs
    ArtisanManager.cs
    HeroManager.cs
    DungeonManager.cs
    RandomEncounterManager.cs
    ResourceScanner.cs          (static utility, not an Autoload)
  Resources/
    ArtisanData.cs
    EnemyData.cs
    DungeonData.cs
    DungeonLayer.cs
    UpgradeConfig.cs
    HeroData.cs
    EncounterPool.cs
    EncounterPoolEntry.cs
    BattleTextLibrary.cs
    ModifierEffect.cs
    ModifierEnums.cs            (ModifierType and ModifierMode enums)
    HeroStat.cs                 (enum)
    Artisans/
      scribe.tres
      bard.tres
      potter.tres
      sculptor.tres
      playwright.tres
      historian.tres
    Enemies/
      1. Forest/
        1.wild_dog.tres
        2.wolf.tres
        3.wolf_pack.tres
        4.large_wolf.tres
        5.large_wolf_pack.tres
        6.nemean_lion_cub.tres
        7.nemean_lion.tres
    Dungeons/
      forest.tres
    Upgrades/
      1_01_scribes_quill.tres
      1_02_bronze_training.tres
      ... (24 total, prefixed by tier and order)
      3_07_coastal_plunder.tres
    EncounterPools/
      pool_forest.tres
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
      TierHeader.tscn
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
  8. RandomEncounterManager

Each uses a static Instance property with a guard in _Ready() that
calls QueueFree() if Instance is already set. This prevents duplicate
instantiation if the autoload node somehow appears twice.

---

## ResourceScanner (Static Utility)

Purpose: Shared directory scanner for loading all Resource files of a
given type from a folder. Used by ArtisanManager, DungeonManager, and
UpgradeManager to eliminate hardcoded resource paths.

File: res://Autoloads/ResourceScanner.cs (static class, not an Autoload)

Key method:
  static Array LoadAll<T>(string directoryPath) where T : Resource

Behavior:
  Opens directory via DirAccess.
  Iterates all files ending in .tres or .res.
  Loads each with GD.Load() (untyped).
  Uses pattern match (resource is T) to filter correct types.
  Skips files of wrong type silently instead of crashing.
  Returns untyped Array for Godot export compatibility.

The .res extension check handles exported builds where Godot packs
.tres files into binary .res format inside .pck archives.

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

State:
  ownedCounts (Dictionary, string to int)
  unlockedArtisans (List of string)
  hasInitialized (bool, guard flag)

Config loading:
  Uses ResourceScanner.LoadAll<ArtisanData>("res://Resources/Artisans/").
  SortByUnlockOrder() chains artisans by RequiredArtisanId: the artisan
  with no requirement (Scribe) comes first, then each artisan whose
  requirement is the previous one in the chain.

Key methods:
  PurchaseArtisan(ArtisanData) -- spends kleos, increments count,
    recalculates production, checks for new unlocks
  IsArtisanUnlocked(ArtisanData) -- checks unlock condition
  GetOwnedCount(string artisanId) -- returns count for given artisan
  GetCurrentCost(ArtisanData) -- BaseCost * CostMultiplier ^ owned
  CanPurchase(ArtisanData) -- checks unlocked and affordable
  RecalculateTotalProduction() -- sums all artisan output with modifiers
  RefreshUnlocks() -- checks all artisan unlock conditions after purchase
  GetArtisanById(string artisanId) -- returns ArtisanData by ID

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
  Uses ResourceScanner.LoadAll<UpgradeConfig>("res://Resources/Upgrades/").
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
  Uses ResourceScanner.LoadAll<DungeonData>("res://Resources/Dungeons/").
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
  OnLayerCleared(string dungeonId, int layerIndex) -- updates progress

Progress validation:
  Players must complete layers sequentially. Cannot skip ahead.

Save/Load:
  GetSaveData() returns DungeonSaveData (progress dictionary, completed list)
  LoadFromSaveData(DungeonSaveData) restores progress

---

### RandomEncounterManager

Singleton autoload. Multi-pool random encounter system.

Signals:
  EncounterTriggered(EnemyData enemy)

State:
  clickAccumulator (int)
  clickThreshold (int, random between 10 and 30)
  activePools (list, rebuilt when dirty flag is set)

Config loading:
  Encounter pools loaded via GD.Load from res://Resources/EncounterPools/.

Key methods:
  OnDeedClicked() -- increments counter, checks threshold
  RollNewThreshold() -- random int between configurable min/max
  RefreshPoolsIfDirty() -- rebuilds active pool list
  PickRandomPool() -- selects random pool from active pools
  PickRandomEnemy(EncounterPool) -- weighted random selection

Pool activation:
  Forest pool: always active (RequiredDungeon is null)
  Other pools: active when DungeonManager.IsDungeonCompleted() returns
  true for their RequiredDungeon reference.

Subscribes to DungeonManager.DungeonCompleted to mark pools dirty.

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

Key methods:
  SetMusicVolume(float) -- clamps, applies, saves
  SetSfxVolume(float) -- clamps, applies, saves
  SetFullscreen(bool) -- applies, saves
  SetResolutionIndex(int) -- applies, saves
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
Script exists. Asset .tres file not yet created.

### SaveData (Plain C# classes extending RefCounted)

  SaveData -- root container
    Version (string)
    LastSaveTime (long)
    Kleos (KleosSaveData)
    Artisans (ArtisanSaveData)
    Upgrades (UpgradeSaveData)
    Dungeons (DungeonSaveData)
    Hero (HeroSaveData)

Each sub-class extends RefCounted and contains only serializable
properties (strings, ints, floats, dictionaries, arrays).

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
          DeedButton, DeedContextLabel
        RightPanel (VBoxContainer)
          ArtisanScrollContainer > ArtisanList (VBoxContainer)
    HeroPanel (PanelContainer, overlay, hidden)
      VBoxContainer with stat rows, bars, upgrade buttons
    DungeonPanel (PanelContainer, overlay, hidden)
      ScrollContainer > DungeonList (VBoxContainer)
    UpgradePanel (PanelContainer, overlay, hidden)
      ScrollContainer > UpgradeList (VBoxContainer)
    FadeOverlay (ColorRect)

MainGameController responsibilities:
  Connects all button signals in _Ready().
  Subscribes to manager signals for display updates.
  Manages panel visibility (hero, dungeon, upgrade).
  Dungeon and Upgrade panels are mutually exclusive (ActivePanel enum).
  Spawns ArtisanRow instances into ArtisanList via PopulateArtisanList().
  Spawns DungeonRow instances into DungeonList via PopulateDungeonList().
  Spawns UpgradeRow and TierHeader instances into UpgradeList via
    PopulateUpgradeList().
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
  Buy button text "Hire", disabled only when unaffordable.

State transitions:
  Setup(ArtisanData) checks IsArtisanUnlocked and calls SetLocked or
  SetUnlocked accordingly.
  OnAnyArtisanPurchased(string) listens for ArtisanManager.ArtisanPurchased
  and re-checks unlock condition. If now unlocked, calls SetUnlocked().

Signal subscriptions:
  KleosManager.KleosChanged -- updates affordability and cost display.
  ArtisanManager.ArtisanPurchased -- checks if this row should unlock.

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
  KleosManager.KleosChanged -- refreshes if dungeon has kleos requirement.

OnActionPressed() currently prints to console. Will call BattleSystem
when implemented.

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

Lock checks mirror UpgradeManager logic:
  IsTierUnlocked() checks RequiredDungeon completion.
  IsIndividualLockMet() checks hero level, prerequisite upgrade,
    and artisan count.

Signal subscriptions:
  KleosManager.KleosChanged -- refreshes affordability.
  UpgradeManager.UpgradePurchased -- refreshes all rows (prerequisite
    checks may have changed).

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

---

## Signal Wiring Summary

KleosManager.KleosChanged:
  MainGameController.RefreshKleosDisplay
  ArtisanRow.OnKleosChanged (per row)
  UpgradeRow.OnKleosChanged (per row)
  DungeonRow.OnKleosChanged (per row)

KleosManager.KleosGained:
  HeroManager.OnKleosGained (XP tracking)

KleosManager.ProductionChanged:
  MainGameController.RefreshProductionDisplay

KleosManager.DeedContextChanged:
  MainGameController.RefreshDeedContext

ArtisanManager.ArtisanPurchased:
  ArtisanRow.OnAnyArtisanPurchased (per row, for self-unlock)

UpgradeManager.UpgradePurchased:
  UpgradeRow.OnAnyUpgradePurchased (per row, for prerequisite refresh)

DungeonManager.LayerCleared:
  DungeonRow.OnLayerCleared (per row, for progress refresh)

DungeonManager.DungeonCompleted:
  RandomEncounterManager.OnDungeonCompleted (marks pools dirty)

HeroManager.StatsChanged:
  MainGameController.RefreshHeroDisplay

HeroManager.LevelUp:
  MainGameController.OnHeroLevelUp

---

## Key Godot Patterns

Signals vs Unity Events:
  Godot uses [Signal] delegate declarations. Signal names are derived
  from the delegate name minus "EventHandler". Emitted via EmitSignal().
  Connected via += operator on the signal property. Disconnected via -=.

Resource vs ScriptableObject:
  Godot Resource classes replace Unity ScriptableObjects. Saved as .tres
  files. Created in editor via right-click > New Resource. Referenced by
  other resources or exported properties on nodes. [GlobalClass] attribute
  makes them visible in the Godot editor resource picker.

Autoload vs Singleton MonoBehaviour:
  Godot Autoloads are nodes added to the scene tree root before any
  scene loads. They persist across scene changes automatically (no
  DontDestroyOnLoad needed). Accessed via static Instance property.

Scene instantiation:
  PackedScene.Instantiate<T>() replaces Unity Instantiate(prefab).
  AddChild() replaces SetParent(). Node tree order determines render
  order (later children draw on top).

Typed array workaround:
  Godot C# does not support generic typed arrays in exported properties
  (Array<T> causes editor issues). Use untyped Array with .As<T>()
  helper methods for typed access. This affects DungeonData.Layers,
  EncounterPool.Entries, and UpgradeConfig.Effects.

Directory-based resource loading:
  ResourceScanner.LoadAll<T>() scans directories instead of hardcoding
  paths. New content is added by dropping .tres files into the correct
  folder. Each manager applies its own sort after scanning to guarantee
  correct display order.

---

## What Is Not Yet Implemented

Battle system (BattleSystem.cs equivalent -- the most complex piece)
Combat UI (battle panel, log, result overlay, speed toggle)
Brigands Pass and Coastal Caves .tres data files
Deed button visual evolution (tier-based appearance changes)
Flavor text floating notifications
Omen system
Object pooling for flavor text
Number formatting utility (NumberFormatter equivalent)
Battle text library .tres asset (script exists, asset not created)
Status effect and ability system (implemented in Unity, not ported)

---

END OF KAR GODOT

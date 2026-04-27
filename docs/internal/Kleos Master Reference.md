# Kleos Master Reference -- Godot Edition
# KMR_Godot -- Updated April 27, 2026
# Engine: Godot 4.6.2 .NET (C#)
# Status: Port in progress -- core systems, UI, and battle system complete

---

## About This Document

This is the gameplay and system reference for the Godot port of Kleos.
It documents what has been implemented, tested, and confirmed working
in the Godot project specifically.

All gameplay values (artisan costs, enemy stats, upgrade effects, hero
formulas) are unchanged from the Unity version. Refer to the Unity KMR
(KMR_Updated_2026-03-20.md) for balance tables and design rationale.
This document only records Godot-specific implementation details and
what has been confirmed functional.

---

## Port Status Overview

Autoload managers: All nine complete and tested (BattleSystem added).
Resource classes: All seven ported and functional.
Asset data (.tres files): Artisans complete, Forest dungeon complete,
  Forest encounter pool complete, 24 upgrade assets complete,
  BattleTextLibrary asset complete.
  Brigands and Coastal dungeon/encounter data pending.
Main Menu scene: Complete -- fade, prompt text, settings panel.
Game scene: Complete -- core layout with three-panel structure.
Artisan UI: Complete -- rows with locked/unlocked states, hire flow.
Hero portrait and panel: Complete -- compact display, full stat panel.
Dungeon UI: Complete -- DungeonRow with progress display and layer info.
Upgrade UI: Complete -- UpgradeRow with five visual states and tier headers.
Battle panel: Complete -- combat display, battle log, result screens,
  post-combat log, animations, text variety.

---

## Section 1 -- Kleos and Deeds

Base kleos per click: 1.
Click value formula: 1 + UpgradeManager.GetFlat(ModifierType.ClickFlat).

KleosManager.DoDeed() handles click logic. The Deed button in the game
scene calls DoDeed() and RandomEncounterManager.OnDeedClicked() on each
press. Godot Button.Pressed signal connects to a single method, so the
duplicate handler bug from Unity does not apply here.

Passive income accumulates via KleosManager._Process(). Each frame,
totalKleosPerSecond * delta is added to a passiveAccumulator. When the
accumulator reaches 1.0 or more, the integer portion is added as kleos
and the remainder rolls over.

Deed context descriptions update based on artisan count:
- 0 artisans: "Training in solitude..."
- 1+ artisans: "Inspiring scribes with your actions..."
- 2+ artisans: "Your deeds reach the ears of bards..."
- 3+ artisans: "Potters craft vessels depicting your trials..."
- 4+ artisans: "Sculptors immortalize your humble service..."
- 5+ artisans: "Playwrights chronicle your quiet heroism..."
- 6+ artisans: "Historians record your selfless acts..."

The DeedContextChanged signal fires on every deed so the UI label
updates automatically.

---

## Section 2 -- Artisans

Six artisans, each a separate .tres asset file. Passive kleos per
second generation. Purchase using kleos.

Artisan table (values unchanged from Unity KMR):

  Scribe    -- BaseCost 10,    KpS 0.2,  CostMult 1.18, always unlocked
  Bard      -- BaseCost 70,    KpS 0.5,  CostMult 1.18, requires 5 Scribes
  Potter    -- BaseCost 350,   KpS 1.2,  CostMult 1.20, requires 5 Bards
  Sculptor  -- BaseCost 1800,  KpS 4.0,  CostMult 1.20, requires 5 Potters
  Playwright-- BaseCost 9000,  KpS 10.0, CostMult 1.22, requires 5 Sculptors
  Historian -- BaseCost 40000, KpS 25.0, CostMult 1.22, requires 3 Playwrights

Cost formula: BaseCost * CostMultiplier ^ ownedCount.
Production formula: KleosPerSecond * ownedCount * GetMultiplier(ArtisanProductionMultiplier).

Unlock cascade: ArtisanManager.RefreshUnlocks() runs after every
purchase and checks all artisan unlock conditions.

Artisan UI (ArtisanRow):
All six artisans are visible at all times. Locked artisans appear
greyed out with the unlock requirement shown in place of the KpS label.
The Hire button shows "Locked" and is disabled. When the requirement is
met (detected via ArtisanPurchased signal), the row transitions to its
unlocked state without rebuilding the list.

Unlocked rows show the artisan name, KpS, current cost, owned count,
and a Hire button that disables when the player cannot afford it. The
affordability check refreshes on every KleosChanged signal.

---

## Section 3 -- Hero System

Four core attributes with Greek names:

  Strength  (Sthenos)     -- base 10, +1.0 damage per point
  Endurance (Karteria)    -- base 10, +5 max HP per point
  Cunning   (Metis)       -- base 5,  +1% dodge and crit per point
  Divine Favor (Charis Theon) -- base 0, +1% crit chance, +0.1x crit multiplier

Combat stat formulas (identical to Unity):
  Max HP: 40 + (Endurance * 5)
  Damage: 3 + (Strength * 1.0)
  Dodge Chance: Cunning * 0.01, max 30%
  Crit Chance: (Cunning * 0.005) + (Favor * 0.01), max 25%
  Crit Multiplier: 2.0 + (Favor * 0.1)

At level 1 with base stats: Max HP = 90, Damage = 13. Confirmed via
test output.

Leveling:
  XP to next level: 1000 * (1.5 ^ (level - 1))
  3 stat points awarded per level up.
  XP source: all kleos gained (clicks, passive income, battles).
  HeroManager subscribes to KleosManager.KleosGained signal.

Hero portrait (compact, top-left corner):
  Shows level number, HP bar, XP bar.
  Clicking opens the full Hero panel.

Hero panel (overlay):
  Shows all four stats with current values and upgrade buttons.
  Stat upgrade buttons disabled when no points available.
  Combat stats display (damage, dodge chance, crit chance).
  Panel toggles visibility on portrait click.

---

## Section 4 -- Dungeon System

Layer-based dungeon progression. Each dungeon is a DungeonData resource
containing an array of DungeonLayer entries. Each layer references an
EnemyData asset and defines a base kleos reward, boss flags, and flavor
text.

Forest of Trials: 10 layers, Wild Dog through Nemean Lion.
  Layer 9: Nemean Lion Cub (mini-boss).
  Layer 10: Nemean Lion (boss).
  All enemy stats configured in .tres files.

Brigands Pass and Coastal Caves: Enemy data defined in Unity KMR.
Godot .tres assets not yet created.

DungeonManager tracks progress as a dictionary of dungeon name to
highest cleared layer. Sequential access enforced. Progress persists
via save system.

Dungeon UI (DungeonRow):
Each dungeon displays as a row with dungeon name, progress (cleared
layers / total layers), next enemy info, and an action button.

Four visual states:
  Locked -- greyed out, shows lock reason, button disabled.
  Available -- normal color, shows "Enter" button, next enemy name.
  In Progress -- normal color, shows "Continue (Layer N)", next enemy.
  Completed -- green tint, shows "All trials conquered", button disabled.

Layer info shows enemy name with prefix based on layer type:
  Normal layers: "Next: Enemy Name"
  Mini-boss layers: "Mini-Boss: Enemy Name"
  Boss layers: "BOSS: Enemy Name"

DungeonRow refreshes on LayerCleared and KleosChanged signals.
Action button calls BattleSystem.StartDungeonBattle() with the
dungeon data and next layer index. Guards against battles already
in progress and completed dungeons.

PopulateDungeonList() in MainGameController spawns a DungeonRow for
each dungeon config. Rows fill the full width of the DungeonPanel.

---

## Section 5 -- Battle System (April 2026)

Battles triggered by entering dungeon layers via the DungeonRow
action button or by random encounter deed clicks. Turn-based combat
with hero attacking first each round (Greek tradition).

Battle Flow:
1. Setup: BattleContext created (dungeon or random encounter).
   Hero HP restored to full. BattlePanel opens with fade-in.
   Encounter flavor text shown in battle log.
2. Combat Loop: Alternating hero/enemy attacks with timed delays.
   Hero attacks first, then enemy retaliates after 0.4s delay.
   0.4s delay between rounds. Dodge and crit rolls each attack.
3. Resolution: Victory or defeat detected when HP reaches zero.
   0.6s final blow pause before result screen fade-in.
4. Rewards: Victory grants kleos. Defeat restores hero to full HP.
   Dungeon victories advance layer progress.
5. Dismissal: Player presses CLAIM GLORY or RETREAT to close panel.

Battle Context:
Two factory methods create the appropriate context:
  CreateDungeonBattle(dungeon, layerIndex, layer) -- sets dungeon
    metadata, layer reward, boss flags, header text as
    "{Dungeon Name} - Layer {N}".
  CreateRandomEncounter(enemy, poolName) -- sets enemy reward,
    header text as "Random Encounter", pool name for theming.

Battle Sources:
  Dungeon: DungeonRow action button triggers
    BattleSystem.StartDungeonBattle().
  Random Encounter: RandomEncounterManager.EncounterTriggered
    signal triggers BattleSystem.StartRandomEncounterBattle().
    Signal now passes pool name alongside enemy data for theming.

Reward Calculation (DungeonRewardCalculator):
  Dungeon rewards (deterministic):
    Total = (BaseReward + LayerBonus + UpgradeBonus) * BossMultiplier
    LayerBonus = BaseReward * (LayerIndex * 0.10)
    BossMultiplier = 2.0 for boss layers, 1.0 otherwise
    UpgradeBonus = GetFlat(ModifierType.BattleRewardFlat)
  Random encounter rewards (RNG variance table):
    Total = (BaseReward + UpgradeBonus) * LuckMultiplier
    Luck roll: 40% = 1x, 20% = 2x, 15% = 3x, 10% = 4x,
      5% = 6x, 5% = 10x, 5% = 20x.
    WasLucky flag and LuckMultiplier stored for result screen.

Enemy Damage Calculation:
  Damage per hit = DamagePerSecond / AttackRate.
  Preserves intended damage output from the original enemy
  balance tables in the turn-based system.

Post-Victory Healing:
  If PostVictoryHealPercent upgrade modifier is purchased,
  hero heals that percentage of max HP after winning.

Post-Defeat:
  Hero HP restored to full. No reward. No dungeon advancement.

Battle Panel UI:
  Full-screen overlay (Control node, not PanelContainer).
  Sits after UpgradePanel and before FadeOverlay in scene tree.

  Layout (asymmetric):
    Hero (bottom-left): HP text, HP bar, Portrait, Name, Level.
    Enemy (top-right): Name, Portrait, HP bar, HP text.
    Battle log (center-bottom): 4 visible lines.
    Encounter header (top-center): dungeon name or "RANDOM ENCOUNTER".
    Speed toggle (top-left): hidden until upgrade purchased.

  Background Theming:
    ColorRect background tint changes per dungeon or encounter pool.
    Forest: dark green (0.12, 0.18, 0.10).
    Road/Brigands: warm brown (0.20, 0.17, 0.12).
    Coast/Caves: dark blue (0.10, 0.15, 0.22).
    Default: dark brown (0.15, 0.13, 0.11).
    Theme resolved by string matching on dungeon ID or pool name.

  Battle Log:
    4 visible lines during combat, scrolling as new entries appear.
    Hero actions left-aligned in copper (C87840).
    Enemy actions right-aligned in red (C84030).
    Critical hits highlighted in gold (FFD700).
    Dodge lines displayed in steel grey (8A9AAA).
    Older visible lines fade via alpha (newest 100%, oldest 40%).
    Newest line fades in from transparent (0.15s tween).
    Full history stored for post-combat log.

  Result Screen:
    Victory: subtitle (random from pool), VICTORY title in gold,
      flavor text (random from pool with enemy name), formatted
      reward with N0 number formatting, luck bonus display for
      RNG multiplier > 1x, battle summary (rounds, HP remaining,
      crits, dodges), CLAIM GLORY button.
    Defeat: subtitle (random from pool), DEFEAT title in dark red,
      flavor text (random from pool with enemy name), battle summary
      (rounds, enemy HP remaining), consolation quote (random from
      pool), RETREAT button.
    VIEW BATTLE LOG button: opens scrollable full combat history
      with left/right alignment matching the live log.
    BACK TO RESULTS button: returns to result screen.
    Result overlay fades in over 0.4s.

  Animations:
    Portrait attack nudge: attacker slides 20px toward opponent
      then snaps back (0.12s each direction, Quad easing).
    Portrait damage shake: defender shakes horizontally 6px,
      3 oscillations (0.06s each).
    HP bar tween: smooth transition to new value (0.3s, Quad Out).
    Panel open/close: fade in/out over 0.25s.
    All animation durations scale with battle speed multiplier.

  Battle Speed Toggle:
    Button cycles x1, x2, x4.
    Only visible when BattleSpeedX2Unlocked upgrade purchased.
    x4 requires BattleSpeedX4Unlocked upgrade.
    All timer delays and animation durations divided by multiplier.

  Combat Text (BattleTextLibrary):
    9 hero attack lines, 7 crit lines, 7 dodge lines.
    7 generic enemy attack lines (with {0} for enemy name).
    5 enemy anticipation lines (reserved for future use).
    6 victory lines, 4 victory subtitles.
    5 defeat lines, 4 defeat subtitles, 6 consolation quotes.
    All Greek-themed, editable in Inspector.
    Fallback strings if library not loaded.

  Enemy-Specific Attack Lines:
    EnemyData has AttackLines array for per-enemy combat text.
    Priority: enemy-specific line, then generic pool, then fallback.
    Allows unique flavor per creature (dog bites, crab pincers, etc).

  Encounter Flavor Text:
    EnemyData.EncounterFlavorTexts array.
    Random line shown when combat begins.
    Falls back to "{EnemyName} blocks your path..." if empty.

---

## Section 6 -- Upgrade System

Centralized modifier system using ModifierType enum and ModifierEffect
resources. UpgradeManager provides GetFlat() and GetMultiplier() methods
queried by all other systems.

ModifierType values include: ClickFlat, BattleRewardFlat,
BattleCritMultiplier, ArtisanProductionMultiplier, XPMultiplier,
HeroStrengthFlat, HeroMaxHPFlat, HeroDodgeChance, HeroCritChance,
HeroCritMultiplier, BattleSpeedX2Unlocked, BattleSpeedX4Unlocked,
PostVictoryHealPercent, StatPointsPerLevel, ClickMultiplier.

ModifierMode: Flat or Multiplier.

24 upgrade .tres assets created across 3 tiers:

  Tier 1 -- Trials of the Forest (no dungeon gate, 10 upgrades):
    Scribe's Quill, Bronze Training, Inspiring Presence,
    Bard's Inspiration, Blessed Growth, Spartan's Endurance,
    Potter's Craft, Warrior's Discipline, Olympian Strike,
    Echoing Deed. Costs range 50 to 3,500 kleos.

  Tier 2 -- Trials of the Road (requires Brigands Pass, 7 upgrades):
    Stolen Blade, Spoils of the Road, Scribe's Discipline,
    Bard's War Song, Road-Hardened, Brigand's Cunning,
    Victor's Instinct. Costs range 1,500 to 5,000 kleos.
    Dungeon gate left null until brigands.tres is created.

  Tier 3 -- Trials of the Shore (requires Coastal Caves, 7 upgrades):
    Poseidon's Tide, Sailor's Fortune, Potter's Legacy,
    Sculptor's Vision, Sea-Hardened Body, Tidal Instinct,
    Coastal Plunder. Costs range 6,000 to 20,000 kleos.
    Dungeon gate left null until coastal.tres is created.

Upgrade UI (UpgradeRow):
Each upgrade displays as a row with name, cost, description, lock
reason, and a purchase button.

Five visual states:
  Affordable -- full color, button enabled, text "Purchase".
  Unaffordable -- dimmed, button disabled, text "Purchase".
  Purchased -- green tint, button disabled, text "Purchased".
  Tier Locked -- dark grey, shows dungeon requirement, text "Locked".
  Individual Locked -- warm brown, shows specific reason, text "Locked".

Tier headers (TierHeader scene) inserted between upgrade groups.
UpgradeRow refreshes on KleosChanged and UpgradePurchased signals.
Purchasing an upgrade triggers ArtisanManager.RecalculateTotalProduction()
to immediately apply production modifiers.

PopulateUpgradeList() in MainGameController spawns tier headers and
upgrade rows sorted by tier then cost.

---

## Section 7 -- Random Encounters

Triggered by deed clicks. RandomEncounterManager tracks a click counter
and rolls against a threshold (configurable range, default 10-30).

Multi-pool system: multiple EncounterPool assets, each gated by a
dungeon completion requirement. Forest pool is always active (no gate).
When an encounter triggers, a random active pool is selected, then a
random enemy from that pool using weighted selection.

Forest encounter pool configured:
  Wild Dog (weight 3.0), Wild Boar (3.0), Wolf (2.5),
  Wolf Pack (1.5), Large Wolf (1.0).

Brigands and Coastal pools: not yet created as .tres assets.

---

## Section 8 -- Save System

JSON file-based persistence using Godot FileAccess and user:// path.

SaveData structure:
  version (string)
  lastSaveTime (string, ISO format)
  KleosSaveData: currentKleos
  ArtisanSaveData: owned counts dictionary
  UpgradeSaveData: purchased upgrade IDs list
  DungeonSaveData: progress dictionary
  HeroSaveData: level, XP, stat points, stat upgrade counts

SaveManager methods:
  Save(SaveData) -- serializes to JSON, writes to user://kleos_save.json
  Load() -- reads file, deserializes, returns SaveData
  HasSaveData() -- checks file existence (used by main menu)
  DeleteSaveData() -- removes save file

All managers implement GetSaveData() and LoadFromSaveData() methods.
hasInitialized guard flags on ArtisanManager, UpgradeManager, and
HeroManager prevent race conditions between load and _Ready().

Tested: save/load round-trip confirmed via console output.

---

## Section 9 -- Settings System

SettingsManager is a persistent Autoload (does not use DontDestroyOnLoad
since Godot autoloads persist automatically across scenes).

Stores settings in a separate JSON file at user://kleos_settings.json.

Settings:
  Music volume (float, 0-1)
  SFX volume (float, 0-1)
  Fullscreen (bool)
  Resolution index (int)

SettingsUI provides sliders for audio, a fullscreen toggle, a resolution
dropdown, and a delete save button with confirmation dialog.

---

## Section 10 -- Main Menu

Separate scene (res://Scenes/MainMenu/main_menu.tscn), set as the main
scene in Project Settings.

Title "KLEOS" displayed at center.
Prompt text below title:
  First play (no save file): "Begin Your Journey"
  Returning plays: random from pool ("Continue Your Deeds",
    "Continue Your Journey", "Amuse the Gods", "The Fates Await",
    "Glory Calls Once More")

Pulse animation on prompt text via sine wave (alpha oscillates between
0.3 and 1.0).

Click anywhere or press Enter/Space to start.
Fade to black, then async scene change to the game scene.

Settings button opens settings panel. While settings panel is open,
clicks do not trigger game start.

---

## Section 11 -- Game Scene Layout

Scene file: res://Scenes/Game/main_game.tscn
Controller script: MainGameController.cs

Layout structure:

  MainGame (Control, full screen)
    Background (ColorRect)
    TopBar (HBoxContainer)
      HeroPortrait -- compact, top left
      KleosLabel
      ProductionLabel
    MainPanel (HBoxContainer)
      LeftPanel (VBoxContainer, fixed width)
        TopSpacer
        DungeonButton
        MiddleSpacer
        UpgradeButton
        BottomSpacer
      CenterPanel (VBoxContainer, expands)
        DeedButton
        DeedContextLabel
      RightPanel (VBoxContainer, fixed width)
        ArtisanScrollContainer
          ArtisanList (VBoxContainer, artisan rows spawned here)
    HeroPanel (PanelContainer, overlay, starts hidden)
    DungeonPanel (PanelContainer, overlay, starts hidden)
      ScrollContainer > DungeonList
    UpgradePanel (PanelContainer, overlay, starts hidden)
      ScrollContainer > UpgradeList
    BattlePanel (Control, overlay, starts hidden)
      BattleBackground (ColorRect)
      CombatArea (Control)
        EncounterHeaderLabel, HeroSection, EnemySection,
        BattleLogContainer (4 Labels), SpeedToggleButton
      ResultOverlay (Control, hidden)
        ResultContent (VBoxContainer with result labels and buttons)
      PostCombatLogOverlay (Control, hidden)
        LogMargin (MarginContainer)
          PostCombatLogScroll > PostCombatLogList
        BackToResultsButton
    FadeOverlay (ColorRect)

Dungeon and Upgrade panels are mutually exclusive -- opening one closes
the other. Both toggle on their respective button press. HeroPanel
toggles independently on portrait click.

Artisan rows are always visible in the RightPanel. All six rows spawn
on scene load. Locked artisans are greyed out with requirements shown.

Dungeon rows spawn into DungeonList when scene loads. One row per
dungeon config. Rows fill full panel width.

Upgrade rows spawn into UpgradeList when scene loads. Tier headers
inserted between tier groups. Sorted by tier then cost.

Fade overlay handles scene-in transition (black to transparent).

---

## Section 12 -- Resource Loading

All managers use ResourceScanner.LoadAll<T>() to scan directories for
.tres and .res files instead of hardcoding paths. This means adding
new content only requires dropping a .tres file into the correct folder.

ResourceScanner is a static utility class (not an Autoload). It uses
DirAccess to list directory contents, loads each file with GD.Load(),
and uses pattern matching (resource is T) to skip files of the wrong
type. This prevents crashes from stray files in resource folders.

Each manager applies its own sort after scanning:
  ArtisanManager -- sorts by unlock chain (Scribe first, then each
    artisan whose requirement is the previous one).
  DungeonManager -- sorts by progression chain (dungeons with no
    RequiredDungeon first, then chained by RequiredDungeon reference).
  UpgradeManager -- sorts by tier first, then by cost within each tier.

Resource directories:
  res://Resources/Artisans/   -- 6 artisan .tres files
  res://Resources/Dungeons/   -- dungeon .tres files (currently: forest)
  res://Resources/Enemies/    -- enemy .tres files organized by dungeon
  res://Resources/Upgrades/   -- 24 upgrade .tres files
  res://Resources/EncounterPools/ -- encounter pool .tres files

---

## Integration Notes

KleosManager.KleosChanged signal drives:
  KleosLabel text update, ArtisanRow affordability checks,
  UpgradeRow affordability checks, DungeonRow unlock checks.

KleosManager.KleosGained signal drives:
  HeroManager XP tracking.

KleosManager.ProductionChanged signal drives:
  ProductionLabel text update.

KleosManager.DeedContextChanged signal drives:
  DeedContextLabel text update.

ArtisanManager.ArtisanPurchased signal drives:
  ArtisanRow unlock checks (locked rows self-unlock when conditions met),
  production recalculation.

UpgradeManager.UpgradePurchased signal drives:
  UpgradeRow refresh (prerequisite checks, purchased state).

DungeonManager.LayerCleared signal drives:
  DungeonRow progress and state refresh.

DungeonManager.DungeonCompleted signal drives:
  RandomEncounterManager (marks pools dirty).

HeroManager.StatsChanged signal drives:
  Hero panel stat display refresh, hero portrait bar updates.

HeroManager.LevelUp signal drives:
  Stat point notification, hero panel refresh.

---

## What Is Not Yet Implemented

Brigands Pass and Coastal Caves .tres data files.
Deed button visual evolution (Bronze/Silver/Gold/Divine tiers).
Flavor text floating notifications.
Omen system pre-battle warnings.
Status effect and ability system (implemented in Unity, not ported).
Prestige/meta-progression (Echo/Arete mechanics).

---

END OF KMR GODOT
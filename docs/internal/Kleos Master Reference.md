# Kleos Master Reference -- Godot Edition
# KMR_Godot -- Updated April 23, 2026
# Engine: Godot 4.6.2 .NET (C#)
# Status: Port in progress -- core systems and UI complete, battle system pending

---

## About This Document

This is the gameplay and system reference for the Godot port of Kleos.
It documents what has been implemented, tested, and confirmed working
in the Godot project specifically.
Essentially the source of truth for game behavior.

All gameplay values (artisan costs, enemy stats, upgrade effects, hero
formulas) are unchanged from the Unity version. Refer to the Unity KMR
(KMR_Updated_2026-03-20.md) for balance tables and design rationale.
This document only records Godot-specific implementation details and
what has been confirmed functional.

---

## Port Status Overview

Autoload managers: All eight complete and tested.
Resource classes: All seven ported and functional.
Asset data (.tres files): Artisans complete, Forest dungeon complete,
  Forest encounter pool complete, 24 upgrade assets complete.
  Brigands and Coastal dungeon/encounter data pending.
Main Menu scene: Complete -- fade, prompt text, settings panel.
Game scene: Complete -- core layout with three-panel structure.
Artisan UI: Complete -- rows with locked/unlocked states, hire flow.
Hero portrait and panel: Complete -- compact display, full stat panel.
Dungeon UI: Complete -- DungeonRow with progress display and layer info.
Upgrade UI: Complete -- UpgradeRow with five visual states and tier headers.
Battle panel: Not started.

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
Action button prints to console until BattleSystem is implemented.

PopulateDungeonList() in MainGameController spawns a DungeonRow for
each dungeon config. Rows fill the full width of the DungeonPanel.

---

## Section 5 -- Upgrade System

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

## Section 6 -- Random Encounters

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

## Section 7 -- Save System

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

## Section 8 -- Settings System

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

## Section 9 -- Main Menu

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

## Section 10 -- Game Scene Layout

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

## Section 11 -- Resource Loading

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

Battle panel and combat UI (the most complex UI piece).
Brigands Pass and Coastal Caves .tres data files.
Deed button visual evolution (Bronze/Silver/Gold/Divine tiers).
Flavor text floating notifications.
Omen system pre-battle warnings.
Battle text library .tres asset (script exists, asset not created).
Status effect and ability system (implemented in Unity, not ported).

---

END OF KMR GODOT

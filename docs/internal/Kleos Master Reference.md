# Kleos Master Reference -- Godot Edition
# KMR_Godot -- Updated June 18, 2026
# Engine: Godot 4.6.2 .NET (C#)
# Status: Combat RPG system complete, Combat Arts UI, NumberFormatter,
#   Deed Button Visual Evolution, artisan unlock rebalance,
#   FlavorTextManager, Omen system, artisan rounded bulk purchase

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

Autoload managers: All eleven complete and tested (HeroAbilityManager
  added May 2026).
Resource classes: All ten ported and functional (added CombatAbility,
  AbilityEffect, HeroAbilityDatabase May 2026).
Asset data (.tres files): Artisans complete, all three dungeons complete,
  all three encounter pools complete, 24 upgrade assets complete,
  BattleTextLibrary asset complete, 20 enemy ability assets complete,
  9 hero ability assets complete, hero ability database complete.
  Tier 2 and Tier 3 upgrade dungeon gates wired to brigands.tres
  and coastal.tres.
Main Menu scene: Complete -- fade, prompt text, settings panel,
  scientific notation toggle.
Game scene: Complete -- core layout with three-panel structure,
  Combat Arts panel, Deed Button visual evolution.
Artisan UI: Complete -- rows with locked/unlocked states, hire flow,
  rounded bulk purchase (x1/x10) with pinned multiplier button.
  Unlock chain rebalanced June 2026.
Hero portrait and panel: Complete -- compact display, full stat panel.
Dungeon UI: Complete -- DungeonRow with progress display and layer info.
Upgrade UI: Complete -- UpgradeRow with five visual states and tier headers.
Battle panel: Complete -- combat display, battle log, result screens,
  post-combat log, animations, text variety, status effect display,
  ability name and flavor text log lines.
Combat Arts panel: Complete -- 9 hero abilities with type badges,
  auto-generated descriptions, purchase flow, three sections.
DevConsole: Complete -- backtick toggle, 17 commands, command history.

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

Artisan table (unlock chain rebalanced June 2026):

  Scribe    -- BaseCost 10,    KpS 0.2,  CostMult 1.18, always unlocked
  Bard      -- BaseCost 70,    KpS 0.5,  CostMult 1.18, requires 10 Scribes
  Potter    -- BaseCost 350,   KpS 1.2,  CostMult 1.20, requires 15 Bards
  Sculptor  -- BaseCost 1800,  KpS 4.0,  CostMult 1.20, requires 20 Potters
  Playwright-- BaseCost 9000,  KpS 10.0, CostMult 1.22, requires 15 Sculptors
  Historian -- BaseCost 40000, KpS 25.0, CostMult 1.22, requires 10 Playwrights

These six artisans are all early-game content. Additional artisan tiers
for mid and late game are planned for future development.

Cost formula: BaseCost * CostMultiplier ^ ownedCount.
Production formula: KleosPerSecond * ownedCount * GetMultiplier(ArtisanProductionMultiplier).

Bulk purchase (added June 2026):
A global buy multiplier (x1 / x10) applies to every artisan row. The
multiplier is held on ArtisanManager and cycled by a single button pinned
to the artisan panel footer. A third x100 tier exists in the cycle but is
gated behind IsBuyMultiplierUnlocked and stays locked until the "the
Tireless" deed epithet ships in V0.95; locked tiers are skipped when
cycling.

Rounded bulk purchase: x10 does not buy ten more, it buys up to the next
clean multiple of ten. Owning 8 with x10 buys 2 (reaching 10); owning 15
buys 5 (reaching 20); owning 0 buys the full 10. GetRoundedQuantity
returns this count; x1 always returns 1.

Bulk cost is a geometric series, not the current price times the quantity,
since each hire raises the next price by CostMultiplier:
  total = first * (CostMultiplier ^ quantity - 1) / (CostMultiplier - 1)
where first is the cost at the current owned count. GetBulkCost computes
this. Buying is all-or-nothing on the rounded quantity: the Hire button is
enabled only when the player can afford the whole batch.

Unlock cascade: ArtisanManager.RefreshUnlocks() runs after every
purchase and checks all artisan unlock conditions. A bulk purchase fires
ArtisanPurchased once for the batch and runs the cascade a single time.

Artisan UI (ArtisanRow):
All six artisans are visible at all times. Locked artisans appear
greyed out with the unlock requirement shown in place of the KpS label.
The Hire button shows "Locked" and is disabled. When the requirement is
met (detected via ArtisanPurchased signal), the row transitions to its
unlocked state without rebuilding the list.

Unlocked rows show the artisan name, KpS, current bulk cost, owned count,
and a Hire button that disables when the player cannot afford the rounded
batch. In x1 mode the button reads "Hire"; in x10 mode it shows the actual
rounded count, for example "Hire 2" when owning 8. The cost label reflects
the rounded batch cost. Affordability refreshes on every KleosChanged
signal, and the full row refreshes when the buy multiplier changes
(BuyMultiplierChanged signal).

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

Three dungeons implemented, each with 10 layers:

Forest of Trials (forest.tres):
  Unlocked from start. No requirements.
  Layers: Wild Dog, Wild Dog, Wolf, Wolf Pack, Wolf Pack,
	Large Wolf, Large Wolf, Large Wolf Pack,
	Nemean Lion Cub (mini-boss), Nemean Lion (boss).
  Enemy stats: HP 135-800, DPS 4-10, AttackRate 1.5-1.9.

Brigands' Pass (brigands.tres):
  Requires: Forest completed, 500 kleos.
  Layers: Road Thief, Bandit Lookout, Outlaw Peltast,
    Bandit Hoplite, Rogue Mercenary, Outlaw Peltast Band,
    Bandit Champion, War Hounds,
    The Pine-Bender (mini-boss), The Archilestes (boss).
  Enemy stats: HP 900-3800, DPS 5.5-15, AttackRate 1.6-2.1.

Coastal Caves (coastal.tres):
  Requires: Brigands' Pass completed, 2000 kleos.
  Layers: Shore Crab, Reef Serpent, Drowned Sailor,
	Reef Serpent Pair, Siren Thrall, Sea Hag,
	Coastal Chimera, Scylla Spawn,
	Charybdis Maw (mini-boss), The Siren Queen (boss).
  Enemy stats: HP 3000-9500, DPS 14-32, AttackRate 2.0-2.5.

HP scaling is steeper than DPS scaling within each dungeon. This
encourages Endurance investment and makes fights feel long when
underleveled but manageable once the hero has grown.

DungeonManager tracks progress as a dictionary of dungeon name to
highest cleared layer. Sequential access enforced. Progress persists
via save system.

Signal ordering: CheckDungeonCompletion runs BEFORE LayerCleared
signal emission in OnLayerCleared(). This ensures the completed flag
is set before any UI responds to the layer change.

ForceCompleteDungeon(string dungeonId) is a DEV API that sets
progress to the final layer and emits DungeonCompleted then
LayerCleared. Used by DevConsole.

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

DungeonRow refreshes on LayerCleared, DungeonCompleted, and
KleosChanged signals. OnDungeonCompleted also checks if the completed
dungeon is this row's RequiredDungeon, so downstream dungeons unlock
immediately.

OnActionPressed() calls BattleSystem.StartDungeonBattle() with the
dungeon data and next layer index. Guards against battles already
in progress and completed dungeons. Uses IsDungeonCompleted() check
instead of bounds comparison. Progress display clamped with
Mathf.Min() to prevent exceeding total layers.

PopulateDungeonList() in MainGameController spawns a DungeonRow for
each dungeon config. Rows fill the full width of the DungeonPanel.

---

## Section 5 -- Battle System

Battles triggered by entering dungeon layers via the DungeonRow
action button or by random encounter deed clicks. Turn-based combat
with hero attacking first each round (Greek tradition).

Battle Flow:
1. Setup: BattleContext created (dungeon or random encounter).
   Hero HP restored to full. Status effect managers initialized.
   Ability resolvers initialized. BattlePanel opens with fade-in.
   Encounter flavor text shown in battle log.
2. Combat Loop: Each round:
   a. Status effects process at round start (DoT ticks, HoT ticks,
      duration decrements, expiry). Death check after DoT.
   b. Ability resolver cooldowns advance.
   c. Hero turn: stun check, ability resolution, attack with
      modified stats through damage pipeline.
   d. Death check after hero turn.
   e. Enemy turn: stun check, ability resolution, attack with
      modified stats through damage pipeline.
   f. Death check after enemy turn.
   g. End-of-round processing.
3. Resolution: Victory or defeat detected when HP reaches zero.
   0.6s final blow pause before result screen fade-in.
   Status effects and ability resolvers cleared.
4. Rewards: Victory grants kleos. Defeat restores hero to full HP.
   Dungeon victories advance layer progress.
5. Dismissal: Player presses CLAIM GLORY or RETREAT to close panel.

Damage Pipeline (full, with status effect integration):
  1. Attacker base damage (hero stats or enemy DPS/rate).
  2. attackerEffects.GetModifiedDamage() applies AttackDamageUp,
     AttackDamageDown, and WeaponSteal modifiers.
  3. Dodge check: defenderEffects.GetModifiedDodgeChance().
  4. If dodged: log and skip.
  5. Crit immunity check: defenderEffects.HasCritImmunity().
  6. Crit roll: attackerEffects.GetModifiedCritChance().
  7. Apply crit multiplier if successful.
  8. Shield absorption: defenderEffects.AbsorbDamage().
  9. Remainder hits HP.
  10. Damage reflect: defenderEffects.GetReflectPercent().
  11. Log everything with appropriate colors and alignment.

Battle Context:
Two factory methods create the appropriate context:
  CreateDungeonBattle(dungeon, layerIndex, layer) -- sets dungeon
    metadata, layer reward, boss flags, header text.
  CreateRandomEncounter(enemy, poolName) -- sets enemy reward,
    header text as "Random Encounter", pool name for theming.

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

Battle Panel UI:
  Full-screen overlay (Control node).
  Background theming: Forest green, Brigands brown, Coastal blue.
  Hero (bottom-left): Name, Level, HP bar, HP text, Portrait,
    StatusEffectDisplay (below HP).
  Enemy (top-right): Portrait, Name, HP text, HP bar,
    StatusEffectDisplay (below HP bar).
  Battle log (center-bottom): 4 visible lines.
  Speed toggle (top-left): x1/x2/x4 cycle.

Battle Log:
  Hero actions left-aligned in copper (C87840).
  Enemy actions right-aligned in red (C84030).
  Critical hits highlighted in gold (FFD700).
  Dodges in steel grey (8A9AAA).
  Ability names: aligned with caster side, colored per ability.
  Cast flavor text: aligned with caster side.
  Status tick/expiry: centered in muted olive (8A8A6A / 6A6A5A).
  Older visible lines fade via alpha (newest 100%, oldest 40%).
  Full history stored for post-combat log with center alignment
  tracking per line.

StatusEffectDisplay:
  VBoxContainer with StatusEffectDisplay.cs script attached.
  One instance below hero HP, one below enemy HP.
  Shows active effects as small text labels with duration countdown.
  Buffs in olive (7A8A3A), debuffs in red (8A3A3A).
  Format: "EffectName (N)" with stack indicator "x2" if stacked.
  Refreshes on round start and after every attack.
  Cleared on battle start and battle end.

Animations (unchanged from previous):
  Portrait attack nudge, damage shake, HP bar tween, panel fade.
  All durations scale with battle speed multiplier.

---

## Section 5a -- Status Effects (May 2026)

Temporary modifiers on a combatant during battle. Managed by
StatusEffectManager, one instance per combatant, created at battle
start, cleared at battle end.

StatusEffectType enum (17 types):
  Stat Modifiers: AttackDamageUp, AttackDamageDown, AttackSpeedUp,
    AttackSpeedDown, CritChanceUp, CritImmunity, DodgeUp, DodgeDown.
  Damage Over Time: Bleed, Poison, Burn.
  Healing Over Time: Regeneration.
  Defensive: Shield, DamageReflect, DamageAbsorb.
  Special: Stun, WeaponSteal.

StatusEffectMode: Flat or Percentage.

StatusEffect data class fields:
  Type, EffectName, Value, Duration, MaxStacks, CurrentStacks,
  IsDebuff, Mode, SourceId, ApplyFlavorText, TickFlavorText,
  ExpireFlavorText.

Stacking rules:
  Same effect from same source: refreshes duration, no new stacks.
  Same effect from different sources: stacks up to MaxStacks.
  Different effect types always coexist.

Processing order each round:
  1. Start of round: tick DoTs/HoTs, reduce durations, expire.
  2. Attacker turn: check Stun, apply stat modifiers to attack.
  3. Damage dealt: Shield absorb, then HP, then DamageReflect.
  4. End of round: reserved for future triggers.

StatusEffectManager methods:
  ApplyEffect, RemoveEffect, ProcessStartOfRound, ProcessEndOfRound,
  GetModifiedDamage, GetModifiedDodgeChance, GetModifiedCritChance,
  HasEffect, HasCritImmunity, IsStunned, GetShieldAmount,
  AbsorbDamage, GetReflectPercent, GetActiveEffects, ClearAll.

All modifier methods return base values unchanged when no effects
are active, preserving identical behavior to pre-status-effect combat.

---

## Section 5b -- Combat Abilities (May 2026)

Actions a combatant uses instead of or alongside normal attacks.
Defined as CombatAbility Resource files (.tres).

AbilityEffectType enum:
  DealDamage, ApplyStatus, ApplySelfStatus, HealSelf, HealTarget,
  RemoveDebuff, RemoveBuff.

AbilityTrigger enum:
  OnCooldown, WhenHPBelow, WhenHPAbove, WhenTargetHPBelow,
  FirstRound, EveryNRounds.

CombatAbility Resource fields:
  Identity: AbilityId, AbilityName, AbilityDescription, AbilityColor.
  Trigger: Trigger, TriggerValue, CooldownRounds, ReplacesAttack,
    OneTimeUse.
  AI: Priority (higher = checked first), UseChance (0-1 probability).
  Use Conditions: CheckTargetHasNoEffect, RequiresTargetNoEffect,
    CheckSelfHasEffect, RequiresSelfEffect.
  Hero Unlock: UnlockAtLevel, KleosPurchaseCost, UnlockFromDungeonId.
  Flavor: CastFlavorText.
  Effects: Array of AbilityEffect resources.

AbilityEffect Resource fields:
  EffectType, Target, Value.
  Status fields: StatusType, StatusName, StatusValue, StatusDuration,
    StatusIsDebuff, StatusMode, StatusMaxStacks.
  Flavor: StatusApplyText, StatusTickText, StatusExpireText.

AbilityResolver (one instance per combatant):
  Filters abilities by: off cooldown, trigger condition met, use
  conditions met, not already used if OneTimeUse.
  Sorts by Priority descending.
  Rolls UseChance from highest priority down.
  Returns first ability that passes, or null for normal attack.
  Tracks cooldowns and one-time-use state per battle. Resets on
  battle end.

No behavior tree, no state machine. Priority-sorted list is sufficient
for single-target turn-based combat. Confirmed in Unity testing.

Ability execution in BattleSystem:
  Logs ability name (aligned with caster, ability color).
  Logs cast flavor text (aligned with caster).
  Processes each AbilityEffect: DealDamage routes through shield
  absorption, ApplyStatus/ApplySelfStatus creates StatusEffect
  instances, HealSelf/HealTarget restores HP, RemoveDebuff/RemoveBuff
  removes specific effect types.

BattleLogEntry fields for ability display:
  RichTextOverride: pre-formatted text bypasses normal log formatting.
  AlignCenter: forces center alignment (for status messages).
  OverrideColor: ability-specific color for name lines.

---

## Section 5c -- Enemy Abilities (May 2026)

20 enemy ability .tres files across three dungeons. Assigned to
EnemyData via the Abilities export array. Enemies without abilities
use normal attacks only.

Forest of Trials:
  Wolf: Howl (self buff, +25% damage, below 60% HP).
  Wolf Pack: Coordinated Strike (bonus damage every 3 rounds).
  Large Wolf: Savage Lunge (damage + bleed, below 50% HP, one-time).
  Nemean Lion Cub: Young Roar (debuff hero damage, first round).
  Nemean Lion: Impervious Hide (50 damage shield, first round) +
    Thunderous Roar (debuff hero damage and dodge, below 50% HP).

Brigands' Pass:
  Road Thief: Dirty Trick (dodge reduction, periodic).
  Bandit Hoplite: Shield Wall (30 damage shield, below 60% HP).
  Rogue Mercenary: Poisoned Blade (poison DoT, only when hero not
	already poisoned).
  Pine-Bender: Bend the Pine (burst damage + stun, below 70% HP).
  Archilestes: Pickpocket (weapon steal, first round) + Brigand's
    Cunning (self dodge + damage buff, below 30% HP).

Coastal Caves:
  Shore Crab: Karkinos Carapace (crit immunity, below 50% HP).
  Drowned Sailor: Grasp of the Deep (damage reduction, periodic).
  Siren Thrall: Siren's Call (massive dodge reduction, first round).
  Sea Hag: Curse of Thalassa (poison DoT, below 40% HP).
  Scylla Spawn: Tentacle Lash (damage + stacking bleed, every 2 rounds).
  Charybdis Maw: Whirlpool (burst damage + damage reduction, below 60% HP).
  Siren Queen: Song of Oblivion (damage + dodge debuff, first round) +
	Wrath of the Deep (burst + poison + self-regen, below 30% HP).

Enemies without abilities: Wild Dog, Bandit Lookout, Outlaw Peltast,
  Outlaw Peltast Band, Bandit Champion, War Hounds, Reef Serpent,
  Reef Serpent Pair, Coastal Chimera.

---

## Section 5d -- Hero Abilities (May 2026)

9 hero ability .tres files managed by HeroAbilityManager. Three
unlock paths: level-based (automatic), kleos purchase (manual),
dungeon reward (automatic on dungeon completion).

All unlocked abilities are automatically available in combat via
AbilityResolver. No loadout selection. Idle-friendly.

Level-based (automatic unlock):
  Focused Strike (level 2): 20 damage, replaces attack, 4 round cd.
  Second Wind (level 5): heal 25 HP below 30% HP, one-time per battle.
  Battle Hardened (level 8): +10% dodge for 3 rounds, first round.

Kleos-purchased (via Combat Arts panel):
  Shield of Bronze (2,000 kleos): 15 damage shield below 60% HP, 5 cd.
  Viper's Bite (4,000 kleos): 5 damage + 3/round poison 3 rounds,
    3 cd, does not replace attack, only used when target not poisoned.
  War Cry (6,000 kleos): +30% attack damage 3 rounds above 70% HP, 6 cd.

Dungeon rewards (automatic on completion):
  Lion's Resilience (Forest): 3 HP/round regen 4 rounds below 50% HP.
  Brigand's Instinct (Brigands): +15% crit chance 3 rounds every 4 rounds.
  Tide's Blessing (Coastal): heal 15 HP + remove poison below 40% HP.

HeroAbilityManager:
  Loads ability configs from HeroAbilityDatabase resource.
  Subscribes to HeroManager.LevelUp and DungeonManager.DungeonCompleted.
  CheckLevelUnlocks() and CheckDungeonUnlocks() run on signals.
  PurchaseAbility() checks kleos, level requirements, spends kleos.
  GetUnlockedAbilities() returns Godot Array for AbilityResolver.
  Save/load persists UnlockedAbilityIds list.

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

  Tier 3 -- Trials of the Shore (requires Coastal Caves, 7 upgrades):
	Poseidon's Tide, Sailor's Fortune, Potter's Legacy,
	Sculptor's Vision, Sea-Hardened Body, Tidal Instinct,
    Coastal Plunder. Costs range 6,000 to 20,000 kleos.

Upgrade UI (UpgradeRow):
Five visual states: Affordable, Unaffordable, Purchased, Tier Locked,
Individual Locked. Tier headers inserted between groups. Sorted by
tier then cost.

---

## Section 7 -- Random Encounters

Triggered by deed clicks. RandomEncounterManager tracks a click counter
and rolls against a threshold (configurable range, default 10-30).

Multi-pool system: multiple EncounterPool assets, each gated by a
dungeon completion requirement. Forest pool is always active (no gate).
The pool for the coming encounter is pre-selected at the start of each
cycle (pendingPool); a random enemy is then drawn from it by weighted
selection when the encounter fires. Pre-selecting the pool lets the
omen match the encounter (see Section 15).

Bosses and mini-bosses excluded from all encounter pools. They are
dungeon-exclusive.

Omen integration (June 2026; per-pool June 15): each cycle rolls an
omen trigger point 3-8 clicks before the threshold. When reached,
FlavorTextManager displays an amber warning drawn from the
pre-selected pool's own OmenLines, with a generic library fallback,
so the warning matches the coming fight. Cleared when the encounter
fires. See Section 15 for full omen details.

---

## Section 8 -- Save System

JSON file-based persistence using Godot FileAccess and user:// path.

SaveData structure:
  version (string)
  lastSaveTime (long, unix timestamp)
  KleosSaveData: currentKleos, totalKleosPerSecond
  ArtisanSaveData: owned counts dictionary, unlocked artisans list
  UpgradeSaveData: purchased upgrade IDs list
  DungeonSaveData: progress dictionary, completed dungeons list
  HeroSaveData: level, XP, stat points, stat upgrade counts
  HeroAbilitySaveData: unlocked ability IDs list (May 2026)

SaveManager methods:
  Save(SaveData) -- creates backup, serializes to JSON, writes file.
  Load() -- tries main file, falls back to backup, returns empty if both fail.
  HasSaveData() -- checks file existence.
  ResetAllSaveData() -- removes both main and backup files.
  Selective reset methods for each subsystem.

All managers implement GetSaveData() and LoadFromSaveData() methods.

---

## Section 9 -- Settings System

SettingsManager is a persistent Autoload. Uses ConfigFile storage
at user://settings.cfg (separate from game save).

Settings:
  Music volume (float, 0-1)
  SFX volume (float, 0-1)
  Fullscreen (bool)
  Resolution index (int)
  Scientific notation (bool, default false) -- May 2026

Scientific notation toggle controls NumberFormatter display mode.
When enabled, numbers above 999,999 display as scientific notation
(e.g. 1.23e6). Below 1 million, standard suffixes are always used.

---

## Section 10 -- Main Menu

Separate scene (res://Scenes/MainMenu/main_menu.tscn), set as the main
scene in Project Settings.

Title "KLEOS" displayed at center.
Prompt text below title with pulse animation.
Settings panel with audio sliders, fullscreen toggle, scientific
notation toggle, and delete save button.
Fade to black on game start, async scene change.

---

## Section 11 -- Game Scene Layout

Scene file: res://Scenes/Game/main_game.tscn
Controller script: MainGameController.cs

Layout structure:

  MainGame (Control, full screen)
	Background (ColorRect)
	RootLayout (VBoxContainer)
	  TopBar (HBoxContainer)
		HeroPortrait -- compact, top left
		KleosLabel
		ProductionLabel
	  MainPanel (HBoxContainer)
		LeftPanel (VBoxContainer, fixed width)
		  UpgradeButton
		  AbilityButton ("Combat Arts")
		  DungeonButton
		CenterPanel (VBoxContainer, expands)
		  DeedButtonContainer (Control, DeedButtonEvolution.cs)
			DeedGlow (ColorRect, behind button, hidden by default)
			DeedButton (Button)
		  DeedContextLabel
		  FlavorTextLabel
		RightPanel (PanelContainer, fixed width, dark style, shrink center)
		  RightColumn (VBoxContainer)
			ArtisanHeaderLabel ("Artisans")
			ArtisanScrollContainer
			  ArtisanList
			FooterRow (HBoxContainer)
			  BuyMultButton (pinned bottom-left, cycles buy multiplier)
	HeroPanel (overlay, starts hidden)
	DungeonPanel (overlay, starts hidden)
	UpgradePanel (overlay, starts hidden)
	AbilityPanel (overlay, starts hidden) -- May 2026
	  ScrollContainer > AbilityList
	BattlePanel (overlay, starts hidden)
	FadeOverlay (ColorRect)

Four overlay panels: Dungeon, Upgrade, and Ability are mutually
exclusive (opening one closes the others). HeroPanel toggles
independently on portrait click.

---

## Section 12 -- Combat Arts Panel (May 2026)

Displays all 9 hero abilities grouped into three sections:
  "Learned through experience" -- level-based abilities, sorted by level.
  "Purchased with kleos" -- buyable abilities, sorted by cost.
  "Earned through conquest" -- dungeon reward abilities, sorted by
	dungeon progression order.

AbilityRow displays per ability:
  Top row: ability name + type badges.
  Type badges: color-coded pills auto-determined from effects.
	Attack (red), Self buff (olive), Poison/Debuff (amber),
	Heal/Regen (teal), Cleanse (blue).
	Multi-type abilities show two badges (Viper's Bite: Attack + Poison).
  Description: auto-generated from effect data. Includes trigger
    condition, cooldown, one-time-use, replaces-attack info.
  Flavor text: from CastFlavorText field, dimmed italic style.
  Bottom row: unlock condition + status/purchase button.
    Level-based: "Unlocked at level N" or "Requires level N".
    Purchasable: cost in kleos with Purchase button.
    Dungeon reward: "Clear {Dungeon Name}".
  Three visual states: Unlocked (green left accent), Purchasable
    (dimmed with active button), Locked (dimmed with "Locked" badge).

AbilityRow refreshes on KleosChanged, LevelUp, AbilityUnlocked,
and DungeonCompleted signals.

---

## Section 13 -- NumberFormatter (May 2026)

Static utility class for compact number display. Not a Node, not
an Autoload. Called directly as NumberFormatter.FormatCompact(),
FormatFull(), or FormatCost().

Suffix table (short scale, up to decillion):
  K (10^3), M (10^6), B (10^9), T (10^12), Qa (10^15), Qi (10^18),
  Sx (10^21), Sp (10^24), Oc (10^27), No (10^30), Dc (10^33).

Beyond 999 decillion (10^36+): automatic scientific notation.

Display modes:
  Standard (default): 1.5K, 12.34M, 1.12B, 999Dc, then scientific.
  Scientific (user toggle): 1.23e6, 1e12 (only above 999,999).

Formatting rules:
  Below 1000: full integer with thousand separators.
  1.0 - 99.99 in a tier: two decimals, trailing zeros dropped.
  100 - 999 in a tier: no decimal.
  All values truncated (floor), never rounded.

FormatCost: full number with separators below 10K, compact above.
FormatFull: always full number with separators (for DevConsole).

Wired into: MainGameController (kleos, KpS), ArtisanRow (costs),
UpgradeRow (costs), BattlePanel (rewards), AbilityRow (costs),
DevConsole (full precision for debugging).

---

## Section 14 -- Deed Button Visual Evolution (June 2026)

The Deed button changes appearance as the player progresses through
artisan tiers. Button text always stays "Deeds" -- the hero is humble.
The visual presentation reflects the world's growing recognition.

Four tiers based on unique artisan types unlocked:

  Tier 0 -- Bronze (0-1 unique artisans):
	Terracotta background, dark brown border (1px), light text.
	The hero is unknown. Plain unfired clay.

  Tier 1 -- Silver (2-3 unique artisans):
	Cooler earth tone background, warm grey border (2px).
	Word is spreading. The clay has been fired.

  Tier 2 -- Gold (4-5 unique artisans):
	Warm bronze-gold background, golden border (2px).
	The hero's name is known.

  Tier 3 -- Divine (6 unique artisans, all unlocked):
    Deep navy background, bright gold border (3px), gold text.
    Persistent gold glow pulse behind the button.
    The gods have taken notice.

Tier-up animation (during gameplay only, not on load):
  Step 1: White flash burst from button center (0.3 seconds).
  Step 2: Color tween from old tier to new tier (0.5 seconds).
  Step 3: Divine tier starts persistent glow loop after transition.

Divine glow: gold ColorRect behind button, alpha pulses between
0.10 and 0.30 over 2.0 seconds (sine wave, infinite loop).

On game load, current tier is applied instantly with no animation.
Tier is purely derived from artisan state -- nothing to save.

Button evolution tier thresholds for owned artisan counts:
  Bronze:  10 Scribes, 5 Bards
  Silver:  25 Scribes, 15 Bards, 10 Potters
  Gold:    50 Scribes, 30 Bards, 20 Potters, 10 Sculptors
  Divine:  75 Scribes, 50 Bards, 35 Potters, 20 Sculptors,
           15 Playwrights, 5 Historians

Implementation: DeedButtonEvolution.cs on DeedButtonContainer node.
Uses StyleBoxFlat theme overrides (normal, hover, pressed, focus).
Subscribes to ArtisanManager.ArtisanPurchased signal.

---

## Section 15 -- Flavor Text and Omen System (June 2026; data-driven June 12; per-pool omens June 15)

FlavorTextManager is an Autoload singleton (position 12) that manages
a dedicated FlavorTextLabel in CenterPanel, below DeedContextLabel.

Two message types with different priority and behavior:

  Flavor text (low priority):
    Brief messages triggered by artisan purchases. Fades in over 0.3
    seconds, holds for 2.5 seconds, fades out over 0.5 seconds.
    Suppressed while an omen is active.

    Artisan purchase lines are data-driven. Each artisan carries its
    own PurchaseFlavorLines array on its ArtisanData .tres file (four
    lines each, e.g. Scribe: "A reed pen scratches parchment...").
    Priority chain on purchase:
	  1. the artisan's own PurchaseFlavorLines, if non-empty
	  2. GenericArtisanLines from flavor_text_library.tres
	This mirrors the EnemyData AttackLines precedent: owner lines
	first, library fallback second.

  Omen text (high priority):
	Pre-battle warnings from RandomEncounterManager. Appears in amber
	and stays visible until the encounter fires or is cleared.
	Replaces any current flavor text immediately.

	Omen trigger: RandomEncounterManager rolls an omenTriggerPoint
	(3-8 clicks before the encounter threshold) and pre-selects the
	coming encounter pool at the start of the cycle (pendingPool).
	When the click counter reaches the omen point, an omen for that
	pool is displayed, so the warning matches the danger. When the
	encounter fires, ClearOmen() fades the text out and the same
	pendingPool is used to choose the enemy.

	Omen lines are data-driven with a priority chain (per-pool omens,
	June 15):
	  1. the coming pool's own OmenLines on its EncounterPool .tres,
         if non-empty (Forest, Brigands, and Coastal each carry six
         themed lines)
      2. GenericOmenLines from flavor_text_library.tres (eight
         region-neutral lines, e.g. "A cold wind stirs the dust...")
    This is the same owner-first, library-fallback pattern used for
    artisan lines. A pool unlocked mid-cycle becomes eligible on the
    next cycle (cycles are 10-30 clicks).

Colors:
  Flavor text: muted earth tone (#B8A88A)
  Omen text: amber warning (#C4785A)

Label reference is set by MainGameController via SetLabel() in _Ready().
FlavorTextManager subscribes to ArtisanManager.ArtisanPurchased for
automatic artisan flavor text display.

DeedContextLabel remains as a separate system: it shows persistent
progression text based on total artisan count ("Training in solitude..."
through "Historians record your selfless acts..."). FlavorTextLabel
shows temporary messages below it.

FlavorTextLibrary (June 12, 2026): resource class at
res://Resources/FlavorText/FlavorTextLibrary.cs, mirroring the
BattleTextLibrary pattern. Pools: GenericOmenLines (now eight, after
the four Forest-specific lines moved to the Forest pool in the
June 15 omen rework), GenericArtisanLines (four neutral fallback
lines), and MilestoneLines (reserved, empty -- V0.7 awakening
sequence). Data asset:
res://Resources/FlavorText/flavor_text_library.tres, loaded by
FlavorTextManager in _Ready() via GD.Load (database loading pattern).

---

## Section 16 -- DevConsole

Developer tool for testing. Registered as Autoload position 13.
CanvasLayer with Layer 100 so it renders above everything.

Toggle: backtick (`) key.

Commands:
  kleos <amount>            -- adds kleos
  level <target>            -- sets hero to target level via XP grant
  stat <str/end/cun/fav> <n>-- upgrades stat N times
  clear <dungeonId>         -- completes a dungeon
  layer <dungeonId> <count> -- clears next N layers
  hp <amount>               -- sets hero HP to specific value
  pools                     -- shows active encounter pool count
  save / load / reset       -- save system
  status                    -- shows kleos, KpS, hero level, stats
  effects                   -- shows active status effects in battle
  buff <target> <type> <val> <dur> -- applies status effect in battle
  testability               -- adds test ability to current enemy
  abilities                 -- shows all hero abilities with unlock status
  unlock <abilityId>        -- force-unlocks a hero ability
  deed_tier <0-3>           -- forces deed button visual tier

Command history via up/down arrow keys.

---

## Integration Notes

KleosManager signals drive: KleosLabel, ArtisanRow affordability,
  UpgradeRow affordability, DungeonRow unlock checks, HeroManager XP.

ArtisanManager.ArtisanPurchased drives: ArtisanRow unlock checks,
  production recalculation, DeedButtonEvolution tier check,
  FlavorTextManager artisan flavor text.

UpgradeManager.UpgradePurchased drives: UpgradeRow refresh.

DungeonManager.DungeonCompleted drives: RandomEncounterManager pool
  refresh, DungeonRow state refresh, UpgradeRow tier gate refresh,
  HeroAbilityManager dungeon unlock checks.

HeroManager.LevelUp drives: HeroAbilityManager level unlock checks,
  AbilityRow refresh.

HeroAbilityManager.AbilityUnlocked drives: AbilityRow state refresh.

BattleSystem C# events drive: BattlePanel (BattleStarted,
  HeroAttackOccurred, EnemyAttackOccurred, RoundStarted, BattleEnded).

---

## Autoload Order

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

---

## Resource Directories

  res://Autoloads/                -- manager scripts
  res://Autoloads/Cobat/          -- combat system classes
    StatusEffectType.cs, StatusEffect.cs, StatusEffectManager.cs,
    AbilityResolver.cs, AbilityEnums.cs, AbilityEffect.cs,
    CombatAbility.cs
  res://Resources/Artisans/       -- 6 artisan .tres + database
  res://Resources/Dungeons/       -- 3 dungeon .tres + database
  res://Resources/Enemies/        -- enemy .tres by dungeon
  res://Resources/Upgrades/       -- 24 upgrade .tres + database
  res://Resources/EncounterPools/ -- 3 pool .tres + database
  res://Resources/BattleText/     -- battle_text_library.tres
  res://Resources/Abilities/
    Enemies/                      -- 20 enemy ability .tres
    Hero/                         -- 9 hero ability .tres + database
  res://Scenes/Game/              -- game scene, UI row scripts/scenes,
    DeedButtonEvolution.cs
  res://Scenes/MainMenu/          -- main menu scene

---

## What Is Not Yet Implemented

Prestige/meta-progression system (Echo/Arete mechanics).

---

END OF KMR GODOT

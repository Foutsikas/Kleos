# Kleos — System Overview

Kleos is an incremental / idle game built in Godot (C#), centered around earning fame through actions, scaling production, and progressing through structured challenges.

This document gives a high-level overview of how the game works without diving into implementation details.

---

## Core Loop

The gameplay loop is simple but layered:

1. Perform deeds (click)
2. Gain kleos (currency + XP)
3. Hire artisans (passive income)
4. Purchase upgrades (multipliers and bonuses)
5. Level up the hero (combat scaling)
6. Progress through dungeons (gated challenges)
7. Unlock new systems and repeat

Everything feeds back into increasing kleos generation.

---

## Core Systems

### Kleos (Currency + XP)

- Primary resource used for everything
- Gained from:
  - Manual clicks (deeds)
  - Passive artisan production
  - (Future) battles
- Also acts as experience for hero leveling

---

### Artisans (Passive Income)

Six artisan types generate kleos per second:

- Scribe → Bard → Potter → Sculptor → Playwright → Historian

Each artisan:
- Produces kleos passively
- Has exponentially increasing cost
- Unlocks the next artisan in the chain

This creates the main scaling backbone of the game.

---

### Upgrades (Modifiers)

Upgrades apply global or targeted modifiers:

- Flat bonuses (e.g. +click power)
- Multipliers (e.g. x production)
- Unlocks (future systems like battle speed)

Organized into tiers:
- Tier 1: Always available
- Tier 2+: Locked behind dungeon completion

---

### Hero System (Progression Layer)

The hero levels up using kleos as XP.

Stats:
- Strength → damage
- Endurance → max HP
- Cunning → dodge + crit chance
- Favor → crit scaling

Each level grants stat points, allowing player-driven builds.

---

### Dungeons (Progression Gates)

- Structured, layer-based challenges
- Each dungeon contains multiple enemy layers
- Must be completed sequentially

States:
- Locked
- Available
- In Progress
- Completed

Dungeons act as **progression gates** for upgrades and future content.

---

### Random Encounters

- Triggered by performing deeds (clicks)
- Based on a randomized threshold
- Pull enemies from active encounter pools

Pools unlock based on dungeon progression.

---

## UI Structure

The game is split into three main panels:

- **Left:** Navigation (Dungeons, Upgrades)
- **Center:** Core interaction (Deed button)
- **Right:** Passive systems (Artisans)

Additional overlays:
- Hero panel (stats and upgrades)
- Dungeon panel (progression)
- Upgrade panel (modifiers)

---

## Data-Driven Design

The game is heavily data-driven using Godot Resources (`.tres`):

- Artisans
- Enemies
- Dungeons
- Upgrades
- Encounter pools

New content can be added without code changes by dropping new resource files into the correct folders.

---

## Current Status

Implemented:
- Core loop (click + passive income)
- Artisan system
- Upgrade system
- Hero leveling and stats
- Dungeon progression structure
- UI systems for all of the above
- Save/load system

Not yet implemented:
- Battle system and combat UI
- Additional dungeon content
- Advanced systems (status effects, abilities, omens)

---

## Design Direction

- Semi-active idle (not fully passive)
- Clear progression gates (not endless inflation only)
- Player agency via stat allocation
- Strong thematic identity based on Greek mythology

Visual direction is currently placeholder while core systems are being built.

---

## Related Docs

- `docs/internal/KAR_Godot.md` → Full architecture reference (code structure)
- `docs/internal/KMR_Godot.md` → Full gameplay/system reference

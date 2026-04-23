# Kleos

Kleos is an incremental / idle game inspired by Ancient Greek mythology.  
You build fame through deeds, grow passive production via artisans, shape your hero through stat progression, and push through structured dungeon challenges.

The focus is on **clean progression, meaningful scaling, and system-driven design** — not gacha mechanics or idle-only automation.

---

## Current Status

Core systems are implemented and functional:

- Click-based kleos generation  
- Passive income (artisan system)  
- Upgrade system (modifiers & scaling)  
- Hero leveling and stat allocation  
- Dungeon progression structure  
- Full UI for all systems  
- Save / load system  

### Not implemented yet

- Battle system (in progress)  
- Additional dungeon content  
- Advanced combat systems (abilities, status effects)  

---

## Core Features

### Incremental Progression
- Active clicking + passive income  
- Exponential scaling through artisans and upgrades  

### Artisan System
- Six-tier production chain:
  `Scribe → Bard → Potter → Sculptor → Playwright → Historian`<br>
  *Bound to change*
- Unlock-based progression (not time-gated)  

### Upgrade System
- Flat and multiplier modifiers  
- Tiered unlocks tied to dungeon progression  
- No RNG — everything is deterministic  

### Hero Progression
- Levels scale from total kleos earned  

**Stats:**
- Strength → damage  
- Endurance → HP  
- Cunning → crit/dodge  
- Favor → crit scaling  

### Dungeon System
- Layer-based progression (no skipping)  
- Acts as progression gates for content  
- Designed for future combat integration  

### Random Encounters
- Triggered by player activity (clicks)  
- Pool-based system tied to progression  

---

## Tech Stack

- **Engine:** Godot 4 (.NET)  
- **Language:** C#  
- **Architecture:** Data-driven (Resource-based)  
- **Target:** Desktop (Windows / Linux)  

---

## Project Structure
```bash
Kleos/
├── Autoloads/ # Core singleton managers
├── Resources/ # Data-driven content (.tres)
├── Scenes/ # UI and game scenes
├── Sprites/ # All things sprites and art
├── docs/ # Documents related to the project
```

---

## Documentation
- `docs/overview.md`  
  High-level explanation of how the game works  

- `docs/internal/KAR_Godot.md`  
  Full architecture reference (code structure, signals, systems)  

- `docs/internal/KMR_Godot.md`  
  Full gameplay/system reference (values, mechanics, progression)  

---

## Design Goals

- Semi-active idle gameplay (not fully passive)  
- Clear progression gates (not infinite scaling only)  
- Deterministic systems (no RNG-driven progression)  
- Strong systemic foundation before content expansion  

---

## Notes

Visual style is currently placeholder.  
Focus is on building solid core systems before committing to final art direction.

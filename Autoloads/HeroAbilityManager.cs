using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class HeroAbilityManager : Node
{
    public static HeroAbilityManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Signals
    // -------------------------------------------------------------------------

    [Signal] public delegate void AbilityUnlockedEventHandler(string abilityId);

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    // Master list loaded from res://Resources/Abilities/Hero/
    private List<CombatAbility> allHeroAbilities = new();

    // IDs of abilities the player has unlocked (persisted)
    private Array<string> unlockedAbilityIds = new();

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

        LoadAbilityConfigs();

        // Subscribe to unlock triggers
        HeroManager.Instance.LevelUp += OnHeroLevelUp;
        DungeonManager.Instance.DungeonCompleted += OnDungeonCompleted;

        GD.Print($"[HeroAbilityManager] Ready. {allHeroAbilities.Count} hero abilities loaded.");
    }

    private void LoadAbilityConfigs()
    {
        var db = GD.Load<HeroAbilityDatabase>(
            "res://Resources/Abilities/Hero/hero_ability_database.tres");

        if (db == null)
        {
            GD.PrintErr("[HeroAbilityManager] Hero ability database failed to load.");
            return;
        }

        allHeroAbilities.Clear();
        for (int i = 0; i < db.Abilities.Count; i++)
        {
            var ability = db.Abilities[i];
            if (ability != null)
                allHeroAbilities.Add(ability);
        }
    }

    // -------------------------------------------------------------------------
    // Unlock: Level-Based
    // -------------------------------------------------------------------------

    private void OnHeroLevelUp(int newLevel)
    {
        CheckLevelUnlocks(newLevel);
    }

    public void CheckLevelUnlocks(int currentLevel)
    {
        for (int i = 0; i < allHeroAbilities.Count; i++)
        {
            CombatAbility ability = allHeroAbilities[i];

            if (ability.UnlockAtLevel <= 0) continue;
            if (ability.UnlockAtLevel > currentLevel) continue;
            if (IsUnlocked(ability.AbilityId)) continue;

            UnlockAbility(ability.AbilityId);
            GD.Print($"[HeroAbilityManager] Level unlock: {ability.AbilityName} " +
                     $"(level {currentLevel}).");
        }
    }

    // -------------------------------------------------------------------------
    // Unlock: Dungeon Reward
    // -------------------------------------------------------------------------

    private void OnDungeonCompleted(string dungeonId)
    {
        CheckDungeonUnlocks(dungeonId);
    }

    public void CheckDungeonUnlocks(string dungeonId)
    {
        for (int i = 0; i < allHeroAbilities.Count; i++)
        {
            CombatAbility ability = allHeroAbilities[i];

            if (string.IsNullOrEmpty(ability.UnlockFromDungeonId)) continue;
            if (ability.UnlockFromDungeonId != dungeonId) continue;
            if (IsUnlocked(ability.AbilityId)) continue;

            UnlockAbility(ability.AbilityId);
            GD.Print($"[HeroAbilityManager] Dungeon unlock: {ability.AbilityName} " +
                     $"(cleared {dungeonId}).");
        }
    }

    // -------------------------------------------------------------------------
    // Unlock: Kleos Purchase
    // -------------------------------------------------------------------------

    public bool CanPurchaseAbility(string abilityId)
    {
        CombatAbility ability = GetAbilityById(abilityId);
        if (ability == null) return false;
        if (IsUnlocked(abilityId)) return false;
        if (ability.KleosPurchaseCost <= 0f) return false;
        if (ability.UnlockAtLevel > 0
            && HeroManager.Instance.GetLevel() < ability.UnlockAtLevel)
            return false;
        if (KleosManager.Instance.CurrentKleos < ability.KleosPurchaseCost)
            return false;

        return true;
    }

    public bool PurchaseAbility(string abilityId)
    {
        if (!CanPurchaseAbility(abilityId)) return false;

        CombatAbility ability = GetAbilityById(abilityId);

        if (!KleosManager.Instance.SpendKleos(ability.KleosPurchaseCost))
            return false;

        UnlockAbility(abilityId);
        GD.Print($"[HeroAbilityManager] Purchased: {ability.AbilityName} " +
                 $"for {ability.KleosPurchaseCost} kleos.");
        return true;
    }

    // -------------------------------------------------------------------------
    // Core Unlock
    // -------------------------------------------------------------------------

    private void UnlockAbility(string abilityId)
    {
        if (unlockedAbilityIds.Contains(abilityId)) return;

        unlockedAbilityIds.Add(abilityId);
        EmitSignal(SignalName.AbilityUnlocked, abilityId);
    }

    // -------------------------------------------------------------------------
    // Queries
    // -------------------------------------------------------------------------

    public bool IsUnlocked(string abilityId)
    {
        return unlockedAbilityIds.Contains(abilityId);
    }

    public CombatAbility GetAbilityById(string abilityId)
    {
        for (int i = 0; i < allHeroAbilities.Count; i++)
        {
            if (allHeroAbilities[i].AbilityId == abilityId)
                return allHeroAbilities[i];
        }
        return null;
    }

    // Returns all unlocked abilities as a Godot Array for AbilityResolver
    public Array<CombatAbility> GetUnlockedAbilities()
    {
        var result = new Array<CombatAbility>();
        for (int i = 0; i < allHeroAbilities.Count; i++)
        {
            if (unlockedAbilityIds.Contains(allHeroAbilities[i].AbilityId))
                result.Add(allHeroAbilities[i]);
        }
        return result;
    }

    // Returns all abilities with their unlock status for UI display
    public List<CombatAbility> GetAllAbilities()
    {
        return allHeroAbilities;
    }

    public int GetUnlockedCount()
    {
        return unlockedAbilityIds.Count;
    }

    public int GetTotalCount()
    {
        return allHeroAbilities.Count;
    }

    // -------------------------------------------------------------------------
    // Save / Load
    // -------------------------------------------------------------------------

    public HeroAbilitySaveData GetSaveData()
    {
        return new HeroAbilitySaveData
        {
            UnlockedAbilityIds = new Array<string>(unlockedAbilityIds)
        };
    }

    public void LoadFromSaveData(HeroAbilitySaveData data)
    {
        unlockedAbilityIds = data.UnlockedAbilityIds ?? new Array<string>();

        // Re-check level and dungeon unlocks in case the player
        // earned them before the save system existed
        CheckLevelUnlocks(HeroManager.Instance.GetLevel());

        for (int i = 0; i < allHeroAbilities.Count; i++)
        {
            string dungeonId = allHeroAbilities[i].UnlockFromDungeonId;
            if (!string.IsNullOrEmpty(dungeonId)
                && DungeonManager.Instance.IsDungeonCompleted(dungeonId))
            {
                CheckDungeonUnlocks(dungeonId);
            }
        }

        GD.Print($"[HeroAbilityManager] Loaded. {unlockedAbilityIds.Count} abilities unlocked.");
    }
}

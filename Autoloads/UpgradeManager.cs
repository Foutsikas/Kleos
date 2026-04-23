using Godot;
using Godot.Collections;

public partial class UpgradeManager : Node
{
    public static UpgradeManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void UpgradePurchasedEventHandler(string upgradeId);
    [Signal] public delegate void TiersRefreshedEventHandler();

    // --- Config ---
    public Array UpgradeConfigs { get; private set; } = new();

    private void LoadConfigs()
    {
        UpgradeConfigs.Clear();
        string[] paths = new[]
        {
        // Tier 1
        "res://Resources/Upgrades/1_01_scribes_quill.tres",
        "res://Resources/Upgrades/1_02_bronze_training.tres",
        "res://Resources/Upgrades/1_03_inspiring_presence.tres",
        "res://Resources/Upgrades/1_04_bards_inspiration.tres",
        "res://Resources/Upgrades/1_05_blessed_growth.tres",
        "res://Resources/Upgrades/1_06_spartans_endurance.tres",
        "res://Resources/Upgrades/1_07_potters_craft.tres",
        "res://Resources/Upgrades/1_08_warriors_discipline.tres",
        "res://Resources/Upgrades/1_09_olympian_strike.tres",
        "res://Resources/Upgrades/1_10_echoing_deed.tres",
        // Tier 2
        "res://Resources/Upgrades/2_01_stolen_blade.tres",
        "res://Resources/Upgrades/2_02_spoils_of_the_road.tres",
        "res://Resources/Upgrades/2_03_scribes_discipline.tres",
        "res://Resources/Upgrades/2_04_bards_war_song.tres",
        "res://Resources/Upgrades/2_05_road_hardened.tres",
        "res://Resources/Upgrades/2_06_brigands_cunning.tres",
        "res://Resources/Upgrades/2_07_victors_instinct.tres",
        // Tier 3
        "res://Resources/Upgrades/3_01_poseidons_tide.tres",
        "res://Resources/Upgrades/3_02_sailors_fortune.tres",
        "res://Resources/Upgrades/3_03_potters_legacy.tres",
        "res://Resources/Upgrades/3_04_sculptors_vision.tres",
        "res://Resources/Upgrades/3_05_sea_hardened_body.tres",
        "res://Resources/Upgrades/3_06_tidal_instinct.tres",
        "res://Resources/Upgrades/3_07_coastal_plunder.tres",
    };
        foreach (var path in paths)
            UpgradeConfigs.Add(GD.Load<UpgradeConfig>(path));
    }

    // --- State ---
    private Array<string> purchasedUpgradeIds = new();
    private Dictionary<string, UpgradeConfig> upgradeConfigLookup = new();

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        LoadConfigs();
        BuildLookup();
        GD.Print("[UpgradeManager] Ready.");
    }

    private void BuildLookup()
    {
        upgradeConfigLookup.Clear();
        for (int i = 0; i < UpgradeConfigs.Count; i++)
        {
            var config = UpgradeConfigs[i].As<UpgradeConfig>();
            if (config == null) continue;
            if (string.IsNullOrEmpty(config.UpgradeId)) continue;
            upgradeConfigLookup[config.UpgradeId] = config;
        }
        GD.Print($"[UpgradeManager] Loaded {upgradeConfigLookup.Count} upgrade configs.");
    }

    // --- Modifier Queries ---

    public float GetFlat(ModifierType type)
    {
        float total = 0f;
        foreach (var id in purchasedUpgradeIds)
        {
            if (!upgradeConfigLookup.TryGetValue(id, out var config)) continue;
            for (int i = 0; i < config.Effects.Count; i++)
            {
                var effect = config.GetEffect(i);
                if (effect == null) continue;
                if (effect.Type == type && effect.Mode == ModifierMode.Flat)
                    total += effect.Value;
            }
        }
        return total;
    }

    public float GetMultiplier(ModifierType type)
    {
        float total = 1f;
        foreach (var id in purchasedUpgradeIds)
        {
            if (!upgradeConfigLookup.TryGetValue(id, out var config)) continue;
            for (int i = 0; i < config.Effects.Count; i++)
            {
                var effect = config.GetEffect(i);
                if (effect == null) continue;
                if (effect.Type == type && effect.Mode == ModifierMode.Multiplier)
                    total *= effect.Value;
            }
        }
        return total;
    }

    // --- Purchase ---

    public bool CanPurchase(string upgradeId)
    {
        if (!upgradeConfigLookup.TryGetValue(upgradeId, out var config)) return false;
        if (IsUpgradePurchased(upgradeId)) return false;
        if (!IsTierUnlocked(config)) return false;
        if (!IsIndividualLockMet(config)) return false;
        if (KleosManager.Instance.CurrentKleos < config.Cost) return false;
        return true;
    }

    public bool PurchaseUpgrade(string upgradeId)
    {
        if (!CanPurchase(upgradeId)) return false;

        if (!upgradeConfigLookup.TryGetValue(upgradeId, out var config)) return false;

        if (!KleosManager.Instance.SpendKleos(config.Cost)) return false;

        purchasedUpgradeIds.Add(upgradeId);
        EmitSignal(SignalName.UpgradePurchased, upgradeId);
        GD.Print($"[UpgradeManager] Purchased: {config.UpgradeName}");
        return true;
    }

    public bool IsUpgradePurchased(string upgradeId)
    {
        return purchasedUpgradeIds.Contains(upgradeId);
    }

    // --- Lock Checks ---

    private bool IsTierUnlocked(UpgradeConfig config)
    {
        if (config.RequiredDungeon == null) return true;
        return DungeonManager.Instance.IsDungeonCompleted(config.RequiredDungeon.DungeonId);
    }

    private bool IsIndividualLockMet(UpgradeConfig config)
    {
        // Hero level requirement
        if (config.RequiredHeroLevel > 0)
        {
            if (HeroManager.Instance.GetLevel() < config.RequiredHeroLevel)
                return false;
        }

        // Prerequisite upgrade
        if (!string.IsNullOrEmpty(config.RequiredUpgradeId))
        {
            if (!IsUpgradePurchased(config.RequiredUpgradeId))
                return false;
        }

        // Artisan count requirement
        if (!string.IsNullOrEmpty(config.RequiredArtisanId) && config.RequiredArtisanCount > 0)
        {
            if (ArtisanManager.Instance.GetOwnedCount(config.RequiredArtisanId) < config.RequiredArtisanCount)
                return false;
        }

        return true;
    }

    // --- Save / Load ---

    public UpgradeSaveData GetSaveData()
    {
        return new UpgradeSaveData
        {
            PurchasedUpgradeIds = new Array<string>(purchasedUpgradeIds)
        };
    }

    public void LoadFromSaveData(UpgradeSaveData data)
    {
        purchasedUpgradeIds = data.PurchasedUpgradeIds ?? new Array<string>();
        EmitSignal(SignalName.TiersRefreshed);
        GD.Print($"[UpgradeManager] Loaded {purchasedUpgradeIds.Count} purchased upgrades.");
    }
}
using Godot;
using Godot.Collections;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }

    // --- Paths ---
    private const string SaveFilePath = "user://game_save.json";
    private const string BackupFilePath = "user://game_save.backup.json";
    private const string CurrentVersion = "1.0";

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[SaveManager] Ready.");
    }

    // --- Save ---

    public void Save(SaveData data)
    {
        data.LastSaveTime = (long)Time.GetUnixTimeFromSystem();

        string json = BuildJson(data);

        // Create backup of existing save before overwriting
        if (FileAccess.FileExists(SaveFilePath))
        {
            DirAccess.CopyAbsolute(
                ProjectSettings.GlobalizePath(SaveFilePath),
                ProjectSettings.GlobalizePath(BackupFilePath)
            );
        }

        using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("[SaveManager] Failed to open save file for writing.");
            return;
        }

        file.StoreString(json);
        GD.Print("[SaveManager] Game saved.");
    }

    // --- Load ---

    public SaveData Load()
    {
        if (FileAccess.FileExists(SaveFilePath))
        {
            SaveData data = TryLoadFrom(SaveFilePath);
            if (data != null)
                return data;

            GD.PrintErr("[SaveManager] Main save corrupted. Trying backup.");
        }

        if (FileAccess.FileExists(BackupFilePath))
        {
            SaveData data = TryLoadFrom(BackupFilePath);
            if (data != null)
            {
                GD.Print("[SaveManager] Loaded from backup.");
                return data;
            }

            GD.PrintErr("[SaveManager] Backup also corrupted. Starting fresh.");
        }

        GD.Print("[SaveManager] No save found. Returning empty SaveData.");
        return new SaveData();
    }

    private SaveData TryLoadFrom(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return null;

        string json = file.GetAsText();
        return ParseJson(json);
    }

    // --- Reset Methods ---

    public void ResetAllSaveData()
    {
        if (FileAccess.FileExists(SaveFilePath))
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SaveFilePath));
        if (FileAccess.FileExists(BackupFilePath))
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(BackupFilePath));

        GD.Print("[SaveManager] All save data cleared.");
    }

    public void ResetKleosOnly()
    {
        SaveData data = Load();
        data.Kleos = new KleosSaveData();
        Save(data);
        GD.Print("[SaveManager] Kleos reset.");
    }

    public void ResetArtisanData()
    {
        SaveData data = Load();
        data.Artisans = new ArtisanSaveData();
        Save(data);
        GD.Print("[SaveManager] Artisan data reset.");
    }

    public void ResetUpgradeData()
    {
        SaveData data = Load();
        data.Upgrades = new UpgradeSaveData();
        Save(data);
        GD.Print("[SaveManager] Upgrade data reset.");
    }

    public void ResetDungeonData()
    {
        SaveData data = Load();
        data.Dungeons = new DungeonSaveData();
        Save(data);
        GD.Print("[SaveManager] Dungeon data reset.");
    }

    public void ResetHeroData()
    {
        SaveData data = Load();
        data.Hero = new HeroSaveData();
        Save(data);
        GD.Print("[SaveManager] Hero data reset.");
    }

    public bool HasSaveData() => FileAccess.FileExists(SaveFilePath);

    // --- JSON Serialization ---
    // Godot's built-in JSON class is used directly.
    // Each save section is stored as a nested Dictionary.

    private string BuildJson(SaveData data)
    {
        var root = new Dictionary
        {
            ["version"] = data.Version,
            ["lastSaveTime"] = data.LastSaveTime,

            ["kleos"] = new Dictionary
            {
                ["currentKleos"] = data.Kleos.CurrentKleos,
                ["totalKleosPerSecond"] = data.Kleos.TotalKleosPerSecond
            },

            ["artisans"] = new Dictionary
            {
                ["ownedCounts"] = OwnedCountsToVariant(data.Artisans.OwnedCounts),
                ["unlockedArtisans"] = data.Artisans.UnlockedArtisans
            },

            ["upgrades"] = new Dictionary
            {
                ["purchasedUpgradeIds"] = data.Upgrades.PurchasedUpgradeIds
            },

            ["dungeons"] = new Dictionary
            {
                ["dungeonProgress"] = DungeonProgressToVariant(data.Dungeons.DungeonProgress),
                ["completedDungeons"] = data.Dungeons.CompletedDungeons
            },

            ["hero"] = new Dictionary
            {
                ["level"] = data.Hero.Level,
                ["currentXP"] = data.Hero.CurrentXP,
                ["availableStatPoints"] = data.Hero.AvailableStatPoints,
                ["strengthUpgrades"] = data.Hero.StrengthUpgrades,
                ["enduranceUpgrades"] = data.Hero.EnduranceUpgrades,
                ["cunningUpgrades"] = data.Hero.CunningUpgrades,
                ["favorUpgrades"] = data.Hero.FavorUpgrades
            }
        };

        return Json.Stringify(root, "\t");
    }

    private SaveData ParseJson(string json)
    {
        var parsed = Json.ParseString(json);
        if (parsed.VariantType != Variant.Type.Dictionary)
            return null;

        var root = parsed.As<Dictionary>();
        var data = new SaveData();

        // -- Version --
        if (root.TryGetValue("version", out var version))
            data.Version = version.As<string>();

        if (root.TryGetValue("lastSaveTime", out var lastSave))
            data.LastSaveTime = lastSave.As<long>();

        // -- Kleos --
        if (root.TryGetValue("kleos", out var kleosVar))
        {
            var k = kleosVar.As<Dictionary>();
            if (k.TryGetValue("currentKleos", out var ck))
                data.Kleos.CurrentKleos = ck.As<float>();
            if (k.TryGetValue("totalKleosPerSecond", out var tkps))
                data.Kleos.TotalKleosPerSecond = tkps.As<float>();
        }

        // -- Artisans --
        if (root.TryGetValue("artisans", out var artisansVar))
        {
            var a = artisansVar.As<Dictionary>();
            if (a.TryGetValue("ownedCounts", out var oc))
                data.Artisans.OwnedCounts = VariantToOwnedCounts(oc.As<Dictionary>());
            if (a.TryGetValue("unlockedArtisans", out var ua))
                data.Artisans.UnlockedArtisans = ua.As<Array<string>>();
        }

        // -- Upgrades --
        if (root.TryGetValue("upgrades", out var upgradesVar))
        {
            var u = upgradesVar.As<Dictionary>();
            if (u.TryGetValue("purchasedUpgradeIds", out var pu))
                data.Upgrades.PurchasedUpgradeIds = pu.As<Array<string>>();
        }

        // -- Dungeons --
        if (root.TryGetValue("dungeons", out var dungeonsVar))
        {
            var d = dungeonsVar.As<Dictionary>();
            if (d.TryGetValue("dungeonProgress", out var dp))
                data.Dungeons.DungeonProgress = VariantToDungeonProgress(dp.As<Dictionary>());
            if (d.TryGetValue("completedDungeons", out var cd))
                data.Dungeons.CompletedDungeons = cd.As<Array<string>>();
        }

        // -- Hero --
        if (root.TryGetValue("hero", out var heroVar))
        {
            var h = heroVar.As<Dictionary>();
            if (h.TryGetValue("level", out var lv))
                data.Hero.Level = lv.As<int>();
            if (h.TryGetValue("currentXP", out var xp))
                data.Hero.CurrentXP = xp.As<float>();
            if (h.TryGetValue("availableStatPoints", out var sp))
                data.Hero.AvailableStatPoints = sp.As<int>();
            if (h.TryGetValue("strengthUpgrades", out var str))
                data.Hero.StrengthUpgrades = str.As<int>();
            if (h.TryGetValue("enduranceUpgrades", out var end))
                data.Hero.EnduranceUpgrades = end.As<int>();
            if (h.TryGetValue("cunningUpgrades", out var cun))
                data.Hero.CunningUpgrades = cun.As<int>();
            if (h.TryGetValue("favorUpgrades", out var fav))
                data.Hero.FavorUpgrades = fav.As<int>();
        }

        return data;
    }

    // --- Dictionary Helpers ---
    // Godot JSON stores all keys as strings so Dictionary<string, int>
    // needs explicit conversion on both save and load.

    private Dictionary OwnedCountsToVariant(Dictionary<string, int> counts)
    {
        var result = new Dictionary();
        foreach (var kvp in counts)
            result[kvp.Key] = kvp.Value;
        return result;
    }

    private Dictionary<string, int> VariantToOwnedCounts(Dictionary raw)
    {
        var result = new Dictionary<string, int>();
        foreach (var key in raw.Keys)
            result[key.As<string>()] = raw[key].As<int>();
        return result;
    }

    private Dictionary DungeonProgressToVariant(Dictionary<string, int> progress)
    {
        var result = new Dictionary();
        foreach (var kvp in progress)
            result[kvp.Key] = kvp.Value;
        return result;
    }

    private Dictionary<string, int> VariantToDungeonProgress(Dictionary raw)
    {
        var result = new Dictionary<string, int>();
        foreach (var key in raw.Keys)
            result[key.As<string>()] = raw[key].As<int>();
        return result;
    }
}
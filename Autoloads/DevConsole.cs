using Godot;
using System;
using System.Collections.Generic;

public partial class DevConsole : CanvasLayer
{
    private PanelContainer panel;
    private LineEdit inputField;
    private Label outputLabel;
    private bool isVisible = false;

    // Command history
    private List<string> history = new List<string>();
    private int historyIndex = -1;

    public override void _Ready()
    {
        // Build the UI in code so no scene setup is needed
        BuildUI();
        panel.Visible = false;
        GD.Print("[DevConsole] Ready. Press ` to toggle.");
    }

    public override void _UnhandledKeyInput(InputEvent ev)
    {
        if (ev is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // Backtick or tilde to toggle
            if (keyEvent.Keycode == Key.Quoteleft)
            {
                ToggleConsole();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Input(InputEvent ev)
    {
        if (!isVisible) return;

        if (ev is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Up && history.Count > 0)
            {
                historyIndex = Mathf.Max(0, historyIndex - 1);
                inputField.Text = history[historyIndex];
                inputField.CaretColumn = inputField.Text.Length;
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Down && history.Count > 0)
            {
                historyIndex = Mathf.Min(history.Count, historyIndex + 1);
                if (historyIndex >= history.Count)
                    inputField.Text = "";
                else
                    inputField.Text = history[historyIndex];
                inputField.CaretColumn = inputField.Text.Length;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    // -------------------------------------------------------------------------
    // UI Construction
    // -------------------------------------------------------------------------

    private void BuildUI()
    {
        Layer = 100;

        panel = new PanelContainer();
        panel.AnchorLeft = 0;
        panel.AnchorTop = 0;
        panel.AnchorRight = 1;
        panel.AnchorBottom = 0;
        panel.OffsetBottom = 160;

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        styleBox.ContentMarginLeft = 10;
        styleBox.ContentMarginRight = 10;
        styleBox.ContentMarginTop = 8;
        styleBox.ContentMarginBottom = 8;
        panel.AddThemeStyleboxOverride("panel", styleBox);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);

        var titleLabel = new Label();
        titleLabel.Text = "DEV CONSOLE";
        titleLabel.AddThemeFontSizeOverride("font_size", 12);
        titleLabel.Modulate = new Color(0.6f, 0.6f, 0.4f);

        outputLabel = new Label();
        outputLabel.Text = "Type 'help' for commands.";
        outputLabel.AddThemeFontSizeOverride("font_size", 13);
        outputLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        outputLabel.CustomMinimumSize = new Vector2(0, 60);

        inputField = new LineEdit();
        inputField.PlaceholderText = "Enter command...";
        inputField.AddThemeFontSizeOverride("font_size", 14);
        inputField.TextSubmitted += OnCommandSubmitted;

        vbox.AddChild(titleLabel);
        vbox.AddChild(outputLabel);
        vbox.AddChild(inputField);
        panel.AddChild(vbox);
        AddChild(panel);
    }

    // -------------------------------------------------------------------------
    // Toggle
    // -------------------------------------------------------------------------

    private void ToggleConsole()
    {
        isVisible = !isVisible;
        panel.Visible = isVisible;

        if (isVisible)
        {
            inputField.GrabFocus();
            inputField.Clear();
        }
    }

    // -------------------------------------------------------------------------
    // Command Processing
    // -------------------------------------------------------------------------

    private void OnCommandSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        history.Add(text);
        historyIndex = history.Count;
        inputField.Clear();

        string result = ExecuteCommand(text.Trim().ToLower());
        outputLabel.Text = result;

        GD.Print($"[DevConsole] {text} -> {result}");
    }

    private string ExecuteCommand(string input)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "Empty command.";

        string cmd = parts[0];

        switch (cmd)
        {
            case "help":
                return "Commands:\n" +
                       "  kleos <amount> -- Add kleos\n" +
                       "  level <level> -- Set hero level\n" +
                       "  stat <str/end/cun/fav> <amount> -- Add stat points\n" +
                       "  clear <dungeonId> -- Force complete dungeon\n" +
                       "  layer <dungeonId> <count> -- Clear N layers\n" +
                       "  hp <amount> -- Set hero HP\n" +
                       "  pools -- Show active encounter pools\n" +
                       "  save / load / reset -- Save system\n" +
                       "  status -- Show game state";

            case "kleos":
                return CmdKleos(parts);

            case "level":
                return CmdLevel(parts);

            case "stat":
                return CmdStat(parts);

            case "clear":
                return CmdClearDungeon(parts);

            case "layer":
                return CmdClearLayers(parts);

            case "hp":
                return CmdHP(parts);

            case "pools":
                return CmdPools();

            case "save":
                return CmdSave();

            case "load":
                return CmdLoad();

            case "reset":
                return CmdReset();

            case "status":
                return CmdStatus();

            default:
                return $"Unknown command: {cmd}. Type 'help'.";
        }
    }

    // -------------------------------------------------------------------------
    // Command Implementations
    // -------------------------------------------------------------------------

    private string CmdKleos(string[] parts)
    {
        if (parts.Length < 2) return "Usage: kleos <amount>";
        if (!float.TryParse(parts[1], out float amount)) return "Invalid number.";

        KleosManager.Instance.AddKleos(amount);
        return $"Added {amount:N0} kleos. Total: {KleosManager.Instance.CurrentKleos:N0}";
    }

    private string CmdLevel(string[] parts)
    {
        if (parts.Length < 2) return "Usage: level <target>";
        if (!int.TryParse(parts[1], out int target)) return "Invalid number.";

        int current = HeroManager.Instance.GetLevel();
        if (target <= current) return $"Already level {current}.";

        // Grant enough XP to reach target level
        for (int i = current; i < target; i++)
        {
            float xpNeeded = HeroManager.Instance.GetXPToNextLevel();
            float currentXP = HeroManager.Instance.GetCurrentXP();
            float deficit = xpNeeded - currentXP + 1;
            if (deficit > 0)
                HeroManager.Instance.AddExperience(deficit);
        }

        return $"Hero set to level {HeroManager.Instance.GetLevel()}. " +
               $"Stat points: {HeroManager.Instance.GetAvailableStatPoints()}";
    }

    private string CmdStat(string[] parts)
    {
        if (parts.Length < 3) return "Usage: stat <str/end/cun/fav> <amount>";
        if (!int.TryParse(parts[2], out int amount)) return "Invalid number.";

        HeroStat stat;
        switch (parts[1])
        {
            case "str": stat = HeroStat.Strength; break;
            case "end": stat = HeroStat.Endurance; break;
            case "cun": stat = HeroStat.Cunning; break;
            case "fav": stat = HeroStat.Favor; break;
            default: return "Unknown stat. Use: str, end, cun, fav";
        }

        for (int i = 0; i < amount; i++)
            HeroManager.Instance.UpgradeStat(stat);

        return $"Upgraded {parts[1]} by {amount}. " +
               $"HP: {HeroManager.Instance.GetMaxHP():F0}, " +
               $"DMG: {HeroManager.Instance.GetDamage():F1}";
    }

    private string CmdClearDungeon(string[] parts)
    {
        if (parts.Length < 2)
            return "Usage: clear <dungeonId>";

        string dungeonId = parts[1];

        DungeonData dungeon = DungeonManager.Instance.GetDungeonById(dungeonId);
        if (dungeon == null)
            return $"Unknown dungeon: {dungeonId}";

        DungeonManager.Instance.ForceCompleteDungeon(dungeonId);

        return $"Force completed {dungeon.DungeonName}.";
    }

    private string CmdClearLayers(string[] parts)
    {
        if (parts.Length < 3) return "Usage: layer <dungeonId> <count>";
        string dungeonId = parts[1];
        if (!int.TryParse(parts[2], out int count)) return "Invalid number.";

        DungeonData dungeon = DungeonManager.Instance.GetDungeonById(dungeonId);
        if (dungeon == null) return $"Unknown dungeon: {dungeonId}";

        int start = DungeonManager.Instance.GetNextLayer(dungeonId);
        int totalLayers = dungeon.Layers.Count;
        int cleared = 0;

        for (int i = start; i < start + count && i < totalLayers; i++)
        {
            DungeonManager.Instance.OnLayerCleared(dungeonId, i);
            cleared++;
        }

        return $"Cleared {cleared} layers of {dungeon.DungeonName}. " +
               $"Progress: {DungeonManager.Instance.GetNextLayer(dungeonId)}/{totalLayers}";
    }

    private string CmdHP(string[] parts)
    {
        if (parts.Length < 2) return "Usage: hp <amount>";
        if (!float.TryParse(parts[1], out float amount)) return "Invalid number.";

        HeroManager.Instance.RestoreFullHP();
        if (amount < HeroManager.Instance.GetMaxHP())
        {
            float damage = HeroManager.Instance.GetMaxHP() - amount;
            HeroManager.Instance.TakeDamage(damage);
        }

        return $"Hero HP set to {HeroManager.Instance.GetCurrentHP():F0}/{HeroManager.Instance.GetMaxHP():F0}";
    }

    private string CmdPools()
    {
        // RandomEncounterManager does not expose pool list publicly,
        // so we just report the count from the ready log.
        return "Check Output panel for [RandomEncounterManager] Active pools count.\n" +
               "Complete a dungeon to activate its encounter pool.";
    }

    private string CmdSave()
    {
        // Trigger a save through the existing save flow
        var saveData = new SaveData();
        saveData.Kleos = KleosManager.Instance.GetSaveData();
        saveData.Artisans = ArtisanManager.Instance.GetSaveData();
        saveData.Upgrades = UpgradeManager.Instance.GetSaveData();
        saveData.Dungeons = DungeonManager.Instance.GetSaveData();
        saveData.Hero = HeroManager.Instance.GetSaveData();
        SaveManager.Instance.Save(saveData);
        return "Game saved.";
    }

    private string CmdLoad()
    {
        SaveData data = SaveManager.Instance.Load();
        KleosManager.Instance.LoadFromSaveData(data.Kleos);
        ArtisanManager.Instance.LoadFromSaveData(data.Artisans);
        UpgradeManager.Instance.LoadFromSaveData(data.Upgrades);
        DungeonManager.Instance.LoadFromSaveData(data.Dungeons);
        HeroManager.Instance.LoadFromSaveData(data.Hero);
        return "Game loaded.";
    }

    private string CmdReset()
    {
        SaveManager.Instance.ResetAllSaveData();
        return "Save data deleted. Restart the game to apply.";
    }

    private string CmdStatus()
    {
        return $"Kleos: {KleosManager.Instance.CurrentKleos:N0} | " +
               $"KpS: {KleosManager.Instance.TotalKleosPerSecond:F1}\n" +
               $"Hero Lv.{HeroManager.Instance.GetLevel()} | " +
               $"HP: {HeroManager.Instance.GetCurrentHP():F0}/{HeroManager.Instance.GetMaxHP():F0} | " +
               $"DMG: {HeroManager.Instance.GetDamage():F1} | " +
               $"Dodge: {HeroManager.Instance.GetDodgeChance() * 100:F1}% | " +
               $"Crit: {HeroManager.Instance.GetCritChance() * 100:F1}%";
    }
}
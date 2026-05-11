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
                       "  status -- Show game state\n" +
                       "  effects -- Show active status effects\n" +
                       "  buff <hero|enemy> <type> <value> <duration>\n" +
                       "  testability -- Add test ability to enemy\n" +
                       "  abilities -- Show hero abilities\n" +
                       "  unlock <abilityId> -- Force unlock ability\n" +
                       "  deed_tier <0-3>    -- Force deed button visual tier"
                       ;

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

            case "buff":
                HandleBuffCommand(parts);
                return "";

            case "testability":
                HandleTestAbilityCommand(parts);
                return "";

            case "effects":
                HandleEffectsCommand();
                return "";

            case "abilities":
                HandleAbilitiesCommand();
                return "";

            case "unlock":
                HandleUnlockCommand(parts);
                return "";

            case "deed_tier":
                return CmdDeedTier(parts);

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
        return $"Added {NumberFormatter.FormatFull(amount)} kleos. Total: {NumberFormatter.FormatFull(KleosManager.Instance.CurrentKleos)}";

    }

    private string CmdLevel(string[] parts)
    {
        if (parts.Length < 2) return "Usage: level <target>";
        if (!int.TryParse(parts[1], out int target)) return "Invalid number.";

        int current = HeroManager.Instance.GetLevel();
        if (target <= current) return $"Already level {current}.";

        // Grant massive XP to reach target
        // XP threshold grows exponentially, so overshoot is fine
        float totalXPNeeded = 0;
        for (int i = current; i < target; i++)
            totalXPNeeded += 1000f * Mathf.Pow(1.5f, i - 1);

        HeroManager.Instance.AddExperience(totalXPNeeded + 1);

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

        int applied = 0;
        for (int i = 0; i < amount; i++)
        {
            if (HeroManager.Instance.UpgradeStat(stat))
                applied++;
            else
                break;
        }

        return $"Upgraded {parts[1]} by {applied} (requested {amount}). " +
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
        saveData.HeroAbilities = HeroAbilityManager.Instance.GetSaveData();
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
        HeroAbilityManager.Instance.LoadFromSaveData(data.HeroAbilities);
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
        return $"Kleos: {NumberFormatter.FormatFull(KleosManager.Instance.CurrentKleos)} | " +
               $"KpS: {NumberFormatter.FormatCompact(KleosManager.Instance.TotalKleosPerSecond)}\n" +
               $"Hero Lv.{HeroManager.Instance.GetLevel()} | " +
               $"HP: {HeroManager.Instance.GetCurrentHP():F0}/{HeroManager.Instance.GetMaxHP():F0} | " +
               $"DMG: {HeroManager.Instance.GetDamage():F1} | " +
               $"Dodge: {HeroManager.Instance.GetDodgeChance() * 100:F1}% | " +
               $"Crit: {HeroManager.Instance.GetCritChance() * 100:F1}%";
    }

    private void HandleBuffCommand(string[] parts)
    {
        if (parts.Length < 5)
        {
            outputLabel.Text = "Usage: buff <hero|enemy> <type> <value> <duration>\n" +
                "Types: attackup, attackdown, dodgeup, dodgedown, critup, critimmune, stun";
            return;
        }

        if (!BattleSystem.Instance.IsBattleInProgress())
        {
            outputLabel.Text = "No active battle.";
            return;
        }

        string target = parts[1];
        string typeName = parts[2];

        if (!float.TryParse(parts[3], out float value))
        {
            outputLabel.Text = "Invalid value.";
            return;
        }
        if (!int.TryParse(parts[4], out int duration))
        {
            outputLabel.Text = "Invalid duration.";
            return;
        }

        StatusEffectType? effectType = typeName switch
        {
            "attackup" => StatusEffectType.AttackDamageUp,
            "attackdown" => StatusEffectType.AttackDamageDown,
            "dodgeup" => StatusEffectType.DodgeUp,
            "dodgedown" => StatusEffectType.DodgeDown,
            "critup" => StatusEffectType.CritChanceUp,
            "critimmune" => StatusEffectType.CritImmunity,
            "stun" => StatusEffectType.Stun,
            _ => null
        };

        if (effectType == null)
        {
            outputLabel.Text = $"Unknown effect type: {typeName}";
            return;
        }

        bool isDebuff = typeName.Contains("down") || typeName == "stun";
        string displayName = $"Debug {typeName}";

        var effect = new StatusEffect(
            effectType.Value,
            displayName,
            value,
            duration,
            isDebuff,
            StatusEffectMode.Flat
        );
        effect.ApplyFlavorText = $"Debug: {displayName} applied.";
        effect.ExpireFlavorText = $"Debug: {displayName} fades.";

        StatusEffectManager manager = target == "hero"
            ? BattleSystem.Instance.GetHeroEffects()
            : BattleSystem.Instance.GetEnemyEffects();

        if (manager == null)
        {
            outputLabel.Text = "Effect manager not available.";
            return;
        }

        manager.ApplyEffect(effect, "devconsole");
        outputLabel.Text = $"Applied {displayName} ({value}, {duration} rounds) to {target}.";
    }

    private void HandleTestAbilityCommand(string[] parts)
    {
        if (!BattleSystem.Instance.IsBattleInProgress())
        {
            outputLabel.Text = "No active battle.";
            return;
        }

        // Creates a temporary test ability on the current enemy
        // Usage: testability
        // This adds a one-time "Test Strike" to the enemy that
        // deals 10 damage and applies a 3-round AttackDamageDown.
        // The enemy will use it on their next available turn.

        var dmgEffect = new AbilityEffect();
        dmgEffect.EffectType = AbilityEffectType.DealDamage;
        dmgEffect.Target = AbilityTargetType.Enemy;
        dmgEffect.Value = 10f;

        var debuffEffect = new AbilityEffect();
        debuffEffect.EffectType = AbilityEffectType.ApplyStatus;
        debuffEffect.Target = AbilityTargetType.Enemy;
        debuffEffect.StatusType = StatusEffectType.AttackDamageDown;
        debuffEffect.StatusName = "Test Weaken";
        debuffEffect.StatusValue = 3f;
        debuffEffect.StatusDuration = 3;
        debuffEffect.StatusIsDebuff = true;
        debuffEffect.StatusMode = StatusEffectMode.Flat;
        debuffEffect.StatusMaxStacks = 1;
        debuffEffect.StatusApplyText = "A test weakness takes hold...";
        debuffEffect.StatusExpireText = "The test weakness fades.";

        var testAbility = new CombatAbility();
        testAbility.AbilityId = "test_strike";
        testAbility.AbilityName = "Test Strike";
        testAbility.AbilityColor = new Color("9A7ABF");
        testAbility.Trigger = AbilityTrigger.OnCooldown;
        testAbility.ReplacesAttack = true;
        testAbility.OneTimeUse = true;
        testAbility.Priority = 100;
        testAbility.UseChance = 1.0f;
        testAbility.CastFlavorText = "The creature channels a mysterious force!";
        testAbility.Effects = new Godot.Collections.Array<AbilityEffect>
    {
        dmgEffect, debuffEffect
    };

        // Add to current enemy's ability list
        BattleSystem.Instance.GetCurrentContext().Enemy.Abilities.Add(testAbility);
        outputLabel.Text = "Added Test Strike to enemy. It will use it next turn.";
    }

    private void HandleEffectsCommand()
    {
        if (!BattleSystem.Instance.IsBattleInProgress())
        {
            outputLabel.Text = "No active battle.";
            return;
        }

        var heroEffects = BattleSystem.Instance.GetHeroEffects();
        var enemyEffects = BattleSystem.Instance.GetEnemyEffects();

        string result = "HERO EFFECTS:\n";
        if (heroEffects != null)
        {
            var heroList = heroEffects.GetActiveEffects();
            if (heroList.Count == 0)
                result += "  (none)\n";
            else
                for (int i = 0; i < heroList.Count; i++)
                    result += $"  {heroList[i].EffectName} " +
                              $"({heroList[i].Duration} rounds, " +
                              $"val:{heroList[i].Value})\n";
        }

        result += "\nENEMY EFFECTS:\n";
        if (enemyEffects != null)
        {
            var enemyList = enemyEffects.GetActiveEffects();
            if (enemyList.Count == 0)
                result += "  (none)";
            else
                for (int i = 0; i < enemyList.Count; i++)
                    result += $"  {enemyList[i].EffectName} " +
                              $"({enemyList[i].Duration} rounds, " +
                              $"val:{enemyList[i].Value})";
        }

        outputLabel.Text = result;
    }

    private void HandleAbilitiesCommand()
    {
        var manager = HeroAbilityManager.Instance;
        var allAbilities = manager.GetAllAbilities();

        if (allAbilities.Count == 0)
        {
            outputLabel.Text = "No hero abilities found.";
            return;
        }

        string result = $"HERO ABILITIES ({manager.GetUnlockedCount()}/{manager.GetTotalCount()}):\n";

        for (int i = 0; i < allAbilities.Count; i++)
        {
            var ability = allAbilities[i];
            bool unlocked = manager.IsUnlocked(ability.AbilityId);
            string status = unlocked ? "[UNLOCKED]" : "[locked]";
            string unlockInfo = "";

            if (ability.UnlockAtLevel > 0)
                unlockInfo += $" Lv.{ability.UnlockAtLevel}";
            if (ability.KleosPurchaseCost > 0)
                unlockInfo += $" {ability.KleosPurchaseCost} kleos";
            if (!string.IsNullOrEmpty(ability.UnlockFromDungeonId))
                unlockInfo += $" {ability.UnlockFromDungeonId}";

            result += $"  {status} {ability.AbilityName}{unlockInfo}\n";
        }

        outputLabel.Text = result;
    }

    private void HandleUnlockCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            outputLabel.Text = "Usage: unlock <abilityId>";
            return;
        }

        string abilityId = parts[1];
        var ability = HeroAbilityManager.Instance.GetAbilityById(abilityId);

        if (ability == null)
        {
            outputLabel.Text = $"Unknown ability: {abilityId}";
            return;
        }

        if (HeroAbilityManager.Instance.IsUnlocked(abilityId))
        {
            outputLabel.Text = $"{ability.AbilityName} already unlocked.";
            return;
        }

        // Force unlock by calling the save data path
        var saveData = HeroAbilityManager.Instance.GetSaveData();
        saveData.UnlockedAbilityIds.Add(abilityId);
        HeroAbilityManager.Instance.LoadFromSaveData(saveData);

        outputLabel.Text = $"Unlocked: {ability.AbilityName}";
    }

    private string CmdDeedTier(string[] parts)
    {
        if (parts.Length < 2)
            return "Usage: deed_tier <0-3>  (0=Bronze, 1=Silver, 2=Gold, 3=Divine)";

        if (!int.TryParse(parts[1], out int tier) || tier < 0 || tier > 3)
            return "Invalid tier. Use 0-3.";

        var container = GetTree().Root.FindChild("DeedButtonContainer", true, false);
        if (container == null)
            return "DeedButtonContainer not found in scene.";

        var evolution = container as DeedButtonEvolution;
        if (evolution == null)
            return "DeedButtonEvolution script not found.";

        evolution.ForceVisualTier(tier);
        string[] tierNames = { "Bronze", "Silver", "Gold", "Divine" };
        return $"Deed button forced to {tierNames[tier]} tier.";
    }
}
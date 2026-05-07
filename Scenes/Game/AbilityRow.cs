using Godot;

public partial class AbilityRow : PanelContainer
{
    // -------------------------------------------------------------------------
    // Node References
    // -------------------------------------------------------------------------

    [Export] public Label AbilityNameLabel { get; set; }
    [Export] public Label TypeBadgeLabel { get; set; }
    [Export] public Label TypeBadge2Label { get; set; }
    [Export] public Label DescriptionLabel { get; set; }
    [Export] public Label FlavorLabel { get; set; }
    [Export] public Label UnlockConditionLabel { get; set; }
    [Export] public Label StatusLabel { get; set; }
    [Export] public Button PurchaseButton { get; set; }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private CombatAbility abilityData;

    // -------------------------------------------------------------------------
    // Colors
    // -------------------------------------------------------------------------

    // Card states
    private static readonly Color UnlockedAccent = new Color("7A8A3A");
    private static readonly Color LockedModulate = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color NormalModulate = new Color(1f, 1f, 1f, 1f);

    // Type badge colors
    private static readonly Color AttackBg = new Color("C84030");
    private static readonly Color BuffBg = new Color("7A8A3A");
    private static readonly Color DebuffBg = new Color("B8860B");
    private static readonly Color HealBg = new Color("2E8B57");
    private static readonly Color CleanseBg = new Color("4682B4");

    private static readonly Color BadgeTextLight = new Color(1f, 1f, 1f, 0.9f);

    // Status badge colors
    private static readonly Color StatusUnlockedBg = new Color("7A8A3A");
    private static readonly Color StatusLockedBg = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    private static readonly Color StatusTextLight = new Color(1f, 1f, 1f, 0.85f);

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        if (PurchaseButton != null)
            PurchaseButton.Pressed += OnPurchasePressed;

        KleosManager.Instance.KleosChanged += OnKleosChanged;
        HeroManager.Instance.LevelUp += OnLevelUp;
        HeroAbilityManager.Instance.AbilityUnlocked += OnAbilityUnlocked;
        DungeonManager.Instance.DungeonCompleted += OnDungeonCompleted;
    }

    public override void _ExitTree()
    {
        KleosManager.Instance.KleosChanged -= OnKleosChanged;
        HeroManager.Instance.LevelUp -= OnLevelUp;
        HeroAbilityManager.Instance.AbilityUnlocked -= OnAbilityUnlocked;
        DungeonManager.Instance.DungeonCompleted -= OnDungeonCompleted;
    }

    // -------------------------------------------------------------------------
    // Setup
    // -------------------------------------------------------------------------

    public void Setup(CombatAbility ability)
    {
        abilityData = ability;
        RefreshDisplay();
    }

    // -------------------------------------------------------------------------
    // Display
    // -------------------------------------------------------------------------

    private void RefreshDisplay()
    {
        if (abilityData == null) return;

        bool unlocked = HeroAbilityManager.Instance.IsUnlocked(abilityData.AbilityId);

        // Name
        if (AbilityNameLabel != null)
            AbilityNameLabel.Text = abilityData.AbilityName;

        // Type badges
        SetTypeBadges();

        // Description (mechanical)
        if (DescriptionLabel != null)
            DescriptionLabel.Text = GetMechanicalDescription();

        // Flavor text
        if (FlavorLabel != null)
            FlavorLabel.Text = abilityData.CastFlavorText;

        // Unlock condition
        if (UnlockConditionLabel != null)
            UnlockConditionLabel.Text = GetUnlockConditionText();

        // Status and purchase button
        if (unlocked)
        {
            ShowUnlocked();
        }
        else if (abilityData.KleosPurchaseCost > 0)
        {
            ShowPurchasable();
        }
        else
        {
            ShowLocked();
        }
    }

    // -------------------------------------------------------------------------
    // Visual States
    // -------------------------------------------------------------------------

    private void ShowUnlocked()
    {
        Modulate = NormalModulate;

        if (StatusLabel != null)
        {
            StatusLabel.Text = "Unlocked";
            StatusLabel.Visible = true;
            SetLabelBadgeColor(StatusLabel, StatusUnlockedBg, StatusTextLight);
        }

        if (PurchaseButton != null)
            PurchaseButton.Visible = false;
    }

    private void ShowPurchasable()
    {
        Modulate = LockedModulate;

        if (StatusLabel != null)
            StatusLabel.Visible = false;

        if (PurchaseButton != null)
        {
            PurchaseButton.Visible = true;
            PurchaseButton.Text = $"Purchase ({abilityData.KleosPurchaseCost:N0})";
            PurchaseButton.Disabled = !HeroAbilityManager.Instance
                .CanPurchaseAbility(abilityData.AbilityId);
        }
    }

    private void ShowLocked()
    {
        Modulate = LockedModulate;

        if (StatusLabel != null)
        {
            StatusLabel.Text = "Locked";
            StatusLabel.Visible = true;
            SetLabelBadgeColor(StatusLabel, StatusLockedBg, StatusTextLight);
        }

        if (PurchaseButton != null)
            PurchaseButton.Visible = false;
    }

    // -------------------------------------------------------------------------
    // Type Badges
    // -------------------------------------------------------------------------

    private void SetTypeBadges()
    {
        if (abilityData == null) return;

        string type1 = "";
        Color color1 = BuffBg;
        string type2 = "";
        Color color2 = BuffBg;
        bool hasTwoBadges = false;

        // Determine types from the effects list
        bool hasDirectDamage = false;
        bool hasHeal = false;
        bool hasPoison = false;
        bool hasBuff = false;
        bool hasDebuff = false;
        bool hasCleanse = false;
        bool hasRegen = false;

        for (int i = 0; i < abilityData.Effects.Count; i++)
        {
            AbilityEffect effect = abilityData.Effects[i];

            switch (effect.EffectType)
            {
                case AbilityEffectType.DealDamage:
                    hasDirectDamage = true;
                    break;
                case AbilityEffectType.HealSelf:
                case AbilityEffectType.HealTarget:
                    hasHeal = true;
                    break;
                case AbilityEffectType.RemoveDebuff:
                    hasCleanse = true;
                    break;
                case AbilityEffectType.ApplySelfStatus:
                    if (effect.StatusType == StatusEffectType.Regeneration)
                        hasRegen = true;
                    else if (!effect.StatusIsDebuff)
                        hasBuff = true;
                    else
                        hasDebuff = true;
                    break;
                case AbilityEffectType.ApplyStatus:
                    if (effect.StatusType == StatusEffectType.Poison)
                        hasPoison = true;
                    else if (effect.StatusIsDebuff)
                        hasDebuff = true;
                    else
                        hasBuff = true;
                    break;
            }
        }

        // Pick primary badge
        if (hasDirectDamage)
        {
            type1 = "Attack";
            color1 = AttackBg;
        }
        else if (hasRegen)
        {
            type1 = "Regen";
            color1 = HealBg;
        }
        else if (hasHeal)
        {
            type1 = "Heal";
            color1 = HealBg;
        }
        else if (hasBuff)
        {
            type1 = "Self buff";
            color1 = BuffBg;
        }
        else if (hasDebuff)
        {
            type1 = "Debuff";
            color1 = DebuffBg;
        }
        else if (hasCleanse)
        {
            type1 = "Cleanse";
            color1 = CleanseBg;
        }

        // Pick secondary badge if ability has multiple effect types
        if (hasDirectDamage && hasPoison)
        {
            type2 = "Poison";
            color2 = DebuffBg;
            hasTwoBadges = true;
        }
        else if (hasHeal && hasCleanse)
        {
            type2 = "Cleanse";
            color2 = CleanseBg;
            hasTwoBadges = true;
        }

        if (TypeBadgeLabel != null)
        {
            TypeBadgeLabel.Text = type1;
            SetLabelBadgeColor(TypeBadgeLabel, color1, BadgeTextLight);
        }

        if (TypeBadge2Label != null)
        {
            if (hasTwoBadges)
            {
                TypeBadge2Label.Text = type2;
                TypeBadge2Label.Visible = true;
                SetLabelBadgeColor(TypeBadge2Label, color2, BadgeTextLight);
            }
            else
            {
                TypeBadge2Label.Visible = false;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Description Builder
    // -------------------------------------------------------------------------

    private string GetMechanicalDescription()
    {
        if (abilityData == null) return "";

        var parts = new System.Collections.Generic.List<string>();

        for (int i = 0; i < abilityData.Effects.Count; i++)
        {
            AbilityEffect effect = abilityData.Effects[i];

            switch (effect.EffectType)
            {
                case AbilityEffectType.DealDamage:
                    parts.Add($"Deals {effect.Value:G} damage");
                    break;
                case AbilityEffectType.HealSelf:
                    parts.Add($"Heals {effect.Value:G} HP");
                    break;
                case AbilityEffectType.ApplySelfStatus:
                    parts.Add(DescribeStatusEffect(effect, true));
                    break;
                case AbilityEffectType.ApplyStatus:
                    parts.Add(DescribeStatusEffect(effect, false));
                    break;
                case AbilityEffectType.RemoveDebuff:
                    parts.Add("Removes poison");
                    break;
            }
        }

        string desc = string.Join(" + ", parts) + ".";

        // Add trigger info
        desc += " " + GetTriggerDescription();

        // Add cooldown
        if (abilityData.CooldownRounds > 0)
            desc += $" {abilityData.CooldownRounds} round cooldown.";

        // Add one-time use
        if (abilityData.OneTimeUse)
            desc += " One use per battle.";

        // Add replaces attack info
        if (!abilityData.ReplacesAttack)
            desc += " Also attacks normally.";

        return desc;
    }

    private string DescribeStatusEffect(AbilityEffect effect, bool isSelf)
    {
        string target = isSelf ? "" : "enemy ";
        string sign = effect.StatusIsDebuff ? "-" : "+";

        switch (effect.StatusType)
        {
            case StatusEffectType.AttackDamageUp:
                return effect.StatusMode == StatusEffectMode.Percentage
                    ? $"+{Mathf.Round(effect.StatusValue * 100)}% attack damage for {effect.StatusDuration} rounds"
                    : $"+{effect.StatusValue:G} attack damage for {effect.StatusDuration} rounds";
            case StatusEffectType.DodgeUp:
                return $"+{Mathf.Round(effect.StatusValue * 100)}% dodge chance for {effect.StatusDuration} rounds";
            case StatusEffectType.CritChanceUp:
                return $"+{Mathf.Round(effect.StatusValue * 100)}% crit chance for {effect.StatusDuration} rounds";
            case StatusEffectType.Shield:
                return $"Absorbs {effect.StatusValue:G} damage";
            case StatusEffectType.Regeneration:
                return $"Regenerates {effect.StatusValue:G} HP/round for {effect.StatusDuration} rounds";
            case StatusEffectType.Poison:
                return $"Poisons for {effect.StatusValue:G}/round over {effect.StatusDuration} rounds";
            default:
                return $"{effect.StatusName} for {effect.StatusDuration} rounds";
        }
    }

    private string GetTriggerDescription()
    {
        switch (abilityData.Trigger)
        {
            case AbilityTrigger.OnCooldown:
                return "Uses when ready.";
            case AbilityTrigger.WhenHPBelow:
                return $"Activates below {Mathf.Round(abilityData.TriggerValue * 100)}% HP.";
            case AbilityTrigger.WhenHPAbove:
                return $"Activates above {Mathf.Round(abilityData.TriggerValue * 100)}% HP.";
            case AbilityTrigger.FirstRound:
                return "Activates on first round.";
            case AbilityTrigger.EveryNRounds:
                return $"Activates every {(int)abilityData.TriggerValue} rounds.";
            default:
                return "";
        }
    }

    // -------------------------------------------------------------------------
    // Unlock Condition Text
    // -------------------------------------------------------------------------

    private string GetUnlockConditionText()
    {
        if (abilityData == null) return "";

        if (abilityData.UnlockAtLevel > 0)
        {
            bool met = HeroManager.Instance.GetLevel() >= abilityData.UnlockAtLevel;
            return met
                ? $"Unlocked at level {abilityData.UnlockAtLevel}"
                : $"Requires level {abilityData.UnlockAtLevel}";
        }

        if (abilityData.KleosPurchaseCost > 0)
            return $"{abilityData.KleosPurchaseCost:N0} kleos";

        if (!string.IsNullOrEmpty(abilityData.UnlockFromDungeonId))
        {
            DungeonData dungeon = DungeonManager.Instance
                .GetDungeonById(abilityData.UnlockFromDungeonId);
            string dungeonName = dungeon != null
                ? dungeon.DungeonName
                : abilityData.UnlockFromDungeonId;

            bool cleared = DungeonManager.Instance
                .IsDungeonCompleted(abilityData.UnlockFromDungeonId);
            return cleared
                ? $"Clear {dungeonName}"
                : $"Clear {dungeonName}";
        }

        return "";
    }

    // -------------------------------------------------------------------------
    // Badge Helper
    // -------------------------------------------------------------------------

    private void SetLabelBadgeColor(Label label, Color bg, Color text)
    {
        var style = new StyleBoxFlat();
        style.BgColor = bg;
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 2;
        style.ContentMarginBottom = 2;
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        label.AddThemeStyleboxOverride("normal", style);
        label.AddThemeColorOverride("font_color", text);
    }

    // -------------------------------------------------------------------------
    // Event Handlers
    // -------------------------------------------------------------------------

    private void OnPurchasePressed()
    {
        if (abilityData == null) return;
        if (HeroAbilityManager.Instance.PurchaseAbility(abilityData.AbilityId))
            RefreshDisplay();
    }

    private void OnKleosChanged(float amount)
    {
        if (abilityData == null) return;
        if (HeroAbilityManager.Instance.IsUnlocked(abilityData.AbilityId)) return;
        if (abilityData.KleosPurchaseCost <= 0) return;
        RefreshDisplay();
    }

    private void OnLevelUp(int newLevel)
    {
        if (abilityData == null) return;
        RefreshDisplay();
    }

    private void OnAbilityUnlocked(string abilityId)
    {
        if (abilityData == null) return;
        if (abilityData.AbilityId == abilityId)
            RefreshDisplay();
    }

    private void OnDungeonCompleted(string dungeonId)
    {
        if (abilityData == null) return;
        RefreshDisplay();
    }
}
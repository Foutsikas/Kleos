using Godot;

public partial class MainGameController : Control
{
    // --- Node References ---
    [Export] public Label KleosLabel { get; set; }
    [Export] public Label ProductionLabel { get; set; }
    [Export] public Button DeedButton { get; set; }
    [Export] public Label DeedContextLabel { get; set; }
    [Export] public PanelContainer HeroPortrait { get; set; }
    [Export] public Label HeroLevelLabel { get; set; }
    [Export] public ProgressBar HeroHPBar { get; set; }
    [Export] public ProgressBar HeroXPBar { get; set; }
    [Export] public Control HeroPanel { get; set; }
    [Export] public Label HeroPanelTitle { get; set; }
    [Export] public Label HeroNameLabel { get; set; }
    [Export] public Label HeroLevelDetailLabel { get; set; }
    [Export] public ProgressBar HeroHPDetailBar { get; set; }
    [Export] public ProgressBar HeroXPDetailBar { get; set; }
    [Export] public Label StrengthValueLabel { get; set; }
    [Export] public Label EnduranceValueLabel { get; set; }
    [Export] public Label CunningValueLabel { get; set; }
    [Export] public Label FavorValueLabel { get; set; }
    [Export] public Button StrengthUpgradeButton { get; set; }
    [Export] public Button EnduranceUpgradeButton { get; set; }
    [Export] public Button CunningUpgradeButton { get; set; }
    [Export] public Button FavorUpgradeButton { get; set; }
    [Export] public Label StatPointsLabel { get; set; }
    [Export] public Label CombatStatsLabel { get; set; }
    [Export] public ColorRect FadeOverlay { get; set; }
    [Export] public PackedScene ArtisanRowScene { get; set; }
    [Export] public VBoxContainer ArtisanList { get; set; }

    // --- State ---
    private bool heroPanelOpen = false;

    private void OnArtisanUnlocked(string artisanId)
    {
        PopulateArtisanList();
    }

    // --- Lifecycle ---

    public override void _Ready()
    {
        ConnectSignals();
        ConnectButtons();
        FadeIn();
        RefreshKleosDisplay(KleosManager.Instance.CurrentKleos);
        RefreshProductionDisplay(KleosManager.Instance.TotalKleosPerSecond);
        RefreshHeroDisplay();
        RefreshDeedContext();
        PopulateArtisanList();
    }

    // --- Signal Connections ---

    private void ConnectSignals()
    {
        KleosManager.Instance.KleosChanged += RefreshKleosDisplay;
        KleosManager.Instance.ProductionChanged += RefreshProductionDisplay;
        KleosManager.Instance.DeedContextChanged += RefreshDeedContext;
        HeroManager.Instance.StatsChanged += RefreshHeroDisplay;
        HeroManager.Instance.LevelUp += OnHeroLevelUp;
        ArtisanManager.Instance.ArtisanUnlocked += OnArtisanUnlocked;
    }

    private void ConnectButtons()
    {
        if (DeedButton != null)
            DeedButton.Pressed += OnDeedButtonPressed;

        if (HeroPortrait != null)
            HeroPortrait.GuiInput += OnHeroPortraitInput;

        if (StrengthUpgradeButton != null)
            StrengthUpgradeButton.Pressed += () => OnStatUpgradePressed(HeroStat.Strength);
        if (EnduranceUpgradeButton != null)
            EnduranceUpgradeButton.Pressed += () => OnStatUpgradePressed(HeroStat.Endurance);
        if (CunningUpgradeButton != null)
            CunningUpgradeButton.Pressed += () => OnStatUpgradePressed(HeroStat.Cunning);
        if (FavorUpgradeButton != null)
            FavorUpgradeButton.Pressed += () => OnStatUpgradePressed(HeroStat.Favor);
    }

    // --- Deed Button ---

    private void OnDeedButtonPressed()
    {
        KleosManager.Instance.DoDeed();
        RandomEncounterManager.Instance.OnDeedClicked();
    }

    // --- Hero Portrait ---

    private void OnHeroPortraitInput(InputEvent e)
    {
        if (e is InputEventMouseButton mouseEvent
            && mouseEvent.Pressed
            && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            ToggleHeroPanel();
        }
    }

    private void ToggleHeroPanel()
    {
        heroPanelOpen = !heroPanelOpen;
        if (HeroPanel != null)
            HeroPanel.Visible = heroPanelOpen;
    }

    // --- Stat Upgrades ---

    private void OnStatUpgradePressed(HeroStat stat)
    {
        HeroManager.Instance.UpgradeStat(stat);
    }

    // --- Display Refresh ---

    private void RefreshKleosDisplay(float amount)
    {
        if (KleosLabel != null)
            KleosLabel.Text = $"Kleos: {Mathf.Floor(amount):N0}";
    }

    private void RefreshProductionDisplay(float amount)
    {
        if (ProductionLabel != null)
            ProductionLabel.Text = $"{amount:F1} K/s";
    }

    private void RefreshDeedContext()
    {
        if (DeedContextLabel == null) return;

        int totalArtisans = 0;
        for (int i = 0; i < ArtisanManager.Instance.ArtisanConfigs.Count; i++)
        {
            var artisan = ArtisanManager.Instance.ArtisanConfigs[i].As<ArtisanData>();
            if (artisan != null)
                totalArtisans += ArtisanManager.Instance.GetOwnedCount(artisan.ArtisanId);
        }

        DeedContextLabel.Text = totalArtisans switch
        {
            0 => "Training in solitude...",
            1 => "Inspiring scribes with your actions...",
            2 => "Your deeds reach the ears of bards...",
            3 => "Potters craft vessels depicting your trials...",
            4 => "Sculptors immortalize your humble service...",
            5 => "Playwrights chronicle your quiet heroism...",
            _ => "Historians record your selfless acts..."
        };
    }

    private void RefreshHeroDisplay()
    {
        float maxHP = HeroManager.Instance.GetMaxHP();
        float currentHP = HeroManager.Instance.GetCurrentHP();
        float currentXP = HeroManager.Instance.GetCurrentXP();
        float xpToNext = HeroManager.Instance.GetXPToNextLevel();
        int level = HeroManager.Instance.GetLevel();
        int statPoints = HeroManager.Instance.GetAvailableStatPoints();

        // Compact portrait
        if (HeroLevelLabel != null)
            HeroLevelLabel.Text = $"Lv. {level}";
        if (HeroHPBar != null)
        {
            HeroHPBar.MaxValue = maxHP;
            HeroHPBar.Value = currentHP;
        }
        if (HeroXPBar != null)
        {
            HeroXPBar.MaxValue = xpToNext;
            HeroXPBar.Value = currentXP;
        }

        // Full hero panel
        if (HeroLevelDetailLabel != null)
            HeroLevelDetailLabel.Text = $"Level {level}";
        if (HeroHPDetailBar != null)
        {
            HeroHPDetailBar.MaxValue = maxHP;
            HeroHPDetailBar.Value = currentHP;
        }
        if (HeroXPDetailBar != null)
        {
            HeroXPDetailBar.MaxValue = xpToNext;
            HeroXPDetailBar.Value = currentXP;
        }

        // Stat values
        if (StrengthValueLabel != null)
            StrengthValueLabel.Text = HeroManager.Instance.GetStrength().ToString();
        if (EnduranceValueLabel != null)
            EnduranceValueLabel.Text = HeroManager.Instance.GetEndurance().ToString();
        if (CunningValueLabel != null)
            CunningValueLabel.Text = HeroManager.Instance.GetCunning().ToString();
        if (FavorValueLabel != null)
            FavorValueLabel.Text = HeroManager.Instance.GetFavor().ToString();

        // Stat points
        if (StatPointsLabel != null)
            StatPointsLabel.Text = $"Stat Points: {statPoints}";

        // Combat stats
        if (CombatStatsLabel != null)
        {
            float dodge = HeroManager.Instance.GetDodgeChance() * 100f;
            float crit = HeroManager.Instance.GetCritChance() * 100f;
            float dmg = HeroManager.Instance.GetDamage();
            CombatStatsLabel.Text = $"DMG: {dmg:F0}  DODGE: {dodge:F1}%  CRIT: {crit:F1}%";
        }

        // Upgrade button states
        bool hasPoints = statPoints > 0;
        if (StrengthUpgradeButton != null) StrengthUpgradeButton.Disabled = !hasPoints;
        if (EnduranceUpgradeButton != null) EnduranceUpgradeButton.Disabled = !hasPoints;
        if (CunningUpgradeButton != null) CunningUpgradeButton.Disabled = !hasPoints;
        if (FavorUpgradeButton != null) FavorUpgradeButton.Disabled = !hasPoints;
    }

    private void OnHeroLevelUp(int newLevel)
    {
        GD.Print($"[MainGame] Hero reached level {newLevel}!");
    }

    // --- Artisan ---
    private void PopulateArtisanList()
    {
        if (ArtisanList == null || ArtisanRowScene == null) return;

        foreach (Node child in ArtisanList.GetChildren())
            child.QueueFree();

        for (int i = 0; i < ArtisanManager.Instance.ArtisanConfigs.Count; i++)
        {
            var artisan = ArtisanManager.Instance.ArtisanConfigs[i].As<ArtisanData>();
            if (artisan == null) continue;
            if (!ArtisanManager.Instance.IsArtisanUnlocked(artisan)) continue;

            var row = ArtisanRowScene.Instantiate<ArtisanRow>();
            ArtisanList.AddChild(row);
            row.Setup(artisan);
        }
    }

    // --- Fade ---

    private async void FadeIn()
    {
        if (FadeOverlay == null) return;

        FadeOverlay.Visible = true;
        FadeOverlay.Modulate = new Color(0, 0, 0, 1);

        var tween = CreateTween();
        tween.TweenProperty(FadeOverlay, "modulate:a", 0f, 1.0);
        await ToSignal(tween, Tween.SignalName.Finished);

        FadeOverlay.Visible = false;
    }
}
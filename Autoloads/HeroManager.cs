using Godot;

public partial class HeroManager : Node
{
    public static HeroManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void LevelUpEventHandler(int newLevel);
    [Signal] public delegate void StatsChangedEventHandler();

    // --- Data ---
    private HeroData heroData = new();
    private float currentHP = 0f;

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;

        currentHP = heroData.GetMaxHP();

        // Subscribe to kleos gained for XP tracking
        KleosManager.Instance.KleosGained += OnKleosGained;

        GD.Print("[HeroManager] Ready.");
    }

    // --- XP and Leveling ---

    private void OnKleosGained(float amount)
    {
        AddExperience(amount);
    }

    public void AddExperience(float amount)
    {
        float xpMultiplier = UpgradeManager.Instance.GetMultiplier(ModifierType.XPMultiplier);
        heroData.CurrentXP += amount * xpMultiplier;
        CheckForLevelUp();
        EmitSignal(SignalName.StatsChanged);
    }

    private void CheckForLevelUp()
    {
        float xpThreshold = GetXPThreshold(heroData.Level);

        while (heroData.CurrentXP >= xpThreshold)
        {
            heroData.CurrentXP -= xpThreshold;
            heroData.Level++;
            heroData.AvailableStatPoints += 3;

            GD.Print($"[HeroManager] Level up! Now level {heroData.Level}.");
            EmitSignal(SignalName.LevelUp, heroData.Level);

            xpThreshold = GetXPThreshold(heroData.Level);
        }
    }

    private float GetXPThreshold(int level)
    {
        return 1000f * Mathf.Pow(1.5f, level - 1);
    }

    // --- Stat Allocation ---

    public bool UpgradeStat(HeroStat stat)
    {
        if (heroData.AvailableStatPoints <= 0) return false;

        switch (stat)
        {
            case HeroStat.Strength:
                heroData.StrengthUpgrades++;
                break;
            case HeroStat.Endurance:
                heroData.EnduranceUpgrades++;
                float newMaxHP = heroData.GetMaxHP();
                currentHP = Mathf.Min(currentHP + 5f, newMaxHP);
                break;
            case HeroStat.Cunning:
                heroData.CunningUpgrades++;
                break;
            case HeroStat.Favor:
                heroData.FavorUpgrades++;
                break;
            default:
                return false;
        }

        heroData.AvailableStatPoints--;
        EmitSignal(SignalName.StatsChanged);
        GD.Print($"[HeroManager] Upgraded {stat}. Points remaining: {heroData.AvailableStatPoints}.");
        return true;
    }

    // --- Public Accessors ---

    public int GetLevel() => heroData.Level;
    public float GetCurrentXP() => heroData.CurrentXP;
    public float GetXPToNextLevel() => GetXPThreshold(heroData.Level);
    public int GetAvailableStatPoints() => heroData.AvailableStatPoints;
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => heroData.GetMaxHP();
    public float GetDamage() => heroData.GetDamage();
    public float GetDodgeChance() => heroData.GetDodgeChance();
    public float GetCritChance() => heroData.GetCritChance();
    public float GetCritMultiplier() => heroData.GetCritMultiplier();
    public int GetStrength() => heroData.GetStrength();
    public int GetEndurance() => heroData.GetEndurance();
    public int GetCunning() => heroData.GetCunning();
    public int GetFavor() => heroData.GetFavor();

    // --- HP Management ---

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(0f, currentHP - amount);
        EmitSignal(SignalName.StatsChanged);
    }

    public void RestoreHP(float amount)
    {
        currentHP = Mathf.Min(heroData.GetMaxHP(), currentHP + amount);
        EmitSignal(SignalName.StatsChanged);
    }

    public void RestoreFullHP()
    {
        currentHP = heroData.GetMaxHP();
        EmitSignal(SignalName.StatsChanged);
    }

    public bool IsAlive() => currentHP > 0f;

    // --- Combat Rolls ---

    public bool RollDodge()
    {
        return GD.Randf() < heroData.GetDodgeChance();
    }

    public bool RollCrit()
    {
        return GD.Randf() < heroData.GetCritChance();
    }

    // --- Reset ---

    public void ResetHero()
    {
        heroData = new HeroData();
        currentHP = heroData.GetMaxHP();
        EmitSignal(SignalName.StatsChanged);
        GD.Print("[HeroManager] Hero reset.");
    }

    // --- Save / Load ---

    public HeroSaveData GetSaveData()
    {
        return new HeroSaveData
        {
            Level = heroData.Level,
            CurrentXP = heroData.CurrentXP,
            AvailableStatPoints = heroData.AvailableStatPoints,
            StrengthUpgrades = heroData.StrengthUpgrades,
            EnduranceUpgrades = heroData.EnduranceUpgrades,
            CunningUpgrades = heroData.CunningUpgrades,
            FavorUpgrades = heroData.FavorUpgrades
        };
    }

    public void LoadFromSaveData(HeroSaveData data)
    {
        heroData.Level = data.Level;
        heroData.CurrentXP = data.CurrentXP;
        heroData.AvailableStatPoints = data.AvailableStatPoints;
        heroData.StrengthUpgrades = data.StrengthUpgrades;
        heroData.EnduranceUpgrades = data.EnduranceUpgrades;
        heroData.CunningUpgrades = data.CunningUpgrades;
        heroData.FavorUpgrades = data.FavorUpgrades;
        currentHP = heroData.GetMaxHP();
        EmitSignal(SignalName.StatsChanged);
        GD.Print($"[HeroManager] Loaded. Level: {heroData.Level}.");
    }
}
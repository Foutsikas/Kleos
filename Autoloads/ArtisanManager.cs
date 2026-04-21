using Godot;
using Godot.Collections;

public partial class ArtisanManager : Node
{
    public static ArtisanManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void ArtisanPurchasedEventHandler(string artisanId);
    [Signal] public delegate void ArtisanUnlockedEventHandler(string artisanId);
    [Signal] public delegate void ProductionRecalculatedEventHandler(float totalKPS);

    // --- Config ---
    public Array ArtisanConfigs { get; private set; } = new();

    private void LoadConfigs()
    {
        ArtisanConfigs.Clear();
        string[] paths = new[]
        {
        "res://Resources/Artisans/scribe.tres",
        "res://Resources/Artisans/bard.tres",
        "res://Resources/Artisans/potter.tres",
        "res://Resources/Artisans/sculptor.tres",
        "res://Resources/Artisans/playwright.tres",
        "res://Resources/Artisans/historian.tres"
    };
        foreach (var path in paths)
            ArtisanConfigs.Add(GD.Load<ArtisanData>(path));
    }

    // --- State ---
    private Dictionary<string, int> ownedCounts = new();
    private Array<string> unlockedArtisans = new();
    private bool hasInitialized = false;

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;

        if (!hasInitialized)
            Initialize();

        GD.Print("[ArtisanManager] Ready.");
    }

    private void Initialize()
    {
        LoadConfigs();
        hasInitialized = true;
        RefreshUnlocks();
        RecalculateTotalProduction();
    }

    // --- Unlock System ---

    private void RefreshUnlocks()
    {
        for (int i = 0; i < ArtisanConfigs.Count; i++)
        {
            var artisan = ArtisanConfigs[i].As<ArtisanData>();
            if (artisan == null) continue;
            if (unlockedArtisans.Contains(artisan.ArtisanId)) continue;

            if (IsArtisanUnlocked(artisan))
            {
                unlockedArtisans.Add(artisan.ArtisanId);
                EmitSignal(SignalName.ArtisanUnlocked, artisan.ArtisanId);
                GD.Print($"[ArtisanManager] Unlocked: {artisan.ArtisanName}.");
            }
        }
    }

    public bool IsArtisanUnlocked(ArtisanData artisan)
    {
        if (string.IsNullOrEmpty(artisan.RequiredArtisanId)) return true;
        int owned = GetOwnedCount(artisan.RequiredArtisanId);
        return owned >= artisan.RequiredArtisanCount;
    }

    // --- Purchase ---

    public bool CanPurchase(ArtisanData artisan)
    {
        if (artisan == null) return false;
        if (!IsArtisanUnlocked(artisan)) return false;
        if (KleosManager.Instance.CurrentKleos < GetCurrentCost(artisan)) return false;
        return true;
    }

    public bool PurchaseArtisan(ArtisanData artisan)
    {
        if (!CanPurchase(artisan)) return false;

        float cost = GetCurrentCost(artisan);
        if (!KleosManager.Instance.SpendKleos(cost)) return false;

        if (!ownedCounts.ContainsKey(artisan.ArtisanId))
            ownedCounts[artisan.ArtisanId] = 0;

        ownedCounts[artisan.ArtisanId]++;

        EmitSignal(SignalName.ArtisanPurchased, artisan.ArtisanId);
        GD.Print($"[ArtisanManager] Purchased {artisan.ArtisanName}. Owned: {ownedCounts[artisan.ArtisanId]}.");

        RefreshUnlocks();
        RecalculateTotalProduction();
        return true;
    }

    // --- Queries ---

    public int GetOwnedCount(string artisanId)
    {
        return ownedCounts.TryGetValue(artisanId, out int count) ? count : 0;
    }

    public float GetCurrentCost(ArtisanData artisan)
    {
        int owned = GetOwnedCount(artisan.ArtisanId);
        return artisan.BaseCost * Mathf.Pow(artisan.CostMultiplier, owned);
    }

    public ArtisanData GetArtisanById(string artisanId)
    {
        for (int i = 0; i < ArtisanConfigs.Count; i++)
        {
            var artisan = ArtisanConfigs[i].As<ArtisanData>();
            if (artisan != null && artisan.ArtisanId == artisanId)
                return artisan;
        }
        return null;
    }

    // --- Production ---

    public void RecalculateTotalProduction()
    {
        float total = 0f;

        for (int i = 0; i < ArtisanConfigs.Count; i++)
        {
            var artisan = ArtisanConfigs[i].As<ArtisanData>();
            if (artisan == null) continue;

            int owned = GetOwnedCount(artisan.ArtisanId);
            if (owned <= 0) continue;

            float baseProd = artisan.KleosPerSecond * owned;
            float multiplier = UpgradeManager.Instance.GetMultiplier(ModifierType.ArtisanProductionMultiplier);
            total += baseProd * multiplier;
        }

        KleosManager.Instance.RecalculateTotalProduction(total);
        EmitSignal(SignalName.ProductionRecalculated, total);
    }

    // --- Save / Load ---

    public ArtisanSaveData GetSaveData()
    {
        return new ArtisanSaveData
        {
            OwnedCounts = new Dictionary<string, int>(ownedCounts),
            UnlockedArtisans = new Array<string>(unlockedArtisans)
        };
    }

    public void LoadFromSaveData(ArtisanSaveData data)
    {
        hasInitialized = true;
        ownedCounts = data.OwnedCounts ?? new Dictionary<string, int>();
        unlockedArtisans = data.UnlockedArtisans ?? new Array<string>();
        RefreshUnlocks();
        RecalculateTotalProduction();
        GD.Print($"[ArtisanManager] Loaded. Owned counts: {ownedCounts.Count}.");
    }
}
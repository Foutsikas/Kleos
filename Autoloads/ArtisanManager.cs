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
        var db = GD.Load<ArtisanDatabase>("res://Resources/Artisans/artisan_database.tres");

        if (db == null)
        {
            GD.PrintErr("Artisan DB failed to load");
            return;
        }

        ArtisanConfigs = (Array)db.Artisans;
        SortByUnlockOrder();
    }

    private void SortByUnlockOrder()
    {
        // Artisans must appear in unlock chain order:
        // Scribe (no requirement) first, then each artisan whose
        // requirement is the previous one in the chain.

        var unsorted = new System.Collections.Generic.List<ArtisanData>();
        for (int i = 0; i < ArtisanConfigs.Count; i++)
        {
            var artisan = ArtisanConfigs[i].As<ArtisanData>();
            if (artisan != null)
                unsorted.Add(artisan);
        }

        var sorted = new System.Collections.Generic.List<ArtisanData>();
        var remaining = new System.Collections.Generic.List<ArtisanData>(unsorted);

        // First pass: find the artisan with no requirement (Scribe)
        for (int i = remaining.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrEmpty(remaining[i].RequiredArtisanId))
            {
                sorted.Add(remaining[i]);
                remaining.RemoveAt(i);
            }
        }

        // Chain: find the artisan whose requirement is the last sorted one
        int safety = 0;
        while (remaining.Count > 0 && safety < 20)
        {
            safety++;
            string lastId = sorted[sorted.Count - 1].ArtisanId;
            bool found = false;
            for (int i = 0; i < remaining.Count; i++)
            {
                if (remaining[i].RequiredArtisanId == lastId)
                {
                    sorted.Add(remaining[i]);
                    remaining.RemoveAt(i);
                    found = true;
                    break;
                }
            }
            if (!found) break;
        }

        // Append anything left over (should not happen with correct data)
        sorted.AddRange(remaining);

        ArtisanConfigs.Clear();
        foreach (var artisan in sorted)
            ArtisanConfigs.Add(artisan);
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
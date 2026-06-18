using Godot;
using Godot.Collections;

public partial class ArtisanManager : Node
{
    public static ArtisanManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void ArtisanPurchasedEventHandler(string artisanId);
    [Signal] public delegate void ArtisanUnlockedEventHandler(string artisanId);
    [Signal] public delegate void ProductionRecalculatedEventHandler(float totalKPS);
    [Signal] public delegate void BuyMultiplierChangedEventHandler(int multiplier);

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

    // --- Buy Multiplier ---
    private int currentBuyMultiplier = 1;
    private static readonly int[] BuyMultiplierCycle = { 1, 10, 100 };

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

    // All-or-nothing on the rounded quantity: enabled only if the player can
    // afford the whole batch.
    public bool CanPurchase(ArtisanData artisan, int quantity)
    {
        if (artisan == null) return false;
        if (!IsArtisanUnlocked(artisan)) return false;
        if (quantity <= 0) return false;
        if (KleosManager.Instance.CurrentKleos < GetBulkCost(artisan, quantity)) return false;
        return true;
    }

    public bool PurchaseArtisan(ArtisanData artisan, int quantity)
    {
        if (!CanPurchase(artisan, quantity)) return false;

        float cost = GetBulkCost(artisan, quantity);
        if (!KleosManager.Instance.SpendKleos(cost)) return false;

        if (!ownedCounts.ContainsKey(artisan.ArtisanId))
            ownedCounts[artisan.ArtisanId] = 0;

        ownedCounts[artisan.ArtisanId] += quantity;

        // Fire once for the whole batch. RefreshUnlocks rechecks every condition,
        // so a single emit is enough for the unlock cascade and row listeners.
        EmitSignal(SignalName.ArtisanPurchased, artisan.ArtisanId);
        GD.Print($"[ArtisanManager] Purchased {quantity}x {artisan.ArtisanName}. Owned: {ownedCounts[artisan.ArtisanId]}.");

        RefreshUnlocks();
        RecalculateTotalProduction();
        return true;
    }

    // --- Buy Multiplier ---

    public int GetBuyMultiplier()
    {
        return currentBuyMultiplier;
    }

    // Returns true if the given buy-multiplier tier is available to the player.
    // x1 and x10 are always available. x100 unlocks with the "the Tireless"
    // deed epithet (10,000 lifetime deeds). Epithets arrive in V0.95, so this
    // stays false until that system is wired.
    public bool IsBuyMultiplierUnlocked(int multiplier)
    {
        if (multiplier <= 10) return true;
        // V0.95: replace with EpithetManager.Instance.HasDeedUnlock("the_tireless").
        return false;
    }

    // Steps the buy multiplier to the next unlocked tier in the cycle, wrapping
    // back to the start. Locked tiers (x100 until V0.95) are skipped.
    public void CycleBuyMultiplier()
    {
        int currentIndex = System.Array.IndexOf(BuyMultiplierCycle, currentBuyMultiplier);

        for (int step = 1; step <= BuyMultiplierCycle.Length; step++)
        {
            int nextIndex = (currentIndex + step) % BuyMultiplierCycle.Length;
            int candidate = BuyMultiplierCycle[nextIndex];
            if (IsBuyMultiplierUnlocked(candidate))
            {
                currentBuyMultiplier = candidate;
                EmitSignal(SignalName.BuyMultiplierChanged, currentBuyMultiplier);
                GD.Print($"[ArtisanManager] Buy multiplier set to x{currentBuyMultiplier}.");
                return;
            }
        }
    }

    // Returns how many of this artisan the current buy multiplier will buy.
    // For x1 this is always 1. For x10 and x100 it rounds up to the next clean
    // multiple: owning 8 with x10 buys 2 (reaching 10); owning 10 with x10 buys
    // 10 (reaching 20); owning 0 buys the full multiplier.
    public int GetRoundedQuantity(ArtisanData artisan)
    {
        if (artisan == null) return 0;
        int mult = currentBuyMultiplier;
        if (mult <= 1) return 1;

        int owned = GetOwnedCount(artisan.ArtisanId);
        int nextMultiple = ((owned / mult) + 1) * mult;
        return nextMultiple - owned;
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

    // Total cost of buying `quantity` of this artisan starting from the current
    // owned count. Each purchase raises the next price by CostMultiplier, so the
    // total is a geometric series, not quantity multiplied by the current price.
    public float GetBulkCost(ArtisanData artisan, int quantity)
    {
        if (artisan == null || quantity <= 0) return 0f;

        int owned = GetOwnedCount(artisan.ArtisanId);
        float first = artisan.BaseCost * Mathf.Pow(artisan.CostMultiplier, owned);
        float ratio = artisan.CostMultiplier;

        // Guard against a ratio of exactly 1, which would divide by zero. Falls
        // back to flat pricing. No current artisan uses a 1.0 multiplier.
        if (Mathf.IsEqualApprox(ratio, 1f))
            return first * quantity;

        return first * (Mathf.Pow(ratio, quantity) - 1f) / (ratio - 1f);
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

    public int GetUnlockedCount()
    {
        return unlockedArtisans.Count;
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
using Godot;

public static class DungeonRewardCalculator
{
    // --- RNG Variance Table (Random Encounters Only) ---
    // Each entry: cumulative probability threshold, multiplier.
    // 40% -> 1x, 20% -> 2x, 15% -> 3x, 10% -> 4x, 5% -> 6x, 5% -> 10x, 5% -> 20x.
    // Average multiplier across all outcomes: ~3.55x weighted.

    private static readonly (float cumulative, int multiplier)[] VarianceTable =
    {
        (0.40f, 1),
        (0.60f, 2),
        (0.75f, 3),
        (0.85f, 4),
        (0.90f, 6),
        (0.95f, 10),
        (1.00f, 20)
    };

    // --- Dungeon Reward ---

    public static BattleRewardResult CalculateDungeonReward(BattleContext context)
    {
        float baseReward = context.BaseReward;

        // Layer scaling: +10% per layer number
        float layerBonus = baseReward * (context.LayerIndex * 0.10f);

        // Boss multiplier: 2x total for boss layers
        float bossMultiplier = context.IsBossLayer ? 2.0f : 1.0f;

        // Upgrade bonus: flat addition from purchased upgrades
        float upgradeBonus = UpgradeManager.Instance.GetFlat(ModifierType.BattleRewardFlat);

        float totalReward = (baseReward + layerBonus + upgradeBonus) * bossMultiplier;

        return new BattleRewardResult
        {
            FinalReward = Mathf.FloorToInt(totalReward),
            BaseReward = Mathf.FloorToInt(baseReward),
            LuckMultiplier = 1,
            WasLucky = false
        };
    }

    // --- Random Encounter Reward ---

    public static BattleRewardResult CalculateRandomEncounterReward(BattleContext context)
    {
        float baseReward = context.BaseReward;

        // Upgrade bonus
        float upgradeBonus = UpgradeManager.Instance.GetFlat(ModifierType.BattleRewardFlat);

        // Roll RNG variance
        int luckMultiplier = RollVariance();

        float totalReward = (baseReward + upgradeBonus) * luckMultiplier;

        return new BattleRewardResult
        {
            FinalReward = Mathf.FloorToInt(totalReward),
            BaseReward = Mathf.FloorToInt(baseReward),
            LuckMultiplier = luckMultiplier,
            WasLucky = luckMultiplier > 1
        };
    }

    // --- Unified Entry Point ---

    public static BattleRewardResult CalculateReward(BattleContext context)
    {
        if (context.Source == BattleSource.Dungeon)
            return CalculateDungeonReward(context);
        else
            return CalculateRandomEncounterReward(context);
    }

    // --- RNG Roll ---

    private static int RollVariance()
    {
        float roll = GD.Randf();

        for (int i = 0; i < VarianceTable.Length; i++)
        {
            if (roll <= VarianceTable[i].cumulative)
                return VarianceTable[i].multiplier;
        }

        // Fallback (should never reach here)
        return 1;
    }
}

// --- Reward Result ---

public class BattleRewardResult
{
    public int FinalReward { get; set; }
    public int BaseReward { get; set; }
    public int LuckMultiplier { get; set; }
    public bool WasLucky { get; set; }
}

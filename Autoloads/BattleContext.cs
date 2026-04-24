public enum BattleSource
{
    Dungeon,
    RandomEncounter
}

public class BattleContext
{
    // --- Battle Source ---

    public BattleSource Source { get; private set; }

    // --- Enemy ---

    public EnemyData Enemy { get; private set; }

    // --- Dungeon Context (null if random encounter) ---

    public DungeonData Dungeon { get; private set; }
    public int LayerIndex { get; private set; }
    public bool IsBossLayer { get; private set; }
    public bool IsMiniBossLayer { get; private set; }

    // --- Reward ---

    public float BaseReward { get; private set; }

    // --- Display ---

    public string HeaderText { get; private set; }
    public string PoolName { get; private set; }

    // --- Factory: Dungeon Battle ---

    public static BattleContext CreateDungeonBattle(
        DungeonData dungeon, int layerIndex, DungeonLayer layer)
    {
        var ctx = new BattleContext();
        ctx.Source = BattleSource.Dungeon;
        ctx.Enemy = layer.Enemy;
        ctx.Dungeon = dungeon;
        ctx.LayerIndex = layerIndex;
        ctx.IsBossLayer = layer.IsBossLayer;
        ctx.IsMiniBossLayer = layer.IsMiniBossLayer;
        ctx.BaseReward = layer.BaseKleosReward;
        ctx.HeaderText = $"{dungeon.DungeonName} - Layer {layerIndex + 1}";
        ctx.PoolName = "";
        return ctx;
    }

    // --- Factory: Random Encounter ---

    public static BattleContext CreateRandomEncounter(
        EnemyData enemy, string poolName)
    {
        var ctx = new BattleContext();
        ctx.Source = BattleSource.RandomEncounter;
        ctx.Enemy = enemy;
        ctx.Dungeon = null;
        ctx.LayerIndex = -1;
        ctx.IsBossLayer = false;
        ctx.IsMiniBossLayer = false;
        ctx.BaseReward = enemy.KleosReward;
        ctx.HeaderText = "Random Encounter";
        ctx.PoolName = poolName;
        return ctx;
    }
}

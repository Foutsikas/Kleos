using Godot;
using Godot.Collections;

public partial class RandomEncounterManager : Node
{
    public static RandomEncounterManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void EncounterTriggeredEventHandler(EnemyData enemy);

    // --- Config ---
    [Export] public Array EncounterPools { get; set; } = new();

    // --- State ---
    private int clickAccumulator = 0;
    private int clickThreshold = 0;
    private bool poolsDirty = true;
    private Array<EncounterPool> activePools = new();

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;

        RollNewThreshold();

        // Refresh active pools when a dungeon is completed
        DungeonManager.Instance.DungeonCompleted += OnDungeonCompleted;

        GD.Print("[RandomEncounterManager] Ready.");
    }

    // --- Click Tracking ---

    public void OnDeedClicked()
    {
        clickAccumulator++;
        if (clickAccumulator >= clickThreshold)
        {
            TryTriggerEncounter();
            clickAccumulator = 0;
            RollNewThreshold();
        }
    }

    private void RollNewThreshold()
    {
        clickThreshold = GD.RandRange(10, 30);
    }

    // --- Encounter Trigger ---

    private void TryTriggerEncounter()
    {
        RefreshPoolsIfDirty();

        if (activePools.Count == 0) return;

        EncounterPool pool = PickRandomPool();
        if (pool == null) return;

        EnemyData enemy = PickRandomEnemy(pool);
        if (enemy == null) return;

        GD.Print($"[RandomEncounterManager] Encounter triggered: {enemy.EnemyName}.");
        EmitSignal(SignalName.EncounterTriggered, enemy);
    }

    // --- Pool Selection ---

    private void RefreshPoolsIfDirty()
    {
        if (!poolsDirty) return;

        activePools.Clear();

        for (int i = 0; i < EncounterPools.Count; i++)
        {
            var pool = EncounterPools[i].As<EncounterPool>();
            if (pool == null) continue;

            bool active = pool.RequiredDungeon == null
                || DungeonManager.Instance.IsDungeonCompleted(pool.RequiredDungeon.DungeonId);

            if (active)
                activePools.Add(pool);
        }

        poolsDirty = false;
        GD.Print($"[RandomEncounterManager] Active pools: {activePools.Count}.");
    }

    private EncounterPool PickRandomPool()
    {
        if (activePools.Count == 0) return null;
        return activePools[GD.RandRange(0, activePools.Count - 1)];
    }

    private EnemyData PickRandomEnemy(EncounterPool pool)
    {
        if (pool.Entries.Count == 0) return null;

        // Weighted selection
        float totalWeight = 0f;
        for (int i = 0; i < pool.Entries.Count; i++)
        {
            var entry = pool.GetEntry(i);
            if (entry != null) totalWeight += entry.Weight;
        }

        float roll = GD.Randf() * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < pool.Entries.Count; i++)
        {
            var entry = pool.GetEntry(i);
            if (entry == null) continue;
            cumulative += entry.Weight;
            if (roll <= cumulative)
                return entry.Enemy;
        }

        // Fallback to last entry
        return pool.GetEntry(pool.Entries.Count - 1)?.Enemy;
    }

    // --- Event Handlers ---

    private void OnDungeonCompleted(string dungeonId)
    {
        poolsDirty = true;
        GD.Print($"[RandomEncounterManager] Pools marked dirty after completing: {dungeonId}.");
    }
}
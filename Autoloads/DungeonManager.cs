using Godot;
using Godot.Collections;

public partial class DungeonManager : Node
{
    public static DungeonManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void DungeonCompletedEventHandler(string dungeonId);
    [Signal] public delegate void LayerClearedEventHandler(string dungeonId, int layerIndex);

    // --- Config ---
    public Array DungeonConfigs { get; private set; } = new();

    private void LoadConfigs()
    {
        DungeonConfigs = ResourceScanner.LoadAll<DungeonData>("res://Resources/Dungeons/");
        SortByProgression();
    }

    private void SortByProgression()
    {
        // Dungeons with no RequiredDungeon come first (Forest).
        // Then each dungeon whose RequiredDungeon is the previous one.

        var unsorted = new System.Collections.Generic.List<DungeonData>();
        for (int i = 0; i < DungeonConfigs.Count; i++)
        {
            var dungeon = DungeonConfigs[i].As<DungeonData>();
            if (dungeon != null)
                unsorted.Add(dungeon);
        }

        var sorted = new System.Collections.Generic.List<DungeonData>();
        var remaining = new System.Collections.Generic.List<DungeonData>(unsorted);

        // First pass: find dungeons with no prerequisite
        for (int i = remaining.Count - 1; i >= 0; i--)
        {
            if (remaining[i].RequiredDungeon == null)
            {
                sorted.Add(remaining[i]);
                remaining.RemoveAt(i);
            }
        }

        // Chain: find the dungeon whose RequiredDungeon is the last sorted one
        int safety = 0;
        while (remaining.Count > 0 && safety < 20)
        {
            safety++;
            string lastId = sorted[sorted.Count - 1].DungeonId;
            bool found = false;
            for (int i = 0; i < remaining.Count; i++)
            {
                if (remaining[i].RequiredDungeon != null
                    && remaining[i].RequiredDungeon.DungeonId == lastId)
                {
                    sorted.Add(remaining[i]);
                    remaining.RemoveAt(i);
                    found = true;
                    break;
                }
            }
            if (!found) break;
        }

        sorted.AddRange(remaining);

        DungeonConfigs.Clear();
        foreach (var dungeon in sorted)
            DungeonConfigs.Add(dungeon);
    }

    // --- State ---
    private Dictionary<string, int> dungeonProgress = new();
    private Dictionary<string, bool> completedDungeons = new();

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[DungeonManager] Ready.");

        LoadConfigs();
        GD.Print($"[DungeonManager] Loaded {DungeonConfigs.Count} dungeon configs.");
    }

    // --- Dungeon Queries ---

    public bool IsDungeonUnlocked(string dungeonId)
    {
        DungeonData dungeon = GetDungeonById(dungeonId);
        if (dungeon == null) return false;

        if (dungeon.KleosRequirement > 0)
        {
            if (KleosManager.Instance.CurrentKleos < dungeon.KleosRequirement)
                return false;
        }

        if (dungeon.RequiredDungeon != null)
        {
            if (!IsDungeonCompleted(dungeon.RequiredDungeon.DungeonId))
                return false;
        }

        return true;
    }

    public bool IsDungeonCompleted(string dungeonId)
    {
        return completedDungeons.TryGetValue(dungeonId, out bool completed) && completed;
    }

    public int GetHighestClearedLayer(string dungeonId)
    {
        return dungeonProgress.TryGetValue(dungeonId, out int layer) ? layer : -1;
    }

    public int GetNextLayer(string dungeonId)
    {
        return GetHighestClearedLayer(dungeonId) + 1;
    }

    public bool CanAccessLayer(string dungeonId, int layerIndex)
    {
        if (!IsDungeonUnlocked(dungeonId)) return false;
        int highestCleared = GetHighestClearedLayer(dungeonId);
        return layerIndex <= highestCleared + 1;
    }

    public DungeonData GetDungeonById(string dungeonId)
    {
        for (int i = 0; i < DungeonConfigs.Count; i++)
        {
            var dungeon = DungeonConfigs[i].As<DungeonData>();
            if (dungeon != null && dungeon.DungeonId == dungeonId)
                return dungeon;
        }
        return null;
    }

    // --- Layer Completion ---

    public void OnLayerCleared(string dungeonId, int layerIndex)
    {
        int current = GetHighestClearedLayer(dungeonId);
        if (layerIndex > current)
            dungeonProgress[dungeonId] = layerIndex;

        GD.Print($"[DungeonManager] Layer {layerIndex} cleared in {dungeonId}.");

        CheckDungeonCompletion(dungeonId);
        EmitSignal(SignalName.LayerCleared, dungeonId, layerIndex);
    }

    private void CheckDungeonCompletion(string dungeonId)
    {
        DungeonData dungeon = GetDungeonById(dungeonId);
        if (dungeon == null) return;

        int totalLayers = dungeon.Layers.Count;
        int highestCleared = GetHighestClearedLayer(dungeonId);

        if (highestCleared >= totalLayers - 1 && !IsDungeonCompleted(dungeonId))
        {
            completedDungeons[dungeonId] = true;
            EmitSignal(SignalName.DungeonCompleted, dungeonId);
            GD.Print($"[DungeonManager] Dungeon completed: {dungeonId}.");
        }
    }

    // --- Save / Load ---

    public DungeonSaveData GetSaveData()
    {
        var saveData = new DungeonSaveData();

        foreach (var kvp in dungeonProgress)
            saveData.DungeonProgress[kvp.Key] = kvp.Value;

        foreach (var kvp in completedDungeons)
            if (kvp.Value)
                saveData.CompletedDungeons.Add(kvp.Key);

        return saveData;
    }

    public void LoadFromSaveData(DungeonSaveData data)
    {
        dungeonProgress.Clear();
        completedDungeons.Clear();

        foreach (var kvp in data.DungeonProgress)
            dungeonProgress[kvp.Key] = kvp.Value;

        foreach (var id in data.CompletedDungeons)
            completedDungeons[id] = true;

        GD.Print($"[DungeonManager] Loaded progress for {dungeonProgress.Count} dungeons.");
    }

    /// DEV API
    public void ForceCompleteDungeon(string dungeonId)
    {
        DungeonData dungeon = GetDungeonById(dungeonId);
        if (dungeon == null)
        {
            GD.PrintErr($"[DungeonManager] ForceComplete failed: {dungeonId} not found.");
            return;
        }

        int lastLayer = dungeon.Layers.Count - 1;

        // Set progress directly
        dungeonProgress[dungeonId] = lastLayer;

        // Mark as completed
        completedDungeons[dungeonId] = true;

        // Emit signals in correct order
        EmitSignal(SignalName.DungeonCompleted, dungeonId);
        EmitSignal(SignalName.LayerCleared, dungeonId, lastLayer);

        GD.Print($"[DungeonManager] Force completed: {dungeonId}");
    }
}
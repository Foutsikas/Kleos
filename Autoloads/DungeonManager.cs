using Godot;

public partial class DungeonManager : Node
{
    public static DungeonManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void DungeonCompletedEventHandler(string dungeonId);
    [Signal] public delegate void LayerClearedEventHandler(string dungeonId, int layerIndex);

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
    }

    public bool IsDungeonCompleted(string dungeonId)
    {
        return false;
    }
}
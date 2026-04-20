using Godot;

public partial class RandomEncounterManager : Node
{
    public static RandomEncounterManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void EncounterTriggeredEventHandler();

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[RandomEncounterManager] Ready.");
    }
}
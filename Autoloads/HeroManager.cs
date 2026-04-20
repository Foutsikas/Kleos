using Godot;

public partial class HeroManager : Node
{
    public static HeroManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void LevelUpEventHandler(int newLevel);
    [Signal] public delegate void StatsChangedEventHandler();

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[HeroManager] Ready.");
    }
}
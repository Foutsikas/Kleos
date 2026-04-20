using Godot;

public partial class UpgradeManager : Node
{
    public static UpgradeManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void UpgradePurchasedEventHandler(string upgradeId);

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[UpgradeManager] Ready.");
    }
}
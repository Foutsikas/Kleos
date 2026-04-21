using Godot;

public partial class UpgradeManager : Node
{
    public static UpgradeManager Instance { get; private set; }

    [Signal] public delegate void UpgradePurchasedEventHandler(string upgradeId);

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

    public float GetFlat(ModifierType type)
    {
        return 0f;
    }
}
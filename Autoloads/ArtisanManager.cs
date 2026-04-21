using Godot;

public partial class ArtisanManager : Node
{
    public static ArtisanManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void ArtisanPurchasedEventHandler(string artisanId);
    [Signal] public delegate void ArtisanUnlockedEventHandler(string artisanId);

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[ArtisanManager] Ready.");
    }

    public int GetOwnedCount(string artisanId)
    {
        return 0;
    }
}
using Godot;

public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    // --- Settings state ---
    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public bool Fullscreen { get; private set; } = false;

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[SettingsManager] Ready.");
    }
}
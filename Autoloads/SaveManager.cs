using Godot;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFilePath = "user://game_save.json";
    private const string BackupFilePath = "user://game_save.backup.json";

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[SaveManager] Ready.");
    }
}
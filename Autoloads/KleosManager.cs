using Godot;

public partial class KleosManager : Node
{
    public static KleosManager Instance { get; private set; }

    // --- Signals ---
    [Signal] public delegate void KleosChangedEventHandler(float amount);
    [Signal] public delegate void KleosGainedEventHandler(float amount);
    [Signal] public delegate void ProductionChangedEventHandler(float amount);
    [Signal] public delegate void DeedContextChangedEventHandler();

    // --- State ---
    private float currentKleos = 0f;
    private float totalKleosPerSecond = 0f;

    public float CurrentKleos => currentKleos;
    public float TotalKleosPerSecond => totalKleosPerSecond;

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[KleosManager] Ready.");
    }
}
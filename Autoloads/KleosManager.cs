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
    private float passiveAccumulator = 0f;

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

    public override void _Process(double delta)
    {
        if (totalKleosPerSecond <= 0f) return;

        passiveAccumulator += totalKleosPerSecond * (float)delta;

        if (passiveAccumulator >= 1f)
        {
            float earned = Mathf.Floor(passiveAccumulator);
            passiveAccumulator -= earned;
            AddKleos(earned);
        }
    }

    // --- Public API ---

    public void DoDeed()
    {
        float damage = CalculateClickDamage();
        AddKleos(damage);
        EmitSignal(SignalName.DeedContextChanged);
    }

    public void AddKleos(float amount)
    {
        if (amount <= 0f) return;
        currentKleos += amount;
        EmitSignal(SignalName.KleosChanged, currentKleos);
        EmitSignal(SignalName.KleosGained, amount);
    }

    public bool SpendKleos(float amount)
    {
        if (amount <= 0f) return false;
        if (currentKleos < amount) return false;
        currentKleos -= amount;
        EmitSignal(SignalName.KleosChanged, currentKleos);
        return true;
    }

    public void RecalculateTotalProduction(float newTotal)
    {
        totalKleosPerSecond = newTotal;
        passiveAccumulator = 0f;
        EmitSignal(SignalName.ProductionChanged, totalKleosPerSecond);
    }

    public void ResetToDefaults()
    {
        currentKleos = 0f;
        totalKleosPerSecond = 0f;
        passiveAccumulator = 0f;
        EmitSignal(SignalName.KleosChanged, currentKleos);
        EmitSignal(SignalName.ProductionChanged, totalKleosPerSecond);
    }

    // --- Save / Load ---

    public KleosSaveData GetSaveData()
    {
        return new KleosSaveData
        {
            CurrentKleos = currentKleos,
            TotalKleosPerSecond = totalKleosPerSecond
        };
    }

    public void LoadFromSaveData(KleosSaveData data)
    {
        currentKleos = data.CurrentKleos;
        totalKleosPerSecond = data.TotalKleosPerSecond;
        passiveAccumulator = 0f;
        EmitSignal(SignalName.KleosChanged, currentKleos);
        EmitSignal(SignalName.ProductionChanged, totalKleosPerSecond);
    }

    // --- Internal ---

    private float CalculateClickDamage()
    {
        float baseDamage = 1f;
        float upgradeBonus = UpgradeManager.Instance.GetFlat(ModifierType.ClickFlat);
        return baseDamage + upgradeBonus;
    }
}
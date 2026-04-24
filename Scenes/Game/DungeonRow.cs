using Godot;

public partial class DungeonRow : PanelContainer
{
    // --- Node References ---
    [Export] public Label DungeonNameLabel { get; set; }
    [Export] public Label ProgressLabel { get; set; }
    [Export] public Label LayerInfoLabel { get; set; }
    [Export] public Button DungeonActionButton { get; set; }

    // --- State ---
    private DungeonData dungeonData;

    // --- Colors ---
    private static readonly Color LockedModulate = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    private static readonly Color CompletedModulate = new Color(0.7f, 0.85f, 0.7f, 1f);
    private static readonly Color NormalModulate = new Color(1f, 1f, 1f, 1f);

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (DungeonActionButton != null)
            DungeonActionButton.Pressed += OnActionPressed;

        DungeonManager.Instance.LayerCleared += OnLayerCleared;
        KleosManager.Instance.KleosChanged += OnKleosChanged;
    }

    public override void _ExitTree()
    {
        DungeonManager.Instance.LayerCleared -= OnLayerCleared;
        KleosManager.Instance.KleosChanged -= OnKleosChanged;
    }

    // --- Setup ---

    public void Setup(DungeonData data)
    {
        dungeonData = data;
        RefreshDisplay();
    }

    // --- Display ---

    private void RefreshDisplay()
    {
        if (dungeonData == null) return;

        string id = dungeonData.DungeonId;
        int totalLayers = dungeonData.Layers.Count;
        int cleared = DungeonManager.Instance.GetHighestClearedLayer(id) + 1;
        bool unlocked = DungeonManager.Instance.IsDungeonUnlocked(id);
        bool completed = DungeonManager.Instance.IsDungeonCompleted(id);

        // Name
        if (DungeonNameLabel != null)
            DungeonNameLabel.Text = dungeonData.DungeonName;

        // Progress
        if (ProgressLabel != null)
            ProgressLabel.Text = $"{cleared} / {totalLayers}";

        // State-specific display
        if (completed)
            ShowCompleted(totalLayers);
        else if (!unlocked)
            ShowLocked();
        else
            ShowAvailable(cleared, totalLayers);
    }

    private void ShowLocked()
    {
        Modulate = LockedModulate;

        if (LayerInfoLabel != null)
        {
            string reason = GetLockReason();
            LayerInfoLabel.Text = reason;
        }

        if (DungeonActionButton != null)
        {
            DungeonActionButton.Text = "Locked";
            DungeonActionButton.Disabled = true;
        }
    }

    private void ShowAvailable(int cleared, int totalLayers)
    {
        Modulate = NormalModulate;

        int nextLayerIndex = cleared;
        DungeonLayer nextLayer = dungeonData.GetLayer(nextLayerIndex);

        if (LayerInfoLabel != null)
        {
            if (nextLayer != null && nextLayer.Enemy != null)
            {
                string prefix = "Next";
                if (nextLayer.IsBossLayer) prefix = "BOSS";
                else if (nextLayer.IsMiniBossLayer) prefix = "Mini-Boss";
                LayerInfoLabel.Text = $"{prefix}: {nextLayer.Enemy.EnemyName}";
            }
            else
            {
                LayerInfoLabel.Text = "Ready to enter";
            }
        }

        if (DungeonActionButton != null)
        {
            DungeonActionButton.Disabled = false;
            DungeonActionButton.Text = cleared == 0 ? "Enter" : $"Continue (Layer {cleared + 1})";
        }
    }

    private void ShowCompleted(int totalLayers)
    {
        Modulate = CompletedModulate;

        if (LayerInfoLabel != null)
            LayerInfoLabel.Text = "All trials conquered.";

        if (DungeonActionButton != null)
        {
            DungeonActionButton.Text = "Completed";
            DungeonActionButton.Disabled = true;
        }
    }

    private string GetLockReason()
    {
        if (dungeonData.RequiredDungeon != null)
        {
            if (!DungeonManager.Instance.IsDungeonCompleted(dungeonData.RequiredDungeon.DungeonId))
                return $"Requires: Clear {dungeonData.RequiredDungeon.DungeonName}";
        }

        if (dungeonData.KleosRequirement > 0)
            return $"Requires: {dungeonData.KleosRequirement:N0} Kleos";

        return "Locked";
    }

    // --- Handlers ---

    private void OnActionPressed()
    {
        if (dungeonData == null) return;
        if (BattleSystem.Instance.IsBattleInProgress()) return;
        int nextLayer = DungeonManager.Instance.GetNextLayer(dungeonData.DungeonId);
        int totalLayers = dungeonData.Layers.Count;

        // Do not start battle if dungeon is already completed
        if (nextLayer > totalLayers) return;

        BattleSystem.Instance.StartDungeonBattle(dungeonData, nextLayer);
    }

    private void OnLayerCleared(string dungeonId, int layerIndex)
    {
        if (dungeonData == null) return;
        if (dungeonData.DungeonId == dungeonId)
            RefreshDisplay();
    }

    private void OnKleosChanged(float amount)
    {
        // Refresh in case a kleos requirement was just met
        if (dungeonData == null) return;
        if (!DungeonManager.Instance.IsDungeonUnlocked(dungeonData.DungeonId))
            RefreshDisplay();
    }
}
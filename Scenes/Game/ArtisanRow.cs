using Godot;

public partial class ArtisanRow : PanelContainer
{
	// --- Node References ---
	[Export] public Label ArtisanNameLabel { get; set; }
	[Export] public Label ArtisanKPSLabel { get; set; }
	[Export] public Label ArtisanCostLabel { get; set; }
	[Export] public Button ArtisanBuyButton { get; set; }
	[Export] public Label ArtisanOwnedLabel { get; set; }

	// --- Data ---
	private ArtisanData artisanData;

	// --- Lifecycle ---

	public override void _Ready()
	{
		if (ArtisanBuyButton != null)
			ArtisanBuyButton.Pressed += OnBuyPressed;

		// Refresh affordability when kleos changes
		KleosManager.Instance.KleosChanged += OnKleosChanged;
	}

	public override void _ExitTree()
	{
		KleosManager.Instance.KleosChanged -= OnKleosChanged;
	}

	// --- Setup ---

	public void Setup(ArtisanData data)
	{
		artisanData = data;
		RefreshDisplay();
	}

	// --- Display ---

	private void RefreshDisplay()
	{
		if (artisanData == null) return;

		int owned = ArtisanManager.Instance.GetOwnedCount(artisanData.ArtisanId);
		float cost = ArtisanManager.Instance.GetCurrentCost(artisanData);
		bool canAfford = ArtisanManager.Instance.CanPurchase(artisanData);

		if (ArtisanNameLabel != null)
			ArtisanNameLabel.Text = artisanData.ArtisanName;

		if (ArtisanKPSLabel != null)
			ArtisanKPSLabel.Text = $"{artisanData.KleosPerSecond:F1} K/s each";

		if (ArtisanCostLabel != null)
			ArtisanCostLabel.Text = $"{Mathf.Floor(cost):N0}";

		if (ArtisanOwnedLabel != null)
			ArtisanOwnedLabel.Text = $"Owned: {owned}";

		if (ArtisanBuyButton != null)
			ArtisanBuyButton.Disabled = !canAfford;
	}

	// --- Handlers ---

	private void OnBuyPressed()
	{
		if (artisanData == null) return;
		if (ArtisanManager.Instance.PurchaseArtisan(artisanData))
			RefreshDisplay();
	}

	private void OnKleosChanged(float amount)
	{
		if (artisanData == null) return;
		bool canAfford = ArtisanManager.Instance.CanPurchase(artisanData);
		if (ArtisanBuyButton != null)
			ArtisanBuyButton.Disabled = !canAfford;
		if (ArtisanCostLabel != null)
			ArtisanCostLabel.Text = $"{Mathf.Floor(ArtisanManager.Instance.GetCurrentCost(artisanData)):N0}";
	}
}

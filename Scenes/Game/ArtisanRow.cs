using Godot;

public partial class ArtisanRow : PanelContainer
{
	// --- Node References ---
	[Export] public Label ArtisanNameLabel { get; set; }
	[Export] public Label ArtisanKPSLabel { get; set; }
	[Export] public Label ArtisanCostLabel { get; set; }
	[Export] public Button ArtisanBuyButton { get; set; }
	[Export] public Label ArtisanOwnedLabel { get; set; }

	// --- State ---
	private ArtisanData artisanData;
	private bool isLocked;

	// --- Colors ---
	private static readonly Color LockedModulate = new Color(0.5f, 0.5f, 0.5f, 0.6f);
	private static readonly Color UnlockedModulate = new Color(1f, 1f, 1f, 1f);

	// --- Lifecycle ---

	public override void _Ready()
	{
		if (ArtisanBuyButton != null)
			ArtisanBuyButton.Pressed += OnBuyPressed;

		KleosManager.Instance.KleosChanged += OnKleosChanged;
		ArtisanManager.Instance.ArtisanPurchased += OnAnyArtisanPurchased;
	}

	public override void _ExitTree()
	{
		KleosManager.Instance.KleosChanged -= OnKleosChanged;
		ArtisanManager.Instance.ArtisanPurchased -= OnAnyArtisanPurchased;
	}

	// --- Setup ---

	public void Setup(ArtisanData data)
	{
		artisanData = data;

		if (ArtisanManager.Instance.IsArtisanUnlocked(data))
			SetUnlocked();
		else
			SetLocked(GetLockReason(data));
	}

	// --- Lock State ---

	private void SetLocked(string reason)
	{
		isLocked = true;
		Modulate = LockedModulate;

		if (ArtisanNameLabel != null)
			ArtisanNameLabel.Text = artisanData.ArtisanName;

		if (ArtisanKPSLabel != null)
			ArtisanKPSLabel.Text = reason;

		if (ArtisanCostLabel != null)
			ArtisanCostLabel.Visible = false;

		if (ArtisanOwnedLabel != null)
			ArtisanOwnedLabel.Visible = false;

		if (ArtisanBuyButton != null)
		{
			ArtisanBuyButton.Disabled = true;
			ArtisanBuyButton.Text = "Locked";
		}
	}

	private void SetUnlocked()
	{
		isLocked = false;
		Modulate = UnlockedModulate;

		if (ArtisanCostLabel != null)
			ArtisanCostLabel.Visible = true;

		if (ArtisanOwnedLabel != null)
			ArtisanOwnedLabel.Visible = true;

		if (ArtisanBuyButton != null)
			ArtisanBuyButton.Text = "Hire";

		RefreshDisplay();
	}

	private string GetLockReason(ArtisanData data)
	{
		// Match the unlock conditions from ArtisanManager
		// Scribe: always unlocked
		// Bard: 5 Scribes, Potter: 5 Bards, Sculptor: 5 Potters,
		// Playwright: 5 Sculptors, Historian: 3 Playwrights
		if (string.IsNullOrEmpty(data.RequiredArtisanId))
			return "";

		string requiredName = GetArtisanDisplayName(data.RequiredArtisanId);
		return $"Requires {data.RequiredArtisanCount} {requiredName}s";
	}

	private string GetArtisanDisplayName(string artisanId)
	{
		foreach (var config in ArtisanManager.Instance.ArtisanConfigs)
		{
			var artisan = config.As<ArtisanData>();
			if (artisan != null && artisan.ArtisanId == artisanId)
				return artisan.ArtisanName;
		}
		return artisanId;
	}

	// --- Display ---

	private void RefreshDisplay()
	{
		if (artisanData == null || isLocked) return;

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
		if (artisanData == null || isLocked) return;
		if (ArtisanManager.Instance.PurchaseArtisan(artisanData))
			RefreshDisplay();
	}

	private void OnKleosChanged(float amount)
	{
		if (artisanData == null || isLocked) return;

		bool canAfford = ArtisanManager.Instance.CanPurchase(artisanData);
		if (ArtisanBuyButton != null)
			ArtisanBuyButton.Disabled = !canAfford;

		if (ArtisanCostLabel != null)
		{
			float cost = ArtisanManager.Instance.GetCurrentCost(artisanData);
			ArtisanCostLabel.Text = $"{Mathf.Floor(cost):N0}";
		}
	}

	private void OnAnyArtisanPurchased(string artisanId)
	{
		if (artisanData == null || !isLocked) return;

		// Check if this row should now be unlocked
		if (ArtisanManager.Instance.IsArtisanUnlocked(artisanData))
			SetUnlocked();
	}
}
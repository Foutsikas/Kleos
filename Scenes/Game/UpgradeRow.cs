using Godot;

public partial class UpgradeRow : PanelContainer
{
	// --- Node References ---
	[Export] public Label UpgradeNameLabel { get; set; }
	[Export] public Label UpgradeCostLabel { get; set; }
	[Export] public Label DescriptionLabel { get; set; }
	[Export] public Label LockReasonLabel { get; set; }
	[Export] public Button UpgradeBuyButton { get; set; }

	// --- State ---
	private UpgradeConfig upgradeConfig;

	// --- Visual State Colors ---
	private static readonly Color AffordableModulate = new Color(1f, 1f, 1f, 1f);
	private static readonly Color UnaffordableModulate = new Color(0.7f, 0.7f, 0.7f, 0.85f);
	private static readonly Color PurchasedModulate = new Color(0.7f, 0.85f, 0.65f, 1f);
	private static readonly Color TierLockedModulate = new Color(0.45f, 0.45f, 0.45f, 0.6f);
	private static readonly Color IndividualLockedModulate = new Color(0.55f, 0.45f, 0.35f, 0.7f);

	// --- Lifecycle ---

	public override void _Ready()
	{
		if (UpgradeBuyButton != null)
			UpgradeBuyButton.Pressed += OnBuyPressed;

		KleosManager.Instance.KleosChanged += OnKleosChanged;
		UpgradeManager.Instance.UpgradePurchased += OnAnyUpgradePurchased;
	}

	public override void _ExitTree()
	{
		KleosManager.Instance.KleosChanged -= OnKleosChanged;
		UpgradeManager.Instance.UpgradePurchased -= OnAnyUpgradePurchased;
	}

	// --- Setup ---

	public void Setup(UpgradeConfig config)
	{
		upgradeConfig = config;
		RefreshDisplay();
	}

	// --- Display ---

	private void RefreshDisplay()
	{
		if (upgradeConfig == null) return;

		string id = upgradeConfig.UpgradeId;
		bool purchased = UpgradeManager.Instance.IsUpgradePurchased(id);

		// Name and cost
		if (UpgradeNameLabel != null)
			UpgradeNameLabel.Text = upgradeConfig.UpgradeName;

		if (UpgradeCostLabel != null)
			UpgradeCostLabel.Text = $"{upgradeConfig.Cost:N0}";

		if (DescriptionLabel != null)
			DescriptionLabel.Text = upgradeConfig.Description;

		if (purchased)
		{
			ShowPurchased();
			return;
		}

		// Check tier lock first, then individual lock, then affordability
		if (!IsTierUnlocked())
		{
			ShowTierLocked();
			return;
		}

		if (!IsIndividualLockMet())
		{
			ShowIndividualLocked();
			return;
		}

		bool canAfford = KleosManager.Instance.CurrentKleos >= upgradeConfig.Cost;
		if (canAfford)
			ShowAffordable();
		else
			ShowUnaffordable();
	}

	// --- Visual States ---

	private void ShowAffordable()
	{
		Modulate = AffordableModulate;
		SetLockReason("");
		if (UpgradeBuyButton != null)
		{
			UpgradeBuyButton.Disabled = false;
			UpgradeBuyButton.Text = "Purchase";
		}
	}

	private void ShowUnaffordable()
	{
		Modulate = UnaffordableModulate;
		SetLockReason("");
		if (UpgradeBuyButton != null)
		{
			UpgradeBuyButton.Disabled = true;
			UpgradeBuyButton.Text = "Purchase";
		}
	}

	private void ShowPurchased()
	{
		Modulate = PurchasedModulate;
		SetLockReason("");
		if (UpgradeBuyButton != null)
		{
			UpgradeBuyButton.Disabled = true;
			UpgradeBuyButton.Text = "Purchased";
		}
	}

	private void ShowTierLocked()
	{
		Modulate = TierLockedModulate;

		string reason = "Locked";
		if (upgradeConfig.RequiredDungeon != null)
			reason = $"Clear {upgradeConfig.RequiredDungeon.DungeonName} to unlock";
		SetLockReason(reason);

		if (UpgradeBuyButton != null)
		{
			UpgradeBuyButton.Disabled = true;
			UpgradeBuyButton.Text = "Locked";
		}
	}

	private void ShowIndividualLocked()
	{
		Modulate = IndividualLockedModulate;

		string reason = GetIndividualLockReason();
		SetLockReason(reason);

		if (UpgradeBuyButton != null)
		{
			UpgradeBuyButton.Disabled = true;
			UpgradeBuyButton.Text = "Locked";
		}
	}

	// --- Lock Reason Helpers ---

	private void SetLockReason(string reason)
	{
		if (LockReasonLabel == null) return;

		if (string.IsNullOrEmpty(reason))
		{
			LockReasonLabel.Visible = false;
			LockReasonLabel.Text = "";
		}
		else
		{
			LockReasonLabel.Visible = true;
			LockReasonLabel.Text = reason;
		}
	}

	private bool IsTierUnlocked()
	{
		if (upgradeConfig.RequiredDungeon == null) return true;
		return DungeonManager.Instance.IsDungeonCompleted(
			upgradeConfig.RequiredDungeon.DungeonId);
	}

	private bool IsIndividualLockMet()
	{
		if (upgradeConfig.RequiredHeroLevel > 0)
		{
			if (HeroManager.Instance.GetLevel() < upgradeConfig.RequiredHeroLevel)
				return false;
		}

		if (!string.IsNullOrEmpty(upgradeConfig.RequiredUpgradeId))
		{
			if (!UpgradeManager.Instance.IsUpgradePurchased(upgradeConfig.RequiredUpgradeId))
				return false;
		}

		if (!string.IsNullOrEmpty(upgradeConfig.RequiredArtisanId)
			&& upgradeConfig.RequiredArtisanCount > 0)
		{
			if (ArtisanManager.Instance.GetOwnedCount(upgradeConfig.RequiredArtisanId)
				< upgradeConfig.RequiredArtisanCount)
				return false;
		}

		return true;
	}

	private string GetIndividualLockReason()
	{
		if (upgradeConfig.RequiredHeroLevel > 0
			&& HeroManager.Instance.GetLevel() < upgradeConfig.RequiredHeroLevel)
			return $"Requires Hero Level {upgradeConfig.RequiredHeroLevel}";

		if (!string.IsNullOrEmpty(upgradeConfig.RequiredUpgradeId)
			&& !UpgradeManager.Instance.IsUpgradePurchased(upgradeConfig.RequiredUpgradeId))
			return "Requires prerequisite upgrade";

		if (!string.IsNullOrEmpty(upgradeConfig.RequiredArtisanId)
			&& upgradeConfig.RequiredArtisanCount > 0)
		{
			string name = upgradeConfig.RequiredArtisanId;
			var artisan = ArtisanManager.Instance.GetArtisanById(name);
			if (artisan != null) name = artisan.ArtisanName;
			return $"Requires {upgradeConfig.RequiredArtisanCount} {name}s";
		}

		return "Locked";
	}

	// --- Handlers ---

	private void OnBuyPressed()
	{
		if (upgradeConfig == null) return;
		if (UpgradeManager.Instance.PurchaseUpgrade(upgradeConfig.UpgradeId))
		{
			ArtisanManager.Instance.RecalculateTotalProduction();
			RefreshDisplay();
		}
	}

	private void OnKleosChanged(float amount)
	{
		if (upgradeConfig == null) return;
		if (UpgradeManager.Instance.IsUpgradePurchased(upgradeConfig.UpgradeId)) return;
		RefreshDisplay();
	}

	private void OnAnyUpgradePurchased(string upgradeId)
	{
		if (upgradeConfig == null) return;
		// Refresh in case a prerequisite was just purchased
		RefreshDisplay();
	}
}
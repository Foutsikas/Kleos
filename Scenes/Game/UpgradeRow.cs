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

		if (TryGetIndividualLockReason(out var reason))
		{
			ShowIndividualLocked(reason);
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

		string lockReason  = "Locked";
		if (upgradeConfig.RequiredDungeon != null)
			lockReason  = $"Clear {upgradeConfig.RequiredDungeon.DungeonName} to unlock";
		SetLockReason(lockReason );

		if (UpgradeBuyButton != null)
		{
			UpgradeBuyButton.Disabled = true;
			UpgradeBuyButton.Text = "Locked";
		}
	}

	private void ShowIndividualLocked(string reason)
	{
		Modulate = IndividualLockedModulate;

		string lockReason = GetIndividualLockReason();
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

	private bool TryGetIndividualLockReason(out string reason)
	{
		var hero = HeroManager.Instance;
		var upgrades = UpgradeManager.Instance;
		var artisans = ArtisanManager.Instance;

		if (upgradeConfig.RequiredHeroLevel > 0 &&
			hero.GetLevel() < upgradeConfig.RequiredHeroLevel)
		{
			reason = $"Requires Hero Level {upgradeConfig.RequiredHeroLevel}";
			return true;
		}

		if (!string.IsNullOrEmpty(upgradeConfig.RequiredUpgradeId) &&
			!upgrades.IsUpgradePurchased(upgradeConfig.RequiredUpgradeId))
		{
			reason = "Requires prerequisite upgrade";
			return true;
		}

		if (!string.IsNullOrEmpty(upgradeConfig.RequiredArtisanId) &&
			upgradeConfig.RequiredArtisanCount > 0)
		{
			string name = upgradeConfig.RequiredArtisanId;
			var artisan = artisans.GetArtisanById(name);
			if (artisan != null) name = artisan.ArtisanName;

			reason = $"Requires {upgradeConfig.RequiredArtisanCount} {name}s";
			return true;
		}

		reason = null;
		return false;
	}

	private string GetIndividualLockReason()
	{
		if (upgradeConfig.RequiredHeroLevel > 0 &&
			HeroManager.Instance.GetLevel() < upgradeConfig.RequiredHeroLevel)
		{
			return $"Requires Hero Level {upgradeConfig.RequiredHeroLevel}";
		}

		if (!string.IsNullOrEmpty(upgradeConfig.RequiredUpgradeId) &&
			!UpgradeManager.Instance.IsUpgradePurchased(upgradeConfig.RequiredUpgradeId))
		{
			return $"Requires {upgradeConfig.RequiredUpgradeId}";
		}

		if (!string.IsNullOrEmpty(upgradeConfig.RequiredArtisanId) &&
			upgradeConfig.RequiredArtisanCount > 0)
		{
			int owned = ArtisanManager.Instance.GetOwnedCount(upgradeConfig.RequiredArtisanId);
			if (owned < upgradeConfig.RequiredArtisanCount)
			{
				var artisan = ArtisanManager.Instance.GetArtisanById(upgradeConfig.RequiredArtisanId);
				string name = artisan != null ? artisan.ArtisanName : upgradeConfig.RequiredArtisanId;

				string suffix = upgradeConfig.RequiredArtisanCount == 1 ? "" : "s";
				return $"Requires {upgradeConfig.RequiredArtisanCount} {name}{suffix}";
			}
		}

		return "Locked";
	}

	// --- Handlers ---

	private void OnBuyPressed()
	{
		if (upgradeConfig == null) return;
		if (UpgradeManager.Instance.PurchaseUpgrade(upgradeConfig.UpgradeId))
		{
			RefreshDisplay();
		}
	}

	private void OnKleosChanged(float amount)
	{
		if (upgradeConfig == null) return;
		if (UpgradeManager.Instance.IsUpgradePurchased(upgradeConfig.UpgradeId)) return;

		// Only care if affordability could change
		bool canAfford = KleosManager.Instance.CurrentKleos >= upgradeConfig.Cost;
		if (UpgradeBuyButton.Disabled == canAfford) // state mismatch
			RefreshDisplay();
	}

	private void OnAnyUpgradePurchased(string upgradeId)
	{
		if (upgradeConfig == null) return;

		// Only refresh if this upgrade depends on the purchased one
		if (upgradeConfig.RequiredUpgradeId == upgradeId ||
			upgradeConfig.UpgradeId == upgradeId)
		{
			RefreshDisplay();
		}
	}
}
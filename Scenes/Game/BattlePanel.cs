using Godot;
using System;
using System.Collections.Generic;

public partial class BattlePanel : Control
{
	// -------------------------------------------------------------------------
	// Exports -- Combat Display
	// -------------------------------------------------------------------------

	[Export] public Label EncounterHeaderLabel { get; set; }

	// Hero side (bottom-left)
	[Export] public Label HeroNameLabel { get; set; }
	[Export] public Label HeroLevelLabel { get; set; }
	[Export] public TextureRect HeroPortrait { get; set; }
	[Export] public ProgressBar HeroHPBar { get; set; }
	[Export] public Label HeroHPText { get; set; }

	// Enemy side (top-right)
	[Export] public Label EnemyNameLabel { get; set; }
	[Export] public TextureRect EnemyPortrait { get; set; }
	[Export] public ProgressBar EnemyHPBar { get; set; }
	[Export] public Label EnemyHPText { get; set; }

	// Battle log (4 visible lines)
	[Export] public Label LogLine1 { get; set; }
	[Export] public Label LogLine2 { get; set; }
	[Export] public Label LogLine3 { get; set; }
	[Export] public Label LogLine4 { get; set; }

	// Speed toggle
	[Export] public Button SpeedToggleButton { get; set; }

	// Background
	[Export] public ColorRect BattleBackground { get; set; }

	// -------------------------------------------------------------------------
	// Exports -- Result Screen
	// -------------------------------------------------------------------------

	[Export] public Control ResultOverlay { get; set; }
	[Export] public Label ResultSubtitleLabel { get; set; }
	[Export] public Label ResultTitleLabel { get; set; }
	[Export] public Label ResultFlavorLabel { get; set; }
	[Export] public Label ResultRewardLabel { get; set; }
	[Export] public Label ResultLuckLabel { get; set; }
	[Export] public Label ResultSummaryLabel { get; set; }
	[Export] public Label ResultConsolationLabel { get; set; }
	[Export] public Button ResultActionButton { get; set; }
	[Export] public Button ViewBattleLogButton { get; set; }

	// -------------------------------------------------------------------------
	// Exports -- Post-Combat Log
	// -------------------------------------------------------------------------

	[Export] public Control PostCombatLogOverlay { get; set; }
	[Export] public ScrollContainer PostCombatLogScroll { get; set; }
	[Export] public VBoxContainer PostCombatLogList { get; set; }
	[Export] public Button BackToResultsButton { get; set; }

	// -------------------------------------------------------------------------
	// Exports -- Combat Area
	// -------------------------------------------------------------------------

	[Export] public Control CombatArea { get; set; }

	// -------------------------------------------------------------------------
	// Constants -- Colors
	// -------------------------------------------------------------------------

	private static readonly Color HeroActionColor = new Color("C87840");
	private static readonly Color EnemyActionColor = new Color("C84030");
	private static readonly Color CritHighlightColor = new Color("FFD700");
	private static readonly Color DodgeColor = new Color("8A9AAA");
	private static readonly Color VictoryTitleColor = new Color("FFD700");
	private static readonly Color DefeatTitleColor = new Color("8B0000");
	private static readonly Color HeroHPBarColor = new Color(0.29f, 0.48f, 0.13f, 1f);
	private static readonly Color EnemyHPBarColor = new Color(0.54f, 0.16f, 0.10f, 1f);

	// Background tints per theme
	private static readonly Color ForestTheme = new Color(0.12f, 0.18f, 0.10f, 1f);
	private static readonly Color RoadTheme = new Color(0.20f, 0.17f, 0.12f, 1f);
	private static readonly Color CoastTheme = new Color(0.10f, 0.15f, 0.22f, 1f);
	private static readonly Color DefaultTheme = new Color(0.15f, 0.13f, 0.11f, 1f);

	// Log line alpha values (newest to oldest)
	private static readonly float[] LogAlpha = { 1.0f, 0.80f, 0.60f, 0.40f };

	// -------------------------------------------------------------------------
	// Constants -- Animation
	// -------------------------------------------------------------------------

	// How far a portrait nudges on attack (pixels)
	private const float AttackNudgeDistance = 20.0f;
	// Duration of the nudge out and back (seconds each)
	private const float NudgeDuration = 0.12f;

	// How far a portrait shakes on taking damage (pixels)
	private const float ShakeDistance = 6.0f;
	// Duration of a single shake oscillation (seconds)
	private const float ShakeDuration = 0.06f;
	// Number of shake oscillations
	private const int ShakeCount = 3;

	// HP bar tween duration (seconds)
	private const float HPBarTweenDuration = 0.3f;

	// Result screen fade-in duration (seconds)
	private const float ResultFadeDuration = 0.4f;

	// Panel fade duration (seconds)
	private const float PanelFadeDuration = 0.25f;

	// -------------------------------------------------------------------------
	// State
	// -------------------------------------------------------------------------

	private BattleContext currentContext;
	private List<BattleLogEntry> fullBattleLog = new List<BattleLogEntry>();
	private List<string> logLineHistory = new List<string>();
	private List<Color> logColorHistory = new List<Color>();
	private List<bool> logIsHeroAction = new List<bool>();
	private int currentSpeedIndex = 0;
	private float[] speedMultipliers = { 1.0f, 2.0f, 4.0f };
	private BattleResult storedResult;
	private bool isCombatActive = false;

	// Original positions for portrait animation reset
	private Vector2 heroPortraitOrigin;
	private Vector2 enemyPortraitOrigin;
	private bool originsStored = false;

	// -------------------------------------------------------------------------
	// Lifecycle
	// -------------------------------------------------------------------------

	public override void _Ready()
	{
		Visible = false;

		if (ResultActionButton != null)
			ResultActionButton.Pressed += OnResultActionPressed;

		if (ViewBattleLogButton != null)
			ViewBattleLogButton.Pressed += OnViewBattleLogPressed;

		if (BackToResultsButton != null)
			BackToResultsButton.Pressed += OnBackToResultsPressed;

		if (SpeedToggleButton != null)
			SpeedToggleButton.Pressed += OnSpeedTogglePressed;

		// Subscribe to BattleSystem events
		BattleSystem.Instance.BattleStarted += OnBattleStarted;
		BattleSystem.Instance.HeroAttackOccurred += OnHeroAttack;
		BattleSystem.Instance.EnemyAttackOccurred += OnEnemyAttack;
		BattleSystem.Instance.RoundStarted += OnRoundStarted;
		BattleSystem.Instance.BattleEnded += OnBattleEnded;
	}

	public override void _ExitTree()
	{
		if (BattleSystem.Instance != null)
		{
			BattleSystem.Instance.BattleStarted -= OnBattleStarted;
			BattleSystem.Instance.HeroAttackOccurred -= OnHeroAttack;
			BattleSystem.Instance.EnemyAttackOccurred -= OnEnemyAttack;
			BattleSystem.Instance.RoundStarted -= OnRoundStarted;
			BattleSystem.Instance.BattleEnded -= OnBattleEnded;
		}
	}

	// -------------------------------------------------------------------------
	// Battle Start
	// -------------------------------------------------------------------------

	private void OnBattleStarted(BattleContext context)
	{
		currentContext = context;
		fullBattleLog.Clear();
		logLineHistory.Clear();
		logColorHistory.Clear();
		logIsHeroAction.Clear();
		storedResult = null;
		isCombatActive = true;
		currentSpeedIndex = 0;

		// Show panel with fade
		ShowCombatArea();
		HideResultOverlay();
		HidePostCombatLog();
		FadePanelIn();

		// Store portrait origins for animation
		StorePortraitOrigins();

		// Set header
		if (EncounterHeaderLabel != null)
			EncounterHeaderLabel.Text = context.HeaderText.ToUpper();

		// Set background theme
		ApplyBackgroundTheme(context);

		// Set up hero display
		SetupHeroDisplay();

		// Set up enemy display
		SetupEnemyDisplay(context.Enemy);

		// Clear log lines
		ClearLogLines();

		// Set up speed toggle visibility
		RefreshSpeedToggle();

		// Show encounter flavor text as first log entry
		ShowEncounterFlavor(context.Enemy);
	}

	// -------------------------------------------------------------------------
	// Portrait Origin Storage
	// -------------------------------------------------------------------------

	private void StorePortraitOrigins()
	{
		if (HeroPortrait != null && !originsStored)
		{
			heroPortraitOrigin = HeroPortrait.Position;
		}
		if (EnemyPortrait != null && !originsStored)
		{
			enemyPortraitOrigin = EnemyPortrait.Position;
		}
		originsStored = true;
	}

	private void ResetPortraitPositions()
	{
		if (HeroPortrait != null)
			HeroPortrait.Position = heroPortraitOrigin;
		if (EnemyPortrait != null)
			EnemyPortrait.Position = enemyPortraitOrigin;
	}

	// -------------------------------------------------------------------------
	// Combat Display Setup
	// -------------------------------------------------------------------------

	private void SetupHeroDisplay()
	{
		if (HeroNameLabel != null)
			HeroNameLabel.Text = "Hero of Hellas";

		if (HeroLevelLabel != null)
			HeroLevelLabel.Text = $"Lv. {HeroManager.Instance.GetLevel()}";

		float maxHP = HeroManager.Instance.GetMaxHP();
		float currentHP = HeroManager.Instance.GetCurrentHP();

		if (HeroHPBar != null)
		{
			HeroHPBar.MaxValue = maxHP;
			HeroHPBar.Value = currentHP;
			SetProgressBarColor(HeroHPBar, HeroHPBarColor);
		}

		if (HeroHPText != null)
			HeroHPText.Text = $"{currentHP:F0} / {maxHP:F0}";
	}

	private void SetupEnemyDisplay(EnemyData enemy)
	{
		if (EnemyNameLabel != null)
			EnemyNameLabel.Text = enemy.EnemyName;

		if (EnemyPortrait != null && enemy.EnemySprite != null)
			EnemyPortrait.Texture = enemy.EnemySprite;

		if (EnemyHPBar != null)
		{
			EnemyHPBar.MaxValue = enemy.Health;
			EnemyHPBar.Value = enemy.Health;
			SetProgressBarColor(EnemyHPBar, EnemyHPBarColor);
		}

		if (EnemyHPText != null)
			EnemyHPText.Text = $"{enemy.Health:F0} / {enemy.Health:F0}";
	}

	private void ApplyBackgroundTheme(BattleContext context)
	{
		if (BattleBackground == null) return;

		Color theme = DefaultTheme;

		if (context.Source == BattleSource.Dungeon && context.Dungeon != null)
		{
			theme = GetThemeForDungeon(context.Dungeon.DungeonId);
		}
		else if (context.Source == BattleSource.RandomEncounter)
		{
			theme = GetThemeForPool(context.PoolName);
		}

		BattleBackground.Color = theme;
	}

	private Color GetThemeForDungeon(string dungeonId)
	{
		string lower = dungeonId.ToLower();

		if (lower.Contains("forest"))
			return ForestTheme;
		if (lower.Contains("brigand") || lower.Contains("road") || lower.Contains("pass"))
			return RoadTheme;
		if (lower.Contains("coast") || lower.Contains("cave") || lower.Contains("shore"))
			return CoastTheme;

		return DefaultTheme;
	}

	private Color GetThemeForPool(string poolName)
	{
		string lower = poolName.ToLower();

		if (lower.Contains("forest"))
			return ForestTheme;
		if (lower.Contains("brigand") || lower.Contains("road"))
			return RoadTheme;
		if (lower.Contains("coast") || lower.Contains("shore"))
			return CoastTheme;

		return DefaultTheme;
	}

	// -------------------------------------------------------------------------
	// Combat Events
	// -------------------------------------------------------------------------

	private void OnRoundStarted(int roundNumber)
	{
		// Reserved for future use (round separators, etc.)
	}

	private void OnHeroAttack(BattleLogEntry entry)
	{
		fullBattleLog.Add(entry);

		// Update enemy HP with tween
		TweenHPBar(EnemyHPBar, entry.TargetCurrentHP);

		if (EnemyHPText != null)
			EnemyHPText.Text = $"{entry.TargetCurrentHP:F0} / {entry.TargetMaxHP:F0}";

		// Animate hero portrait: lunge right toward enemy
		AnimateAttackNudge(HeroPortrait, heroPortraitOrigin, AttackNudgeDistance);

		// Animate enemy portrait: shake from taking damage
		AnimateDamageShake(EnemyPortrait, enemyPortraitOrigin);

		// Build log line
		string line;
		Color lineColor;

		if (entry.IsCritical)
		{
			line = $"Hero strikes true! {entry.Damage:F0} damage!";
			lineColor = CritHighlightColor;
		}
		else
		{
			line = $"Hero strikes for {entry.Damage:F0} damage.";
			lineColor = HeroActionColor;
		}

		PushLogLine(line, lineColor, true);
	}

	private void OnEnemyAttack(BattleLogEntry entry)
	{
		fullBattleLog.Add(entry);

		if (entry.IsDodge)
		{
			// Animate enemy portrait: lunge left (attack attempt)
			AnimateAttackNudge(EnemyPortrait, enemyPortraitOrigin, -AttackNudgeDistance);

			// No damage, no HP update, no shake

			string dodgeLine = $"{entry.ActorName} lunges -- Hero evades!";
			PushLogLine(dodgeLine, DodgeColor, false);
		}
		else
		{
			// Update hero HP with tween
			TweenHPBar(HeroHPBar, entry.TargetCurrentHP);

			if (HeroHPText != null)
				HeroHPText.Text = $"{entry.TargetCurrentHP:F0} / {entry.TargetMaxHP:F0}";

			// Animate enemy portrait: lunge left toward hero
			AnimateAttackNudge(EnemyPortrait, enemyPortraitOrigin, -AttackNudgeDistance);

			// Animate hero portrait: shake from taking damage
			AnimateDamageShake(HeroPortrait, heroPortraitOrigin);

			string hitLine = $"{entry.ActorName} strikes for {entry.Damage:F0} damage.";
			PushLogLine(hitLine, EnemyActionColor, false);
		}
	}

	// -------------------------------------------------------------------------
	// Portrait Animations
	// -------------------------------------------------------------------------

	private void AnimateAttackNudge(TextureRect portrait, Vector2 origin, float direction)
	{
		if (portrait == null) return;

		// Kill any existing tweens on this portrait
		portrait.Position = origin;

		float speed = BattleSystem.Instance.GetCurrentSpeedMultiplier();
		float duration = NudgeDuration / speed;

		var tween = CreateTween();
		// Nudge outward
		tween.TweenProperty(portrait, "position:x",
			origin.X + direction, duration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);
		// Snap back
		tween.TweenProperty(portrait, "position:x",
			origin.X, duration)
			.SetEase(Tween.EaseType.In)
			.SetTrans(Tween.TransitionType.Quad);
	}

	private void AnimateDamageShake(TextureRect portrait, Vector2 origin)
	{
		if (portrait == null) return;

		float speed = BattleSystem.Instance.GetCurrentSpeedMultiplier();
		float duration = ShakeDuration / speed;

		var tween = CreateTween();

		for (int i = 0; i < ShakeCount; i++)
		{
			float dir = (i % 2 == 0) ? ShakeDistance : -ShakeDistance;
			tween.TweenProperty(portrait, "position:x",
				origin.X + dir, duration);
		}

		// Return to origin
		tween.TweenProperty(portrait, "position:x",
			origin.X, duration);
	}

	// -------------------------------------------------------------------------
	// HP Bar Tween
	// -------------------------------------------------------------------------

	private void TweenHPBar(ProgressBar bar, float targetValue)
	{
		if (bar == null) return;

		float speed = BattleSystem.Instance.GetCurrentSpeedMultiplier();
		float duration = HPBarTweenDuration / speed;

		var tween = CreateTween();
		tween.TweenProperty(bar, "value", (double)targetValue, duration)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);
	}

	// -------------------------------------------------------------------------
	// Battle Log (4-line scrolling display)
	// -------------------------------------------------------------------------

	private void ClearLogLines()
	{
		Label[] lines = { LogLine1, LogLine2, LogLine3, LogLine4 };
		foreach (var label in lines)
		{
			if (label != null)
			{
				label.Text = "";
				label.Modulate = new Color(1, 1, 1, 0);
			}
		}
	}

	private void PushLogLine(string text, Color color, bool isHeroAction = true)
	{
		logLineHistory.Add(text);
		logColorHistory.Add(color);
		logIsHeroAction.Add(isHeroAction);

		// Display the most recent 4 lines
		Label[] lines = { LogLine4, LogLine3, LogLine2, LogLine1 };
		// lines[0] = newest (bottom), lines[3] = oldest (top)

		int historyCount = logLineHistory.Count;

		for (int i = 0; i < 4; i++)
		{
			if (lines[i] == null) continue;

			int historyIndex = historyCount - 1 - i;

			if (historyIndex >= 0)
			{
				lines[i].Text = logLineHistory[historyIndex];
				Color c = logColorHistory[historyIndex];
				float targetAlpha = LogAlpha[i];

				// Alignment
				if (historyIndex < logIsHeroAction.Count && !logIsHeroAction[historyIndex])
					lines[i].HorizontalAlignment = HorizontalAlignment.Right;
				else
					lines[i].HorizontalAlignment = HorizontalAlignment.Left;

				// Fade in the newest line, set others directly
				if (i == 0)
				{
					// Newest line fades in
					lines[i].Modulate = new Color(c.R, c.G, c.B, 0);
					var tween = CreateTween();
					tween.TweenProperty(lines[i], "modulate:a",
						targetAlpha, 0.15f);
				}
				else
				{
					lines[i].Modulate = new Color(c.R, c.G, c.B, targetAlpha);
				}
			}
			else
			{
				lines[i].Text = "";
				lines[i].Modulate = new Color(1, 1, 1, 0);
			}
		}
	}

	private void ShowEncounterFlavor(EnemyData enemy)
	{
		string flavor = "";

		if (enemy.EncounterFlavorTexts != null && enemy.EncounterFlavorTexts.Count > 0)
		{
			int index = (int)(GD.Randi() % enemy.EncounterFlavorTexts.Count);
			flavor = enemy.EncounterFlavorTexts[index].ToString();
		}

		if (string.IsNullOrEmpty(flavor))
		{
			flavor = $"{enemy.EnemyName} blocks your path...";
		}

		PushLogLine(flavor, new Color(0.75f, 0.70f, 0.60f), true);
	}

	// -------------------------------------------------------------------------
	// Battle End -- Result Screen
	// -------------------------------------------------------------------------

	private void OnBattleEnded(BattleResult result)
	{
		isCombatActive = false;
		storedResult = result;

		// Reset portraits to their origin before hiding combat area
		ResetPortraitPositions();

		// Short delay before showing results
		float speed = BattleSystem.Instance.GetCurrentSpeedMultiplier();
		var timer = GetTree().CreateTimer(0.5f / speed);
		timer.Timeout += () => ShowResultScreen(result);
	}

	private void ShowResultScreen(BattleResult result)
	{
		HideCombatArea();
		FadeInResultOverlay();

		if (result.IsVictory)
			PopulateVictoryScreen(result);
		else
			PopulateDefeatScreen(result);
	}

	private void PopulateVictoryScreen(BattleResult result)
	{
		if (ResultSubtitleLabel != null)
			ResultSubtitleLabel.Text = "The deed is done";

		if (ResultTitleLabel != null)
		{
			ResultTitleLabel.Text = "VICTORY";
			ResultTitleLabel.Modulate = VictoryTitleColor;
		}

		if (ResultFlavorLabel != null)
		{
			ResultFlavorLabel.Text = $"The {result.Context.Enemy.EnemyName} falls before your might.";
		}

		if (ResultRewardLabel != null)
		{
			ResultRewardLabel.Text = $"+{result.Reward.FinalReward:N0} Kleos";
			ResultRewardLabel.Visible = true;
		}

		if (ResultLuckLabel != null)
		{
			if (result.Reward.WasLucky)
			{
				ResultLuckLabel.Text = $"Fortune smiles! {result.Reward.LuckMultiplier}x reward!";
				ResultLuckLabel.Visible = true;
			}
			else
			{
				ResultLuckLabel.Visible = false;
			}
		}

		if (ResultSummaryLabel != null)
		{
			ResultSummaryLabel.Text =
				$"Rounds: {result.TotalRounds}  |  " +
				$"HP: {result.HeroHPRemaining:F0}/{result.HeroMaxHP:F0}  |  " +
				$"Crits: {result.HeroCritsLanded}  |  " +
				$"Dodges: {result.HeroDodgesPerformed}";
		}

		if (ResultConsolationLabel != null)
			ResultConsolationLabel.Visible = false;

		if (ResultActionButton != null)
			ResultActionButton.Text = "CLAIM GLORY";
	}

	private void PopulateDefeatScreen(BattleResult result)
	{
		if (ResultSubtitleLabel != null)
			ResultSubtitleLabel.Text = "The fates are cruel";

		if (ResultTitleLabel != null)
		{
			ResultTitleLabel.Text = "DEFEAT";
			ResultTitleLabel.Modulate = DefeatTitleColor;
		}

		if (ResultFlavorLabel != null)
		{
			ResultFlavorLabel.Text = $"The {result.Context.Enemy.EnemyName} proved too strong... this time.";
		}

		if (ResultRewardLabel != null)
			ResultRewardLabel.Visible = false;

		if (ResultLuckLabel != null)
			ResultLuckLabel.Visible = false;

		if (ResultSummaryLabel != null)
		{
			ResultSummaryLabel.Text =
				$"Rounds: {result.TotalRounds}  |  " +
				$"Enemy HP: {result.EnemyHPRemaining:F0}/{result.Context.Enemy.Health:F0}  |  " +
				$"Crits: {result.HeroCritsLanded}  |  " +
				$"Dodges: {result.HeroDodgesPerformed}";
		}

		if (ResultConsolationLabel != null)
		{
			ResultConsolationLabel.Text = "Even Herakles knew defeat before glory.";
			ResultConsolationLabel.Visible = true;
		}

		if (ResultActionButton != null)
			ResultActionButton.Text = "RETREAT";
	}

	// -------------------------------------------------------------------------
	// Post-Combat Full Log
	// -------------------------------------------------------------------------

	private void PopulatePostCombatLog()
	{
		if (PostCombatLogList == null) return;

		foreach (Node child in PostCombatLogList.GetChildren())
			child.QueueFree();

		for (int i = 0; i < logLineHistory.Count; i++)
		{
			var label = new Label();
			label.Text = logLineHistory[i];
			label.Modulate = logColorHistory[i];
			label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			label.AddThemeFontSizeOverride("font_size", 13);

			if (i < logIsHeroAction.Count && !logIsHeroAction[i])
				label.HorizontalAlignment = HorizontalAlignment.Right;
			else
				label.HorizontalAlignment = HorizontalAlignment.Left;

			PostCombatLogList.AddChild(label);
		}

		if (PostCombatLogScroll != null)
			PostCombatLogScroll.ScrollVertical = 0;
	}

	// -------------------------------------------------------------------------
	// Speed Toggle
	// -------------------------------------------------------------------------

	private void RefreshSpeedToggle()
	{
		if (SpeedToggleButton == null) return;

		bool hasX2 = UpgradeManager.Instance.GetFlat(ModifierType.BattleSpeedX2Unlocked) >= 1f;

		if (!hasX2)
		{
			SpeedToggleButton.Visible = false;
			return;
		}

		SpeedToggleButton.Visible = true;
		currentSpeedIndex = 0;
		UpdateSpeedLabel();
	}

	private void OnSpeedTogglePressed()
	{
		bool hasX4 = UpgradeManager.Instance.GetFlat(ModifierType.BattleSpeedX4Unlocked) >= 1f;
		int maxIndex = hasX4 ? 2 : 1;

		currentSpeedIndex = (currentSpeedIndex + 1) % (maxIndex + 1);
		UpdateSpeedLabel();

		BattleSystem.Instance.SetSpeedMultiplier(speedMultipliers[currentSpeedIndex]);
	}

	private void UpdateSpeedLabel()
	{
		if (SpeedToggleButton == null) return;

		string[] labels = { "x1", "x2", "x4" };
		SpeedToggleButton.Text = labels[currentSpeedIndex];
	}

	// -------------------------------------------------------------------------
	// Button Handlers
	// -------------------------------------------------------------------------

	private void OnResultActionPressed()
	{
		FadePanelOut();
		isCombatActive = false;
	}

	private void OnViewBattleLogPressed()
	{
		HideResultOverlay();
		ShowPostCombatLog();
		PopulatePostCombatLog();
	}

	private void OnBackToResultsPressed()
	{
		HidePostCombatLog();
		ShowResultOverlay();
	}

	// -------------------------------------------------------------------------
	// Panel Fade In/Out
	// -------------------------------------------------------------------------

	private void FadePanelIn()
	{
		Visible = true;
		Modulate = new Color(1, 1, 1, 0);

		var tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 1.0f, PanelFadeDuration)
			.SetEase(Tween.EaseType.Out);
	}

	private void FadePanelOut()
	{
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 0.0f, PanelFadeDuration)
			.SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(() => Visible = false));
	}

	// -------------------------------------------------------------------------
	// Result Overlay Fade In
	// -------------------------------------------------------------------------

	private void FadeInResultOverlay()
	{
		if (ResultOverlay == null) return;

		ResultOverlay.Visible = true;
		ResultOverlay.Modulate = new Color(1, 1, 1, 0);

		var tween = CreateTween();
		tween.TweenProperty(ResultOverlay, "modulate:a", 1.0f, ResultFadeDuration)
			.SetEase(Tween.EaseType.Out);
	}

	// -------------------------------------------------------------------------
	// Visibility Helpers
	// -------------------------------------------------------------------------

	private void ShowCombatArea()
	{
		if (CombatArea != null) CombatArea.Visible = true;
	}

	private void HideCombatArea()
	{
		if (CombatArea != null) CombatArea.Visible = false;
	}

	private void ShowResultOverlay()
	{
		if (ResultOverlay != null) ResultOverlay.Visible = true;
	}

	private void HideResultOverlay()
	{
		if (ResultOverlay != null) ResultOverlay.Visible = false;
	}

	private void ShowPostCombatLog()
	{
		if (PostCombatLogOverlay != null) PostCombatLogOverlay.Visible = true;
	}

	private void HidePostCombatLog()
	{
		if (PostCombatLogOverlay != null) PostCombatLogOverlay.Visible = false;
	}

	// -------------------------------------------------------------------------
	// Utility
	// -------------------------------------------------------------------------

	private void SetProgressBarColor(ProgressBar bar, Color color)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = color;
		styleBox.ContentMarginLeft = 0;
		styleBox.ContentMarginRight = 0;
		styleBox.ContentMarginTop = 0;
		styleBox.ContentMarginBottom = 0;
		bar.AddThemeStyleboxOverride("fill", styleBox);
	}

	public float GetCurrentSpeedMultiplier()
	{
		return speedMultipliers[currentSpeedIndex];
	}
}

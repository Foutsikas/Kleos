using Godot;

public partial class MainMenuController : Control
{
    // --- Node References ---
    [Export] public Label PromptLabel { get; set; }
    [Export] public Button StartButton { get; set; }
    [Export] public Button SettingsButton { get; set; }
    [Export] public Control SettingsPanel { get; set; }
    [Export] public Button CloseSettingsButton { get; set; }
    [Export] public Button DeleteSaveButton { get; set; }
    [Export] public HSlider MusicSlider { get; set; }
    [Export] public HSlider SfxSlider { get; set; }
    [Export] public CheckBox FullscreenToggle { get; set; }
    [Export] public ColorRect FadeOverlay { get; set; }

    // --- Prompt Text ---
    private readonly string[] returningPrompts = new[]
    {
        "Continue Your Deeds",
        "Continue Your Journey",
        "Amuse the Gods",
        "The Fates Await",
        "Glory Calls Once More"
    };

    // --- State ---
    private bool isTransitioning = false;
    private bool settingsOpen = false;

    // --- Lifecycle ---

    public override void _Ready()
    {
        SetupPromptText();
        SetupSettingsPanel();
        ConnectButtons();
        FadeIn();
    }

    // --- Setup ---

    private void SetupPromptText()
    {
        if (PromptLabel == null) return;

        bool hasSave = SaveManager.Instance.HasSaveData();
        PromptLabel.Text = hasSave
            ? returningPrompts[GD.RandRange(0, returningPrompts.Length - 1)]
            : "Begin Your Journey";

        PulsePrompt();
    }

    private void SetupSettingsPanel()
    {
        if (SettingsPanel != null)
            SettingsPanel.Visible = false;

        if (MusicSlider != null)
            MusicSlider.Value = SettingsManager.Instance.MusicVolume;
        if (SfxSlider != null)
            SfxSlider.Value = SettingsManager.Instance.SfxVolume;
        if (FullscreenToggle != null)
            FullscreenToggle.SetPressedNoSignal(SettingsManager.Instance.Fullscreen);
    }

    private void ConnectButtons()
    {
        if (StartButton != null)
            StartButton.Pressed += OnStartButtonPressed;
        if (SettingsButton != null)
            SettingsButton.Pressed += OnSettingsButtonPressed;
        if (CloseSettingsButton != null)
            CloseSettingsButton.Pressed += OnCloseSettingsPressed;
        if (DeleteSaveButton != null)
            DeleteSaveButton.Pressed += OnDeleteSavePressed;
        if (MusicSlider != null)
            MusicSlider.ValueChanged += OnMusicVolumeChanged;
        if (SfxSlider != null)
            SfxSlider.ValueChanged += OnSfxVolumeChanged;
        if (FullscreenToggle != null)
            FullscreenToggle.Toggled += OnFullscreenToggled;
    }

    // --- Fade ---

    private async void FadeIn()
    {
        if (FadeOverlay == null) return;

        FadeOverlay.Visible = true;
        FadeOverlay.Modulate = new Color(0, 0, 0, 1);

        var tween = CreateTween();
        tween.TweenProperty(FadeOverlay, "modulate:a", 0f, 1.0);
        await ToSignal(tween, Tween.SignalName.Finished);

        FadeOverlay.Visible = false;
    }

    private async void FadeOutAndLoadGame()
    {
        if (FadeOverlay == null)
        {
            LoadGameScene();
            return;
        }

        FadeOverlay.Visible = true;
        FadeOverlay.Modulate = new Color(0, 0, 0, 0);

        var tween = CreateTween();
        tween.TweenProperty(FadeOverlay, "modulate:a", 1f, 1.0);
        await ToSignal(tween, Tween.SignalName.Finished);

        LoadGameScene();
    }

    private void LoadGameScene()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Game/main_game.tscn");
    }

    // --- Prompt Pulse ---

    private async void PulsePrompt()
    {
        if (PromptLabel == null) return;

        while (IsInstanceValid(this) && IsInsideTree())
        {
            var tween = CreateTween();
            tween.TweenProperty(PromptLabel, "modulate:a", 1.0f, 1.2);
            tween.TweenProperty(PromptLabel, "modulate:a", 0.3f, 1.2);
            await ToSignal(tween, Tween.SignalName.Finished);
        }
    }

    // --- Game Start ---

    private void StartGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        FadeOutAndLoadGame();
    }

    // --- Button Handlers ---

    private void OnStartButtonPressed() => StartGame();

    private void OnSettingsButtonPressed()
    {
        settingsOpen = true;
        if (SettingsPanel != null)
            SettingsPanel.Visible = true;
    }

    private void OnCloseSettingsPressed()
    {
        settingsOpen = false;
        if (SettingsPanel != null)
            SettingsPanel.Visible = false;
    }

    private void OnDeleteSavePressed()
    {
        SettingsManager.Instance.DeleteSaveData();
        GD.Print("[MainMenu] Save data deleted.");
        if (PromptLabel != null)
            PromptLabel.Text = "Begin Your Journey";
    }

    private void OnMusicVolumeChanged(double value)
    {
        SettingsManager.Instance.SetMusicVolume((float)value);
    }

    private void OnSfxVolumeChanged(double value)
    {
        SettingsManager.Instance.SetSfxVolume((float)value);
    }

    private void OnFullscreenToggled(bool pressed)
    {
        SettingsManager.Instance.SetFullscreen(pressed);
    }
}
using Godot;
using System.Collections.Generic;

public partial class FlavorTextManager : Node
{
    public static FlavorTextManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private Label flavorLabel;
    private Tween activeTween;
    private bool isOmenActive = false;
    private FlavorTextLibrary library;

    // Flavor text duration
    private const float FlavorDisplayTime = 2.5f;
    private const float FadeInTime = 0.3f;
    private const float FadeOutTime = 0.5f;

    // Colors
    private static readonly Color FlavorColor = new Color("B8A88A");
    private static readonly Color OmenColor = new Color("C4785A");

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;

        library = GD.Load<FlavorTextLibrary>("res://Resources/FlavorText/flavor_text_library.tres");
        if (library == null)
            GD.PrintErr("[FlavorTextManager] FlavorTextLibrary failed to load.");

        // Subscribe to artisan purchases for flavor text
        if (ArtisanManager.Instance != null)
            ArtisanManager.Instance.ArtisanPurchased += OnArtisanPurchased;

        GD.Print("[FlavorTextManager] Ready.");
    }

    public override void _ExitTree()
    {
        if (ArtisanManager.Instance != null)
            ArtisanManager.Instance.ArtisanPurchased -= OnArtisanPurchased;
    }

    // -------------------------------------------------------------------------
    // Label Setup (called by MainGameController)
    // -------------------------------------------------------------------------

    public void SetLabel(Label label)
    {
        flavorLabel = label;

        if (flavorLabel != null)
        {
            flavorLabel.Text = "";
            flavorLabel.Modulate = new Color(1, 1, 1, 0);
            flavorLabel.HorizontalAlignment = HorizontalAlignment.Center;
            flavorLabel.VerticalAlignment = VerticalAlignment.Center;
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// Show a brief flavor message. Ignored if an omen is active.
    public void ShowFlavor(string text)
    {
        if (flavorLabel == null) return;
        if (isOmenActive) return;

        KillTween();
        PlayFlavorSequence(text, FlavorColor);
    }

    /// Show an omen warning. Replaces any current flavor text.
    /// Stays visible until ClearOmen() is called.
    public void ShowOmen(string text)
    {
        if (flavorLabel == null) return;

        KillTween();
        isOmenActive = true;

        flavorLabel.Text = text;
        flavorLabel.Modulate = new Color(1, 1, 1, 0);
        flavorLabel.AddThemeColorOverride("font_color", OmenColor);

        activeTween = CreateTween();
        activeTween.TweenProperty(flavorLabel, "modulate:a",
            1.0f, FadeInTime).SetEase(Tween.EaseType.Out);

        GD.Print($"[FlavorTextManager] Omen: {text}");
    }

    /// Show a random omen from the built-in pool.
    public void ShowRandomOmen()
    {
        string line = library != null
            ? library.GetRandomOmenLine()
            : "A cold wind stirs the dust...";
        ShowOmen(line);
    }

    public void ShowOmenForPool(EncounterPool pool)
    {
        if (pool != null && pool.OmenLines != null && pool.OmenLines.Count > 0)
        {
            int i = (int)(GD.Randi() % pool.OmenLines.Count);
            ShowOmen(pool.OmenLines[i]);
            return;
        }
        ShowRandomOmen();
    }

    /// Clear the current omen. Called when encounter fires.
    public void ClearOmen()
    {
        if (!isOmenActive) return;

        isOmenActive = false;
        FadeOut();

        GD.Print("[FlavorTextManager] Omen cleared.");
    }

    /// Force clear any displayed text (flavor or omen).
    public void Clear()
    {
        KillTween();
        isOmenActive = false;

        if (flavorLabel != null)
        {
            flavorLabel.Text = "";
            flavorLabel.Modulate = new Color(1, 1, 1, 0);
        }
    }

    // -------------------------------------------------------------------------
    // Artisan Purchase Handler
    // -------------------------------------------------------------------------

    private void OnArtisanPurchased(string artisanId)
    {
        if (isOmenActive) return;

        var artisan = ArtisanManager.Instance?.GetArtisanById(artisanId);

        if (artisan != null
            && artisan.PurchaseFlavorLines != null
            && artisan.PurchaseFlavorLines.Count > 0)
        {
            int index = (int)(GD.Randi() % artisan.PurchaseFlavorLines.Count);
            ShowFlavor(artisan.PurchaseFlavorLines[index]);
            return;
        }

        if (library != null)
            ShowFlavor(library.GetRandomGenericArtisanLine());
    }

    // -------------------------------------------------------------------------
    // Animation
    // -------------------------------------------------------------------------

    private void PlayFlavorSequence(string text, Color color)
    {
        flavorLabel.Text = text;
        flavorLabel.Modulate = new Color(1, 1, 1, 0);
        flavorLabel.AddThemeColorOverride("font_color", color);

        activeTween = CreateTween();

        // Fade in
        activeTween.TweenProperty(flavorLabel, "modulate:a",
            1.0f, FadeInTime).SetEase(Tween.EaseType.Out);

        // Hold
        activeTween.TweenInterval(FlavorDisplayTime);

        // Fade out
        activeTween.TweenProperty(flavorLabel, "modulate:a",
            0.0f, FadeOutTime).SetEase(Tween.EaseType.In);

        // Clear text after fade
        activeTween.TweenCallback(Callable.From(() =>
        {
            flavorLabel.Text = "";
        }));
    }

    private void FadeOut()
    {
        KillTween();

        if (flavorLabel == null) return;

        activeTween = CreateTween();
        activeTween.TweenProperty(flavorLabel, "modulate:a",
            0.0f, FadeOutTime).SetEase(Tween.EaseType.In);

        activeTween.TweenCallback(Callable.From(() =>
        {
            flavorLabel.Text = "";
        }));
    }

    private void KillTween()
    {
        if (activeTween != null && activeTween.IsValid())
            activeTween.Kill();

        activeTween = null;
    }
}

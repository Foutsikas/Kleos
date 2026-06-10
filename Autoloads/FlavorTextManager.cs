// FlavorTextManager.cs
// Location: res://Autoloads/FlavorTextManager.cs
// Autoload singleton (position 12, after DevConsole).
//
// Manages the FlavorTextLabel in CenterPanel. Handles two types
// of messages:
//
//   Flavor text: brief messages on artisan purchases, milestones.
//     Shows for 2.5 seconds, then fades out. Low priority.
//
//   Omen text: pre-battle warnings from RandomEncounterManager.
//     Stays visible until cleared by the encounter trigger or
//     a timeout. High priority -- replaces current flavor text.
//
// Any system can call Show() or ShowOmen() to display a message.
// The label reference is set by MainGameController after scene load.

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

    // Flavor text duration
    private const float FlavorDisplayTime = 2.5f;
    private const float FadeInTime = 0.3f;
    private const float FadeOutTime = 0.5f;

    // Colors
    private static readonly Color FlavorColor = new Color("B8A88A");
    private static readonly Color OmenColor = new Color("C4785A");

    // Omen text pool
    private static readonly string[] OmenTexts = new string[]
    {
        "The birds have gone quiet...",
        "A cold wind stirs the dust...",
        "Something watches from the treeline...",
        "The shadows grow restless...",
        "Your hand reaches for a weapon...",
        "The air tastes of iron...",
        "A branch snaps in the distance...",
        "The hairs on your neck rise...",
        "An unnatural stillness settles...",
        "The ground trembles faintly...",
        "A crow circles overhead...",
        "The wind carries a low growl...",
    };

    // Artisan flavor text pools (per artisan ID)
    private static readonly Dictionary<string, string[]> ArtisanFlavorTexts = new()
    {
        ["scribe"] = new string[]
        {
            "A reed pen scratches parchment...",
            "Another hand to record your deeds...",
            "Ink flows in your name...",
            "Words take shape on papyrus...",
        },
        ["bard"] = new string[]
        {
            "A new voice joins the chorus...",
            "The melody grows richer...",
            "A lyre string hums your name...",
            "Song carries across the agora...",
        },
        ["potter"] = new string[]
        {
            "Clay takes shape beneath steady hands...",
            "Another vessel bears your mark...",
            "The kiln fire burns bright...",
            "Red earth becomes art...",
        },
        ["sculptor"] = new string[]
        {
            "Chisel strikes marble...",
            "Your likeness emerges from stone...",
            "Dust fills the workshop...",
            "Another statue stands in your honor...",
        },
        ["playwright"] = new string[]
        {
            "A new act unfolds on the stage...",
            "The audience leans forward...",
            "Drama echoes through the amphitheater...",
            "The chorus speaks your name...",
        },
        ["historian"] = new string[]
        {
            "The chronicles grow longer...",
            "Your name is etched into record...",
            "History bends toward your deeds...",
            "Scrolls fill the archive...",
        },
    };

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
        int index = (int)(GD.Randi() % OmenTexts.Length);
        ShowOmen(OmenTexts[index]);
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

        if (ArtisanFlavorTexts.TryGetValue(artisanId, out string[] texts))
        {
            int index = (int)(GD.Randi() % texts.Length);
            ShowFlavor(texts[index]);
        }
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

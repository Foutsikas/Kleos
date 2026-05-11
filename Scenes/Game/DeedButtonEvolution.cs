using Godot;

public partial class DeedButtonEvolution : Control
{
    // -------------------------------------------------------------------------
    // Exports
    // -------------------------------------------------------------------------

    [Export] public Button DeedButton { get; set; }
    [Export] public ColorRect DeedGlow { get; set; }

    // -------------------------------------------------------------------------
    // Tier Color Definitions
    // -------------------------------------------------------------------------

    private struct TierVisuals
    {
        public Color Background;
        public Color BackgroundHover;
        public Color BackgroundPressed;
        public Color Border;
        public int BorderWidth;
        public Color FontColor;
        public bool GlowActive;

        public TierVisuals(
            Color bg, Color bgHover, Color bgPressed,
            Color border, int borderWidth,
            Color fontColor, bool glowActive)
        {
            Background = bg;
            BackgroundHover = bgHover;
            BackgroundPressed = bgPressed;
            Border = border;
            BorderWidth = borderWidth;
            FontColor = fontColor;
            GlowActive = glowActive;
        }
    }

    private static readonly TierVisuals[] Tiers = new TierVisuals[]
    {
        // Tier 0 - Bronze: terracotta, unfired clay
        new TierVisuals(
            bg:            new Color("6B3A2A"),
            bgHover:       new Color("7D4A38"),
            bgPressed:     new Color("5A2E20"),
            border:        new Color("5C3317"),
            borderWidth:   1,
            fontColor:     new Color("E8D5C4"),
            glowActive:    false
        ),
        // Tier 1 - Silver: fired and smoothed clay
        new TierVisuals(
            bg:            new Color("7A5C45"),
            bgHover:       new Color("8C6B52"),
            bgPressed:     new Color("6A4E3A"),
            border:        new Color("8C7B6B"),
            borderWidth:   2,
            fontColor:     new Color("F0E6DC"),
            glowActive:    false
        ),
        // Tier 2 - Gold: warm bronze-gold
        new TierVisuals(
            bg:            new Color("8B6914"),
            bgHover:       new Color("9E7A1E"),
            bgPressed:     new Color("7A5B0E"),
            border:        new Color("DAA520"),
            borderWidth:   2,
            fontColor:     new Color("FFF3D6"),
            glowActive:    false
        ),
        // Tier 3 - Divine: deep navy with gold
        new TierVisuals(
            bg:            new Color("1B2A4A"),
            bgHover:       new Color("243660"),
            bgPressed:     new Color("14203A"),
            border:        new Color("FFD700"),
            borderWidth:   3,
            fontColor:     new Color("FFD700"),
            glowActive:    true
        ),
    };

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private int currentTier = -1;
    private Tween transitionTween;
    private Tween glowTween;

    // StyleBox references (one per button state, reused across tier changes)
    private StyleBoxFlat styleNormal;
    private StyleBoxFlat styleHover;
    private StyleBoxFlat stylePressed;
    private StyleBoxFlat styleFocus;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        if (DeedButton == null)
        {
            GD.PrintErr("[DeedButtonEvolution] DeedButton export is null.");
            return;
        }

        CreateStyleBoxes();
        SetupGlow();

        // Apply current tier instantly on load (no animation)
        int tier = CalculateTier();
        ApplyTierInstant(tier);
        currentTier = tier;

        // Subscribe to artisan purchases
        if (ArtisanManager.Instance != null)
            ArtisanManager.Instance.ArtisanPurchased += OnArtisanPurchased;
    }

    public override void _ExitTree()
    {
        if (ArtisanManager.Instance != null)
            ArtisanManager.Instance.ArtisanPurchased -= OnArtisanPurchased;

        KillTweens();
    }

    // -------------------------------------------------------------------------
    // StyleBox Setup
    // -------------------------------------------------------------------------

    private void CreateStyleBoxes()
    {
        // Create one StyleBoxFlat per button state. We modify these in place
        // when changing tiers rather than creating new ones each time.
        styleNormal = new StyleBoxFlat();
        styleHover = new StyleBoxFlat();
        stylePressed = new StyleBoxFlat();
        styleFocus = new StyleBoxFlat();

        // Corner radius for all states
        int cornerRadius = 6;
        foreach (var style in new[] { styleNormal, styleHover, stylePressed, styleFocus })
        {
            style.CornerRadiusTopLeft = cornerRadius;
            style.CornerRadiusTopRight = cornerRadius;
            style.CornerRadiusBottomLeft = cornerRadius;
            style.CornerRadiusBottomRight = cornerRadius;

            // Content margins for text padding
            style.ContentMarginLeft = 16;
            style.ContentMarginRight = 16;
            style.ContentMarginTop = 12;
            style.ContentMarginBottom = 12;
        }

        // Assign to button theme overrides
        DeedButton.AddThemeStyleboxOverride("normal", styleNormal);
        DeedButton.AddThemeStyleboxOverride("hover", styleHover);
        DeedButton.AddThemeStyleboxOverride("pressed", stylePressed);
        DeedButton.AddThemeStyleboxOverride("focus", styleFocus);
    }

    private void SetupGlow()
    {
        if (DeedGlow == null) return;

        DeedGlow.Color = new Color("FFD700", 0.0f);
        DeedGlow.Visible = false;
    }

    // -------------------------------------------------------------------------
    // Tier Calculation
    // -------------------------------------------------------------------------

    private int CalculateTier()
    {
        if (ArtisanManager.Instance == null) return 0;

        int count = ArtisanManager.Instance.GetUnlockedCount();

        if (count >= 6) return 3;  // Divine
        if (count >= 4) return 2;  // Gold
        if (count >= 2) return 1;  // Silver
        return 0;                  // Bronze
    }

    // -------------------------------------------------------------------------
    // Tier Application (Instant, no animation)
    // -------------------------------------------------------------------------

    private void ApplyTierInstant(int tier)
    {
        tier = Mathf.Clamp(tier, 0, Tiers.Length - 1);
        TierVisuals v = Tiers[tier];

        // Normal state
        styleNormal.BgColor = v.Background;
        styleNormal.BorderColor = v.Border;
        SetBorderWidth(styleNormal, v.BorderWidth);

        // Hover state (slightly lighter)
        styleHover.BgColor = v.BackgroundHover;
        styleHover.BorderColor = v.Border;
        SetBorderWidth(styleHover, v.BorderWidth);

        // Pressed state (slightly darker)
        stylePressed.BgColor = v.BackgroundPressed;
        stylePressed.BorderColor = v.Border;
        SetBorderWidth(stylePressed, v.BorderWidth);

        // Focus state (same as normal)
        styleFocus.BgColor = v.Background;
        styleFocus.BorderColor = v.Border;
        SetBorderWidth(styleFocus, v.BorderWidth);

        // Font color
        DeedButton.AddThemeColorOverride("font_color", v.FontColor);
        DeedButton.AddThemeColorOverride("font_hover_color", v.FontColor);
        DeedButton.AddThemeColorOverride("font_pressed_color", v.FontColor);
        DeedButton.AddThemeColorOverride("font_focus_color", v.FontColor);

        // Glow
        if (DeedGlow != null)
        {
            if (v.GlowActive)
            {
                DeedGlow.Visible = true;
                StartGlowLoop();
            }
            else
            {
                StopGlowLoop();
                DeedGlow.Visible = false;
            }
        }

        GD.Print($"[DeedButtonEvolution] Applied tier {tier} instantly.");
    }

    private void SetBorderWidth(StyleBoxFlat style, int width)
    {
        style.BorderWidthTop = width;
        style.BorderWidthBottom = width;
        style.BorderWidthLeft = width;
        style.BorderWidthRight = width;
    }

    // -------------------------------------------------------------------------
    // Tier Transition (Animated)
    // -------------------------------------------------------------------------

    private void TransitionToTier(int newTier)
    {
        newTier = Mathf.Clamp(newTier, 0, Tiers.Length - 1);

        // Cancel any in-progress transition
        if (transitionTween != null && transitionTween.IsValid())
            transitionTween.Kill();

        TierVisuals target = Tiers[newTier];

        transitionTween = CreateTween();
        transitionTween.SetParallel(true);

        // Step 1: Flash burst (white overlay fades out)
        PlayFlash();

        // Step 2: Tween StyleBox colors (0.5 seconds)
        float duration = 0.5f;

        // Background colors
        transitionTween.TweenProperty(styleNormal, "bg_color",
            target.Background, duration).SetEase(Tween.EaseType.Out);
        transitionTween.TweenProperty(styleHover, "bg_color",
            target.BackgroundHover, duration).SetEase(Tween.EaseType.Out);
        transitionTween.TweenProperty(stylePressed, "bg_color",
            target.BackgroundPressed, duration).SetEase(Tween.EaseType.Out);
        transitionTween.TweenProperty(styleFocus, "bg_color",
            target.Background, duration).SetEase(Tween.EaseType.Out);

        // Border colors
        transitionTween.TweenProperty(styleNormal, "border_color",
            target.Border, duration).SetEase(Tween.EaseType.Out);
        transitionTween.TweenProperty(styleHover, "border_color",
            target.Border, duration).SetEase(Tween.EaseType.Out);
        transitionTween.TweenProperty(stylePressed, "border_color",
            target.Border, duration).SetEase(Tween.EaseType.Out);
        transitionTween.TweenProperty(styleFocus, "border_color",
            target.Border, duration).SetEase(Tween.EaseType.Out);

        // Border width (set immediately, not tweened -- integer values)
        SetBorderWidth(styleNormal, target.BorderWidth);
        SetBorderWidth(styleHover, target.BorderWidth);
        SetBorderWidth(stylePressed, target.BorderWidth);
        SetBorderWidth(styleFocus, target.BorderWidth);

        // Font color (tweened via theme override)
        // Godot theme color overrides cannot be directly tweened,
        // so we use a callback tween to interpolate manually.
        Color startFontColor = DeedButton.GetThemeColor("font_color");
        Color endFontColor = target.FontColor;

        transitionTween.SetParallel(false);
        transitionTween.TweenMethod(
            Callable.From<float>((t) =>
            {
                Color lerped = startFontColor.Lerp(endFontColor, t);
                DeedButton.AddThemeColorOverride("font_color", lerped);
                DeedButton.AddThemeColorOverride("font_hover_color", lerped);
                DeedButton.AddThemeColorOverride("font_pressed_color", lerped);
                DeedButton.AddThemeColorOverride("font_focus_color", lerped);
            }),
            0.0f, 1.0f, duration
        ).SetEase(Tween.EaseType.Out);

        // Step 3: After transition, start or stop glow
        transitionTween.TweenCallback(Callable.From(() =>
        {
            if (DeedGlow != null)
            {
                if (target.GlowActive)
                {
                    DeedGlow.Visible = true;
                    DeedGlow.Color = new Color("FFD700", 0.0f);
                    StartGlowLoop();
                }
                else
                {
                    StopGlowLoop();
                    DeedGlow.Visible = false;
                }
            }
        }));

        GD.Print($"[DeedButtonEvolution] Transitioning to tier {newTier}.");
    }

    // -------------------------------------------------------------------------
    // Flash Effect
    // -------------------------------------------------------------------------

    private void PlayFlash()
    {
        if (DeedGlow == null) return;

        // Temporarily use the glow rect for the flash burst.
        // Save current visibility to restore after flash.
        DeedGlow.Visible = true;
        DeedGlow.Color = new Color(1.0f, 1.0f, 1.0f, 0.6f);
        DeedGlow.Scale = Vector2.One;

        var flashTween = CreateTween();
        flashTween.SetParallel(true);

        // Scale outward from center
        flashTween.TweenProperty(DeedGlow, "scale",
            new Vector2(1.3f, 1.3f), 0.3f).SetEase(Tween.EaseType.Out);

        // Fade out
        flashTween.TweenProperty(DeedGlow, "color",
            new Color(1.0f, 1.0f, 1.0f, 0.0f), 0.3f).SetEase(Tween.EaseType.Out);

        // Reset scale after flash
        flashTween.SetParallel(false);
        flashTween.TweenCallback(Callable.From(() =>
        {
            DeedGlow.Scale = Vector2.One;
        }));
    }

    // -------------------------------------------------------------------------
    // Divine Glow Loop
    // -------------------------------------------------------------------------

    private void StartGlowLoop()
    {
        StopGlowLoop();

        if (DeedGlow == null) return;

        DeedGlow.Color = new Color("FFD700", 0.10f);

        glowTween = CreateTween();
        glowTween.SetLoops(0); // Infinite

        // Pulse alpha: 0.10 -> 0.30 -> 0.10
        glowTween.TweenProperty(DeedGlow, "color",
            new Color("FFD700", 0.30f), 1.0f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);

        glowTween.TweenProperty(DeedGlow, "color",
            new Color("FFD700", 0.10f), 1.0f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private void StopGlowLoop()
    {
        if (glowTween != null && glowTween.IsValid())
            glowTween.Kill();

        glowTween = null;
    }

    // -------------------------------------------------------------------------
    // Signal Handlers
    // -------------------------------------------------------------------------

    private void OnArtisanPurchased(string artisanId)
    {
        int newTier = CalculateTier();

        if (newTier != currentTier)
        {
            TransitionToTier(newTier);
            currentTier = newTier;
        }
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    private void KillTweens()
    {
        if (transitionTween != null && transitionTween.IsValid())
            transitionTween.Kill();

        StopGlowLoop();
    }

    // -------------------------------------------------------------------------
    // Dev Console Support
    // -------------------------------------------------------------------------

    public void ForceVisualTier(int tier)
    {
        tier = Mathf.Clamp(tier, 0, Tiers.Length - 1);
        KillTweens();
        ApplyTierInstant(tier);
        currentTier = tier;
        GD.Print($"[DeedButtonEvolution] Forced to tier {tier} (dev).");
    }
}

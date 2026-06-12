using Godot;
using Godot.Collections;

[GlobalClass]
public partial class FlavorTextLibrary : Resource
{
    [ExportGroup("Omens")]
    [Export] public Array<string> GenericOmenLines { get; set; } = new();

    [ExportGroup("Artisans")]
    [Export] public Array<string> GenericArtisanLines { get; set; } = new();

    [ExportGroup("Milestones")]
    [Export] public Array<string> MilestoneLines { get; set; } = new();

    // --- Accessors with fallbacks ---

    private string GetRandom(Array<string> pool, string fallback)
    {
        if (pool == null || pool.Count == 0)
            return fallback;
        return pool[GD.RandRange(0, pool.Count - 1)];
    }

    public string GetRandomOmenLine() =>
        GetRandom(GenericOmenLines, "A cold wind stirs the dust...");

    public string GetRandomGenericArtisanLine() =>
        GetRandom(GenericArtisanLines, "Another pair of hands joins the work...");

    public string GetRandomMilestoneLine() =>
        GetRandom(MilestoneLines, "The story continues...");
}

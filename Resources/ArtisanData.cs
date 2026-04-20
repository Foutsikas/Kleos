using Godot;

[GlobalClass]
public partial class ArtisanData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string ArtisanName { get; set; } = "";
    [Export] public string ArtisanId { get; set; } = "";

    [ExportGroup("Economy")]
    [Export] public float BaseCost { get; set; } = 10f;
    [Export] public float KleosPerSecond { get; set; } = 0.2f;
    [Export] public float CostMultiplier { get; set; } = 1.18f;

    [ExportGroup("Unlock Condition")]
    [Export] public string RequiredArtisanId { get; set; } = "";
    [Export] public int RequiredArtisanCount { get; set; } = 0;
}

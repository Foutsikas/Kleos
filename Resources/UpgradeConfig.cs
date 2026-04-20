using Godot;
using Godot.Collections;

[GlobalClass]
public partial class UpgradeConfig : Resource
{
    [ExportGroup("Identity")]
    [Export] public string UpgradeName { get; set; } = "";
    [Export] public string UpgradeId { get; set; } = "";
    [Export] public string Description { get; set; } = "";

    [ExportGroup("Cost")]
    [Export] public float Cost { get; set; } = 100f;

    [ExportGroup("Tier Gate")]
    [Export] public int Tier { get; set; } = 1;
    [Export] public DungeonData RequiredDungeon { get; set; }

    [ExportGroup("Individual Lock")]
    [Export] public int RequiredHeroLevel { get; set; } = 0;
    [Export] public string RequiredUpgradeId { get; set; } = "";
    [Export] public string RequiredArtisanId { get; set; } = "";
    [Export] public int RequiredArtisanCount { get; set; } = 0;

    [ExportGroup("Effects")]
    [Export] public Array Effects { get; set; } = new();

    public ModifierEffect GetEffect(int index)
    {
        if (index < 0 || index >= Effects.Count) return null;
        return Effects[index].As<ModifierEffect>();
    }
}
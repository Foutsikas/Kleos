using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DungeonLayer : Resource
{
    [Export] public EnemyData Enemy { get; set; }
    [Export] public float BaseKleosReward { get; set; } = 10f;
    [Export] public bool IsBossLayer { get; set; } = false;
    [Export] public bool IsMiniBossLayer { get; set; } = false;
}

[GlobalClass]
public partial class DungeonData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string DungeonName { get; set; } = "";
    [Export] public string DungeonId { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public Texture2D DungeonIcon { get; set; }

    [ExportGroup("Layers")]
    [Export] public Array<DungeonLayer> Layers { get; set; } = new();

    [ExportGroup("Unlock Requirements")]
    [Export] public float KleosRequirement { get; set; } = 0f;
    [Export] public int ArtisanRequirement { get; set; } = 0;
    [Export] public DungeonData RequiredDungeon { get; set; }
}

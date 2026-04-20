using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DungeonData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string DungeonName { get; set; } = "";
    [Export] public string DungeonId { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public Texture2D DungeonIcon { get; set; }

    [ExportGroup("Layers")]
    [Export] public Array Layers { get; set; } = new();

    public DungeonLayer GetLayer(int index)
    {
        if (index < 0 || index >= Layers.Count) return null;
        return Layers[index].As<DungeonLayer>();
    }

    [ExportGroup("Unlock Requirements")]
    [Export] public float KleosRequirement { get; set; } = 0f;
    [Export] public int ArtisanRequirement { get; set; } = 0;
    [Export] public DungeonData RequiredDungeon { get; set; }
}
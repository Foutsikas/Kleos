using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EncounterPoolEntry : Resource
{
    [Export] public EnemyData Enemy { get; set; }
    [Export] public float Weight { get; set; } = 1.0f;
}

[GlobalClass]
public partial class EncounterPool : Resource
{
    [ExportGroup("Identity")]
    [Export] public string PoolName { get; set; } = "";

    [ExportGroup("Gate")]
    [Export] public DungeonData RequiredDungeon { get; set; }

    [ExportGroup("Enemies")]
    [Export] public Array<EncounterPoolEntry> Entries { get; set; } = new();
}

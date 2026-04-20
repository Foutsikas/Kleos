using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EncounterPool : Resource
{
    [ExportGroup("Identity")]
    [Export] public string PoolName { get; set; } = "";

    [ExportGroup("Gate")]
    [Export] public DungeonData RequiredDungeon { get; set; }

    [ExportGroup("Enemies")]
    [Export] public Array Entries { get; set; } = new();

    public EncounterPoolEntry GetEntry(int index)
    {
        if (index < 0 || index >= Entries.Count) return null;
        return Entries[index].As<EncounterPoolEntry>();
    }
}
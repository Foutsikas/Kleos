using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EncounterPoolDatabase : Resource
{
    [Export] public Array<EncounterPool> Pools = new();
}

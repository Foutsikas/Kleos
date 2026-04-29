using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DungeonDatabase : Resource
{
    [Export] public Array<DungeonData> Dungeons = new();
}